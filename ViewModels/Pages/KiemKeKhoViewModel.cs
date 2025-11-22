using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using UiDesktopApp1.Models;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class KiemKeKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        // --- Tìm kiếm & Chọn ---
        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _suggestedProducts = new();
        [ObservableProperty] private string _productSearchText = string.Empty;
        [ObservableProperty] private ProductModel? _selectedProduct;

        // --- Form nhập liệu ---
        [ObservableProperty] private DateTime _checkDate = DateTime.Now;
        [ObservableProperty] private int _systemQty = 0; // Tồn trên máy
        [ObservableProperty] private int _actualQty = 0; // Tồn thực tế đếm được
        [ObservableProperty] private string _errorMessage = string.Empty;

        // --- Danh sách chi tiết ---
        [ObservableProperty] private ObservableCollection<InventoryCheckDetailModel> _checkDetails = new();

        public KiemKeKhoViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task OnNavigatedToAsync() => await LoadDataAsync();
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadDataAsync()
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                Products.Clear();
                var list = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                foreach (var p in list) Products.Add(p);
                ProductSearchText = string.Empty;
            }
            catch { }
        }

        // --- Tìm kiếm sản phẩm ---
        partial void OnProductSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SuggestedProducts.Clear();
                SelectedProduct = null; // Reset khi xóa hết text
                return;
            }
            var keyword = value.ToLower();

            // 1. Lọc danh sách gợi ý
            var results = Products.Where(p =>
                (p.ProductName?.ToLower().Contains(keyword) ?? false) ||
                (p.ProductCode?.ToLower().Contains(keyword) ?? false)).Take(20);

            SuggestedProducts.Clear();
            foreach (var item in results) SuggestedProducts.Add(item);

            // 2. Tự động chọn sản phẩm nếu tên hoặc mã khớp chính xác
            // (Logic này giúp khi chọn từ AutoSuggestBox, Text thay đổi -> tự động set SelectedProduct)
            var match = Products.FirstOrDefault(p =>
                (p.ProductName?.Equals(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.ProductCode?.Equals(value, StringComparison.OrdinalIgnoreCase) ?? false));

            if (match != null)
            {
                SelectedProduct = match;
            }
            else
            {
                SelectedProduct = null;
                // Reset thông tin nếu không khớp
                SystemQty = 0;
                ActualQty = 0;
            }
        }

        // --- Khi chọn sản phẩm -> Lấy tồn kho hiện tại ---
        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null)
            {
                SystemQty = 0;
                ActualQty = 0;
                return;
            }

            // Cập nhật lại text hiển thị nếu cần (tránh vòng lặp vô tận vì ObservableProperty kiểm tra equality)
            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
            {
                ProductSearchText = value.ProductName ?? string.Empty;
            }

            // Lấy tồn kho realtime từ DB
            Task.Run(async () =>
            {
                try
                {
                    await using var db = await _dbContextFactory.CreateDbContextAsync();
                    var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == value.Id);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SystemQty = p?.InitialQty ?? 0;
                        ActualQty = SystemQty; // Mặc định thực tế = hệ thống
                    });
                }
                catch { }
            });
        }

        [RelayCommand]
        private void AddToList()
        {
            ErrorMessage = string.Empty;
            if (SelectedProduct == null)
            {
                ErrorMessage = "Vui lòng chọn sản phẩm.";
                return;
            }
            if (ActualQty < 0)
            {
                ErrorMessage = "Số lượng thực tế không được âm.";
                return;
            }

            var existing = CheckDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
            if (existing != null)
            {
                // Nếu đã có, cập nhật lại số thực tế
                existing.ActualQty = ActualQty;
                existing.SystemQty = SystemQty; // Cập nhật lại tồn hệ thống lỡ có thay đổi

                // Refresh UI
                int index = CheckDetails.IndexOf(existing);
                CheckDetails.RemoveAt(index);
                CheckDetails.Insert(index, existing);
            }
            else
            {
                CheckDetails.Add(new InventoryCheckDetailModel
                {
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct,
                    SystemQty = SystemQty,
                    ActualQty = ActualQty
                });
            }

            // Reset form sau khi thêm
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            SystemQty = 0;
            ActualQty = 0;
        }

        [RelayCommand]
        private void RemoveItem(InventoryCheckDetailModel item)
        {
            if (CheckDetails.Contains(item)) CheckDetails.Remove(item);
        }

        [RelayCommand]
        private async Task SaveCheckAsync()
        {
            if (CheckDetails.Count == 0) return;

            var confirm = MessageBox.Show($"Lưu phiếu kiểm kê sẽ cập nhật tồn kho của {CheckDetails.Count} sản phẩm theo số liệu thực tế.\nBạn có chắc chắn không?",
                "Xác nhận cân bằng kho", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                using var trans = await db.Database.BeginTransactionAsync();

                try
                {
                    var checkHeader = new InventoryCheckModel
                    {
                        CheckDate = CheckDate,
                        CheckedBy = "Admin", // Thay bằng User hiện tại
                        Details = new List<InventoryCheckDetailModel>()
                    };

                    foreach (var item in CheckDetails)
                    {
                        // 1. Lưu chi tiết phiếu
                        checkHeader.Details.Add(new InventoryCheckDetailModel
                        {
                            ProductId = item.ProductId,
                            SystemQty = item.SystemQty,
                            ActualQty = item.ActualQty
                        });

                        // 2. CẬP NHẬT KHO (Quan trọng)
                        // Tồn kho mới = Số lượng thực tế đếm được
                        var product = await db.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.InitialQty = item.ActualQty;
                        }
                    }

                    db.InventoryChecks.Add(checkHeader);
                    await db.SaveChangesAsync();
                    await trans.CommitAsync();

                    MessageBox.Show("Đã lưu và cân bằng kho thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);

                    CheckDetails.Clear();
                    ProductSearchText = string.Empty;
                    SelectedProduct = null;
                    SystemQty = 0;
                    ActualQty = 0;
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    MessageBox.Show($"Lỗi: {ex.Message}");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}