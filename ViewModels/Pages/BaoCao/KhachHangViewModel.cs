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
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.BaoCao
{
    public partial class KhachHangViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
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

        // --- List (Top Customers Detail) ---
        // Tạo class DTO nội bộ hoặc dùng anonymous type mapped sang ViewModel con nếu cần
        // Ở đây ta dùng một Model wrapper hoặc Model mở rộng nếu muốn hiển thị doanh thu
        public class CustomerReportDto
        {
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public int OrderCount { get; set; }
            public decimal TotalSpent { get; set; }
        }

        [ObservableProperty] private ObservableCollection<CustomerReportDto> _topCustomers = new();

        public KhachHangViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

            // Cấu hình trục X cho biểu đồ nằm ngang (Giá trị tiền)
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

            // Cấu hình trục Y cho biểu đồ nằm ngang (Tên khách hàng)
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
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Chuẩn hóa thời gian
                var start = StartDate.Date;
                var end = EndDate.Date.AddDays(1).AddTicks(-1);

                // 1. Tính KPIs
                TotalCustomers = await db.Customers.CountAsync();

                // Giả sử ta coi khách hàng có đơn hàng đầu tiên trong kỳ là khách mới
                // Hoặc đơn giản là đếm số khách mua hàng trong kỳ (Active)
                // Ở đây ta lấy số lượng khách hàng CÓ GIAO DỊCH XUẤT KHO trong kỳ
                var activeCustomerIds = await db.Exports
                    .Where(e => e.ExportDate >= start && e.ExportDate <= end && e.CustomerId != null)
                    .Select(e => e.CustomerId)
                    .Distinct()
                    .ToListAsync();

                ActiveCustomers = activeCustomerIds.Count;

                // Tính tổng doanh thu trong kỳ
                var revenueQuery = db.ExportDetails
                    .Include(d => d.Export)
                    .Where(d => d.Export!.ExportDate >= start && d.Export.ExportDate <= end);

                TotalRevenueInPeriod = await revenueQuery.SumAsync(d => d.Quantity * d.UnitPrice);

                // 2. Top Khách hàng chi tiêu nhiều nhất (Top Spending)
                var customerStats = await db.Exports
                    .Where(e => e.ExportDate >= start && e.ExportDate <= end && e.CustomerId != null)
                    .GroupBy(e => e.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        OrderCount = g.Count(),
                        // Tính tổng tiền: Join sang ExportDetail
                        TotalSpent = g.SelectMany(e => e.ExportDetails).Sum(d => d.Quantity * d.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10) // Lấy top 10
                    .ToListAsync();

                // Lấy thông tin chi tiết tên khách hàng
                var topCustomerIds = customerStats.Select(x => x.CustomerId).ToList();
                var customersInfo = await db.Customers
                    .Where(c => topCustomerIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c);

                // Map dữ liệu vào danh sách hiển thị
                TopCustomers.Clear();
                var chartValues = new List<decimal>();
                var chartLabels = new List<string>();

                foreach (var stat in customerStats)
                {
                    if (stat.CustomerId == null || !customersInfo.ContainsKey(stat.CustomerId.Value)) continue;

                    var cus = customersInfo[stat.CustomerId.Value];
                    var name = cus.Name ?? "Khách lẻ";

                    // Add vào List
                    TopCustomers.Add(new CustomerReportDto
                    {
                        Name = name,
                        Phone = cus.PhoneNumber ?? "--",
                        OrderCount = stat.OrderCount,
                        TotalSpent = stat.TotalSpent
                    });

                    // Data cho Chart (Lấy Top 5 thôi cho đẹp)
                    if (chartValues.Count < 5)
                    {
                        chartValues.Add(stat.TotalSpent);
                        chartLabels.Add(name);
                    }
                }

                // New Customers (Logic tạm: Khách chưa từng mua trước ngày Start, và có mua trong kỳ)
                // Logic này hơi phức tạp với schema hiện tại, ta có thể thay bằng "Số đơn hàng"
                NewCustomers = await db.Exports.CountAsync(e => e.ExportDate >= start && e.ExportDate <= end);


                // 3. Cấu hình Biểu đồ (RowSeries - Cột ngang)
                // Lưu ý: RowSeries vẽ từ dưới lên, nên ta cần đảo ngược danh sách để Top 1 nằm trên cùng
                chartValues.Reverse();
                chartLabels.Reverse();

                TopSpendingSeries = new ISeries[]
                {
                    new RowSeries<decimal>
                    {
                        Name = "Chi tiêu",
                        Values = chartValues.ToArray(),
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

                TopSpendingYAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = chartLabels.ToArray(),
                        LabelsPaint = new SolidColorPaint(SKColors.Black),
                        TextSize = 13
                    }
                };
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