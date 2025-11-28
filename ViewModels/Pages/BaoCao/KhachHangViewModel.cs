using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.BaoCao
{
    public partial class KhachHangViewModel : ObservableObject, INavigationAware
    {
        //private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ApiService _apiService;
        private bool _isInitialized = false;

        // --- Filter ---
        [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-30);
        [ObservableProperty] private DateTime _endDate = DateTime.Now;
        [ObservableProperty] private bool _isBusy;

        // --- KPIs ---
        [ObservableProperty] private int _totalCustomers;       // Tổng khách hàng trong DB
        [ObservableProperty] private int _newCustomers;         // Khách mới tạo trong kỳ
        [ObservableProperty] private int _activeCustomers;      // Khách có mua hàng trong kỳ
        [ObservableProperty] private decimal _totalRevenueInPeriod; // Doanh thu từ khách trong kỳ

        // --- Charts (Top Spending) ---
        [ObservableProperty] private ISeries[] _topSpendingSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _topSpendingXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _topSpendingYAxes = Array.Empty<Axis>();

        [ObservableProperty] private ObservableCollection<TopCustomerDto> _topCustomers = new();

        public KhachHangViewModel(ApiService apiService)
        {
            _apiService = apiService;
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            TopSpendingXAxes = new Axis[]
            {
                new Axis
                {
                    //Labeler = value => value.ToString("N0"),
                    LabelsPaint = null,
                    //TextSize = 12,
                    ShowSeparatorLines = true
                }
            };

            TopSpendingYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    TextSize = 12,
                    ShowSeparatorLines = false
                }
            };
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                await LoadDataAsync();
                _isInitialized = true;
            }
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                //await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Chuẩn hóa thời gian
                //var start = StartDate.Date;
                //var end = EndDate.Date.AddDays(1).AddTicks(-1);
                // Giả sử ta coi khách hàng có đơn hàng đầu tiên trong kỳ là khách mới
                // Hoặc đơn giản là đếm số khách mua hàng trong kỳ (Active)
                // Ở đây ta lấy số lượng khách hàng CÓ GIAO DỊCH XUẤT KHO trong kỳ
                /*var activeCustomerIds = await db.Exports
                    .Where(e => e.ExportDate >= start && e.ExportDate <= end && e.CustomerId != null)
                    .Select(e => e.CustomerId)
                    .Distinct()
                    .ToListAsync();*/

                // Gọi API lấy báo cáo khách hàng
                var reportData = await _apiService.GetReportAsync<CustomerReportReponse>("Reports/Customers", StartDate, EndDate);

                if(reportData != null)
                {
                    //Gán dữ liệu
                    TotalCustomers = reportData.TotalCustomers;
                    ActiveCustomers = reportData.ActiveCustomers;
                    NewCustomers = reportData.TotalOrders;
                    TotalRevenueInPeriod = reportData.TotalRevenue;

                    TopCustomers.Clear();
                    foreach(var item in reportData.TopCustomers)
                        TopCustomers.Add(item);

                    //Vẽ biểu đồ Top 5
                    var top5 = reportData.TopCustomers.Take(5).ToList();
                    top5.Reverse();

                    TopSpendingSeries = new ISeries[]
                    {
                        new RowSeries<decimal>
                        {
                            Name = "Chi tiêu",
                            Values = top5.Select(x => x.TotalSpent).ToArray(),
                            Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                            Stroke = null,
                            DataLabelsSize = 12,
                            DataLabelsPaint = new SolidColorPaint(SKColors.White),
                            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                            DataLabelsFormatter = point => $"{point.Model:N0}",
                            // RowSeries dùng XToolTipLabelFormatter cho giá trị (vì trục giá trị nằm ngang)
                            XToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                        }
                    };

                    TopSpendingYAxes[0].Labels = top5.Select(x => x.Name).ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Customer Report: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}