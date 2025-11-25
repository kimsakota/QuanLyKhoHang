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
    public partial class TaiChinhViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private bool _isInitialized = false;

        // --- KPI Properties ---
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private decimal _totalCost;
        [ObservableProperty] private decimal _totalProfit;

        // --- Filter Properties ---
        [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-7);
        [ObservableProperty] private DateTime _endDate = DateTime.Now;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<string> TimeRanges { get; } = new ObservableCollection<string>
        {
            "7 ngày qua",
            "1 tháng qua",
            "3 tháng qua",
            "Năm nay",
            "Tùy chỉnh"
        };

        [ObservableProperty] private int _selectedTimeRangeIndex = 0;

        // --- Chart Properties ---
        [ObservableProperty] private ISeries[] _series = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _yAxes = Array.Empty<Axis>();

        public TaiChinhViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

            UpdateDateRangeFromSelection();

            // Cấu hình trục Y
            YAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("N0"),
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12
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

                var start = StartDate.Date;
                var end = EndDate.Date.AddDays(1).AddTicks(-1);

                // 1. Lấy dữ liệu (Select trước để tối ưu SQL)
                var exports = await db.ExportDetails
                    .Include(d => d.Export)
                    .Where(d => d.Export!.ExportDate >= start && d.Export.ExportDate <= end)
                    .Select(d => new { Date = d.Export!.ExportDate, Total = d.Quantity * d.UnitPrice })
                    .ToListAsync();

                var imports = await db.ImportDetails
                    .Include(d => d.Import)
                    .Where(d => d.Import!.ImportDate >= start && d.Import.ImportDate <= end)
                    .Select(d => new { Date = d.Import!.ImportDate, Total = d.Quantity * d.UnitPrice })
                    .ToListAsync();

                // 2. Tính KPI
                TotalRevenue = exports.Sum(x => x.Total);
                TotalCost = imports.Sum(x => x.Total);
                TotalProfit = TotalRevenue - TotalCost;

                // 3. Xử lý dữ liệu biểu đồ
                var dateRange = Enumerable.Range(0, 1 + end.Subtract(start).Days)
                                          .Select(offset => start.AddDays(offset))
                                          .ToList();

                var revenueData = new List<double>();
                var costData = new List<double>();
                var profitData = new List<double>();
                var labels = new List<string>();

                foreach (var date in dateRange)
                {
                    var rev = (double)exports.Where(x => x.Date.Date == date).Sum(x => x.Total);
                    var cst = (double)imports.Where(x => x.Date.Date == date).Sum(x => x.Total);

                    revenueData.Add(rev);
                    costData.Add(cst);
                    profitData.Add(rev - cst);
                    labels.Add(date.ToString("dd/MM"));
                }

                // 4. Cấu hình Series (Đã sửa TooltipLabelFormatter -> YToolTipLabelFormatter)
                Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Name = "Doanh thu",
                        Values = revenueData,
                        Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                        Stroke = null,
                        // SỬA Ở ĐÂY: Dùng YToolTipLabelFormatter
                        YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                    },
                    new ColumnSeries<double>
                    {
                        Name = "Chi phí",
                        Values = costData,
                        Fill = new SolidColorPaint(SKColors.IndianRed),
                        Stroke = null,
                        // SỬA Ở ĐÂY: Dùng YToolTipLabelFormatter
                        YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                    },
                    new LineSeries<double>
                    {
                        Name = "Lợi nhuận",
                        Values = profitData,
                        Fill = null,
                        GeometrySize = 5,
                        Stroke = new SolidColorPaint(SKColors.ForestGreen) { StrokeThickness = 3 },
                        GeometryStroke = new SolidColorPaint(SKColors.ForestGreen) { StrokeThickness = 3 },
                        // SỬA Ở ĐÂY: Dùng YToolTipLabelFormatter
                        YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                    }
                };

                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = labels,
                        LabelsRotation = 0,
                        LabelsPaint = new SolidColorPaint(SKColors.Gray),
                        TextSize = 12
                    }
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedTimeRangeIndexChanged(int value)
        {
            UpdateDateRangeFromSelection();
            _ = LoadDataAsync();
        }

        partial void OnStartDateChanged(DateTime value)
        {
            if (SelectedTimeRangeIndex != 4 && !IsDateMatchingRange()) // 4 = Tùy chỉnh
                SelectedTimeRangeIndex = 4;
            else
                _ = LoadDataAsync();
        }

        partial void OnEndDateChanged(DateTime value) 
        {
            if (SelectedTimeRangeIndex != 4 && !IsDateMatchingRange()) // 4 = Tùy chỉnh
                SelectedTimeRangeIndex = 4;
            else
                _ = LoadDataAsync();
        }

        private bool IsDateMatchingRange()
        {
            var now = DateTime.Now.Date;
            var start = StartDate.Date;
            var end = EndDate.Date;

            switch(SelectedTimeRangeIndex)
            {
                case 0: // 7 ngày qua
                    return start == now.AddDays(-7) && end == now;
                case 1: // 1 tháng qua
                    return start == now.AddMonths(-1) && end == now;
                case 2: // 3 tháng qua
                    return start == now.AddMonths(-3) && end == now;
                case 3: // Năm nay
                    return start == new DateTime(now.Year, 1, 1) && end == now;
                default:
                    return false;
            }
        }

        private void UpdateDateRangeFromSelection()
        {
            var now = DateTime.Now;
            switch (SelectedTimeRangeIndex)
            {
                case 0: // 7 ngày qua
                    StartDate = now.AddDays(-7);
                    EndDate = now;
                    break;
                case 1: // 1 tháng qua
                    StartDate = now.AddMonths(-1);
                    EndDate = now;
                    break;
                case 2: // 3 tháng qua
                    StartDate = now.AddMonths(-3);
                    EndDate = now;
                    break;
                case 3: // Năm nay
                    StartDate = new DateTime(now.Year, 1, 1);
                    EndDate = now;
                    break;
                default:
                    break;
            }
        }
    }
}