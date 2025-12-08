using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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
    public partial class TaiChinhViewModel : ObservableObject, INavigationAware
    {
        private readonly ApiService _apiService;
        private bool _isInitialized = false;

        /// <summary>
        /// Cờ để phân biệt: đang set Start/EndDate từ preset (code)
        /// hay là do user tự chỉnh trên UI.
        /// </summary>
        private bool _isUpdatingDateRangeFromPreset;

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

        public TaiChinhViewModel(ApiService apiService)
        {
            _apiService = apiService;

            // Khởi tạo khoảng ngày ban đầu theo preset "7 ngày qua"
            UpdateDateRangeFromSelection();
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            YAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("N0"), // Format 1,000,000
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12,
                    ShowSeparatorLines = true
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
                var reportData = await _apiService.GetReportAsync<FinancialReportResponse>(
                    "Reports/Financial", StartDate, EndDate);

                if (reportData != null)
                {
                    // 1. Cập nhật KPI
                    TotalRevenue = reportData.TotalRevenue;
                    TotalCost = reportData.TotalCost;
                    TotalProfit = reportData.TotalProfit;

                    // 2. Cập nhật dữ liệu cho biểu đồ
                    var dates = reportData.DailyStats
                        .Select(x => x.Date.ToString("dd/MM"))
                        .ToArray();

                    var revenueValues = reportData.DailyStats
                        .Select(x => x.Revenue)
                        .ToArray();

                    var costValues = reportData.DailyStats
                        .Select(x => x.Cost)
                        .ToArray();

                    var profitValues = reportData.DailyStats
                        .Select(x => x.Profit)
                        .ToArray();

                    Series = new ISeries[]
                    {
                        new ColumnSeries<decimal>
                        {
                            Name = "Doanh thu",
                            Values = revenueValues,
                            Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                            YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                        },
                        new ColumnSeries<decimal>
                        {
                            Name = "Chi phí",
                            Values = costValues,
                            Fill = new SolidColorPaint(SKColors.IndianRed),
                            YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                        },
                        new LineSeries<decimal>
                        {
                            Name = "Lợi nhuận",
                            Values = profitValues,
                            Fill = null,
                            GeometrySize = 8,
                            Stroke = new SolidColorPaint(SKColors.ForestGreen) { StrokeThickness = 3 },
                            GeometryStroke = new SolidColorPaint(SKColors.ForestGreen) { StrokeThickness = 3 },
                            YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                        }
                    };

                    XAxes = new Axis[]
                    {
                        new Axis
                        {
                            Labels = dates,
                            LabelsRotation = 0,
                            LabelsPaint = new SolidColorPaint(SKColors.Gray),
                            TextSize = 12
                        }
                    };
                }
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

        /// <summary>
        /// Khi đổi preset (7 ngày qua, 1 tháng, 3 tháng, Năm nay, Tùy chỉnh)
        /// </summary>
        partial void OnSelectedTimeRangeIndexChanged(int value)
        {
            // Cập nhật StartDate/EndDate theo preset
            UpdateDateRangeFromSelection();

            // Nếu không phải "Tùy chỉnh" thì auto load
            if (SelectedTimeRangeIndex != 4)
            {
                _ = LoadDataAsync();
            }
            // Nếu là "Tùy chỉnh" thì chỉ đổi index, không tự load.
            // User sẽ chỉnh Start/End rồi mình load trong OnStartDateChanged/OnEndDateChanged.
        }

        /// <summary>
        /// Khi StartDate thay đổi
        /// </summary>
        partial void OnStartDateChanged(DateTime value)
        {
            // Nếu đang update bằng code (preset) thì bỏ qua
            if (_isUpdatingDateRangeFromPreset)
                return;

            // Nếu đang ở preset khác "Tùy chỉnh" → chuyển sang "Tùy chỉnh" rồi LOAD luôn
            if (SelectedTimeRangeIndex != 4)
                SelectedTimeRangeIndex = 4;   // sẽ gọi OnSelectedTimeRangeIndexChanged,
                                              // nhưng nó KHÔNG load vì index == 4

            // Dù là lần đầu hay lần sau, cứ đổi StartDate (do user) là load
            _ = LoadDataAsync();
        }

        /// <summary>
        /// Khi EndDate thay đổi
        /// </summary>
        partial void OnEndDateChanged(DateTime value)
        {
            if (_isUpdatingDateRangeFromPreset)
                return;

            if (SelectedTimeRangeIndex != 4)
                SelectedTimeRangeIndex = 4;

            _ = LoadDataAsync();
        }

        /// <summary>
        /// Set StartDate/EndDate dựa trên SelectedTimeRangeIndex.
        /// Dùng cờ _isUpdatingDateRangeFromPreset để không kích hoạt logic "Tùy chỉnh".
        /// </summary>
        private void UpdateDateRangeFromSelection()
        {
            var now = DateTime.Now;

            _isUpdatingDateRangeFromPreset = true;

            try
            {
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

                    case 4: // Tùy chỉnh → không động vào Start/End
                    default:
                        break;
                }
            }
            finally
            {
                _isUpdatingDateRangeFromPreset = false;
            }
        }
    }
}
