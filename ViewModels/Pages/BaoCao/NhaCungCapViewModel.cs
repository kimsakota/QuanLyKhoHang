using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.BaoCao
{
    public partial class NhaCungCapViewModel : ObservableObject, INavigationAware
    {
        private readonly ApiService _apiService;
        private bool _isInitialized = false;

        // --- Filter ---
        [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-30);
        [ObservableProperty] private DateTime _endDate = DateTime.Now;
        [ObservableProperty] private bool _isBusy;

        // --- KPIs ---
        [ObservableProperty] private int _totalSuppliers;
        [ObservableProperty] private int _totalImportOrders;
        [ObservableProperty] private int _activeSuppliers;
        [ObservableProperty] private decimal _totalImportCost;

        // --- Charts (Top Spending) ---
        [ObservableProperty] private ISeries[] _topImportSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _topImportXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _topImportYAxes = Array.Empty<Axis>();

        [ObservableProperty] private ObservableCollection<TopSupplierDto> _topSuppliers = new();

        public NhaCungCapViewModel(ApiService apiService)
        {
            _apiService = apiService;
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            TopImportXAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = null, // Ẩn label trục X vì dùng RowSeries
                    ShowSeparatorLines = true
                }
            };

            TopImportYAxes = new Axis[]
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
                // Gọi API lấy báo cáo nhà cung cấp
                // Lưu ý: Bạn cần đảm bảo Backend đã có endpoint "Reports/Suppliers" trả về SupplierReportResponse
                var reportData = await _apiService.GetReportAsync<SupplierReportResponse>("Reports/Suppliers", StartDate, EndDate);

                if (reportData != null)
                {
                    // Gán dữ liệu KPI
                    TotalSuppliers = reportData.TotalSuppliers;
                    ActiveSuppliers = reportData.ActiveSuppliers;
                    TotalImportOrders = reportData.TotalImportOrders;
                    TotalImportCost = reportData.TotalImportCost;

                    // Gán danh sách chi tiết
                    TopSuppliers.Clear();
                    foreach (var item in reportData.TopSuppliers)
                        TopSuppliers.Add(item);

                    // Vẽ biểu đồ Top 5
                    var top5 = reportData.TopSuppliers.Take(5).ToList();
                    top5.Reverse(); // Đảo ngược để hiển thị từ trên xuống trong RowSeries

                    TopImportSeries = new ISeries[]
                    {
                        new RowSeries<decimal>
                        {
                            Name = "Giá trị nhập",
                            Values = top5.Select(x => x.TotalImportValue).ToArray(),
                            Fill = new SolidColorPaint(SKColors.IndianRed), // Màu đỏ nhạt cho chi phí
                            Stroke = null,
                            DataLabelsSize = 12,
                            DataLabelsPaint = new SolidColorPaint(SKColors.White),
                            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                            DataLabelsFormatter = point => $"{point.Model:N0}",
                            XToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                        }
                    };

                    TopImportYAxes[0].Labels = top5.Select(x => x.Name).ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Supplier Report: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}