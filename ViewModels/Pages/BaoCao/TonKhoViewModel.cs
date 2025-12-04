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
using UiDesktopApp1.DTOs;
using UiDesktopApp1.Services;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.BaoCao
{
    public partial class TonKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly ApiService _apiService;
        private bool _isInitialized = false;

        // --- KPIs ---
        [ObservableProperty] private int _totalProductsCount;
        [ObservableProperty] private int _totalStockQuantity;
        [ObservableProperty] private decimal _totalStockValue;
        [ObservableProperty] private int _lowStockCount;

        // --- Danh sách cảnh báo ---
        [ObservableProperty] private ObservableCollection<LowStockProductDto> _lowStockProducts = new();

        // --- Biểu đồ ---
        [ObservableProperty] private ISeries[] _categoryValueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private ISeries[] _topValueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _topValueXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _topValueYAxes = Array.Empty<Axis>();

        [ObservableProperty] private bool _isBusy;

        public TonKhoViewModel(ApiService apiService)
        {
            _apiService = apiService;
            InitializeChartAxes();
        }

        private void InitializeChartAxes()
        {
            // Cấu hình trục Y cho biểu đồ Top (ẩn lưới ngang, hiện số format N0)
            TopValueYAxes = new Axis[]
            {
                new Axis {
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12,
                    ShowSeparatorLines = true,
                    Labeler = value => value.ToString("N0")
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
                // Gọi API lấy toàn bộ dữ liệu báo cáo
                var reportData = await _apiService.GetAsync<InventoryReportResponse>("Reports/Inventory");

                if (reportData != null)
                {
                    // 1. Cập nhật KPIs
                    TotalProductsCount = reportData.TotalProductsCount;
                    TotalStockQuantity = reportData.TotalStockQuantity;
                    TotalStockValue = reportData.TotalStockValue;
                    LowStockCount = reportData.LowStockCount;

                    // 2. Cập nhật danh sách cảnh báo nhập hàng
                    LowStockProducts.Clear();
                    foreach (var item in reportData.LowStockProducts)
                    {
                        LowStockProducts.Add(item);
                    }

                    // 3. Vẽ biểu đồ Tròn (Category)
                    var pieSeries = new List<ISeries>();
                    foreach (var item in reportData.CategoryValueChart)
                    {
                        pieSeries.Add(new PieSeries<decimal>
                        {
                            Name = item.Label,
                            Values = new[] { item.Value },
                            ToolTipLabelFormatter = point => $"{point.Context.Series.Name}: {point.Model:N0} đ",
                            DataLabelsFormatter = point => $"{point.StackedValue!.Share:P0}",
                            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                            DataLabelsPaint = new SolidColorPaint(SKColors.White)
                        });
                    }
                    CategoryValueSeries = pieSeries.ToArray();

                    // 4. Vẽ biểu đồ Cột (Top Products)
                    TopValueSeries = new ISeries[]
                    {
                        new ColumnSeries<decimal>
                        {
                            Name = "Giá trị tồn",
                            Values = reportData.TopValueProductChart.Select(x => x.Value).ToArray(),
                            Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                            YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                        }
                    };

                    TopValueXAxes = new Axis[]
                    {
                        new Axis
                        {
                            Labels = reportData.TopValueProductChart.Select(x => x.Label).ToArray(),
                            LabelsRotation = 0,
                            // Ẩn chữ dưới trục X nếu tên sản phẩm quá dài, chỉ hiện khi hover
                            // LabelsPaint = null 
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải báo cáo tồn kho: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}