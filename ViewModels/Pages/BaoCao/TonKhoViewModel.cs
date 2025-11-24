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
    public partial class TonKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private bool _isInitialized = false;
        private const int LOW_STOCK_THRESHOLD = 10;

        // --- KPI Properties ---
        [ObservableProperty] private int _totalProductsCount;
        [ObservableProperty] private int _totalStockQuantity;
        [ObservableProperty] private decimal _totalStockValue;
        [ObservableProperty] private int _lowStockCount;

        // --- Charts Properties ---
        [ObservableProperty] private ISeries[] _categoryValueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private ISeries[] _topValueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _topValueXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] _topValueYAxes = Array.Empty<Axis>();

        // --- Lists ---
        // Chỉ giữ lại danh sách cần nhập hàng (nhẹ hơn rất nhiều so với load all)
        [ObservableProperty] private ObservableCollection<ProductModel> _lowStockProducts = new();

        [ObservableProperty] private bool _isBusy;

        public TonKhoViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

            // Cấu hình trục Y cho biểu đồ Top (ẩn lưới ngang, hiện label màu xám)
            TopValueYAxes = new Axis[]
            {
                new Axis {
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12,
                    ShowSeparatorLines = true,
                    Labeler = value => value.ToString("N0") // Format số trục Y
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

                // 1. Tính toán KPI trực tiếp từ Database (Nhanh hơn tải list về RAM)
                TotalProductsCount = await db.Products.CountAsync();

                // Dùng SumAsync để tính tổng
                TotalStockQuantity = await db.Products.SumAsync(p => p.InitialQty);

                // Tính tổng giá trị tồn kho (Số lượng * Giá bán)
                TotalStockValue = await db.Products.SumAsync(p => p.InitialQty * p.SalePrice);

                // Đếm số sản phẩm sắp hết
                LowStockCount = await db.Products.CountAsync(p => p.InitialQty <= LOW_STOCK_THRESHOLD);

                // 2. Tải danh sách cảnh báo (Chỉ tải những dòng cần thiết)
                var lowStockItems = await db.Products
                    .AsNoTracking()
                    .Where(p => p.InitialQty <= LOW_STOCK_THRESHOLD)
                    .OrderBy(p => p.InitialQty) // Ưu tiên ít hàng nhất lên đầu
                    .ToListAsync();

                LowStockProducts.Clear();
                foreach (var item in lowStockItems) LowStockProducts.Add(item);

                // 3. Biểu đồ tròn: Phân bổ GIÁ TRỊ theo danh mục
                // GroupBy trên Database
                var catGroups = await db.Products
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new
                    {
                        CategoryName = g.First().Category != null ? g.First().Category!.Name : "Chưa phân loại",
                        TotalVal = g.Sum(p => p.InitialQty * p.SalePrice)
                    })
                    .OrderByDescending(x => x.TotalVal)
                    .ToListAsync();

                // Xử lý dữ liệu biểu đồ (Top 5 + Khác)
                var pieSeries = new List<ISeries>();
                var topCats = catGroups.Take(5).ToList();
                var otherVal = catGroups.Skip(5).Sum(x => x.TotalVal);

                foreach (var c in topCats)
                {
                    pieSeries.Add(new PieSeries<decimal>
                    {
                        Name = c.CategoryName ?? "N/A",
                        Values = new[] { c.TotalVal },
                        // PieSeries dùng ToolTipLabelFormatter
                        ToolTipLabelFormatter = point => $"{point.Context.Series.Name}: {point.Model:N0} đ",
                        DataLabelsFormatter = point =>
                        {
                            // Xử lý null an toàn cho StackedValue
                            var share = point.StackedValue?.Share ?? 0;
                            return $"{share:P0}";
                        },
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White)
                    });
                }

                if (otherVal > 0)
                {
                    pieSeries.Add(new PieSeries<decimal>
                    {
                        Name = "Khác",
                        Values = new[] { otherVal },
                        ToolTipLabelFormatter = point => $"Khác: {point.Model:N0} đ"
                    });
                }
                CategoryValueSeries = pieSeries.ToArray();

                // 4. Biểu đồ cột: Top 5 Sản phẩm giá trị cao nhất
                var topProducts = await db.Products
                    .AsNoTracking()
                    .OrderByDescending(p => p.InitialQty * p.SalePrice)
                    .Take(5)
                    .Select(p => new { p.ProductName, Value = p.InitialQty * p.SalePrice })
                    .ToListAsync();

                TopValueSeries = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Name = "Giá trị",
                        Values = topProducts.Select(x => x.Value).ToArray(),
                        Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                        // Format Tooltip: {Tên Series}: {Giá trị}
                        YToolTipLabelFormatter = point => $"{point.Model:N0} VNĐ"
                    }
                };

                TopValueXAxes = new Axis[]
                {
                    new Axis
                    {
                        // Vẫn gán Labels để Tooltip biết tên cột là gì
                        Labels = topProducts.Select(x => x.ProductName ?? "SP").ToArray(),
                        
                        // NHƯNG: Set LabelsPaint = null để KHÔNG VẼ chữ dưới trục X
                        LabelsPaint = null, 
                        
                        LabelsRotation = 0,
                        TextSize = 0 // Chắc chắn ẩn
                    }
                };
            }
            catch (Exception ex)
            {
                // Log lỗi hoặc thông báo nhẹ
                System.Diagnostics.Debug.WriteLine($"Error loading TonKho data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}