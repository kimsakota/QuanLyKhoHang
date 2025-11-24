using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services; // Thêm namespace này
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class KiemKeKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CurrentUserService _currentUserService; 

        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _suggestedProducts = new();
        [ObservableProperty] private string _productSearchText = string.Empty;
        [ObservableProperty] private ProductModel? _selectedProduct;

        [ObservableProperty] private DateTime _checkDate;
        [ObservableProperty] private int _systemQty = 0;
        [ObservableProperty] private int _actualQty = 0;
        [ObservableProperty] private string _errorMessage = string.Empty;

        [ObservableProperty] private ObservableCollection<InventoryCheckDetailModel> _checkDetails = new();

        // Cập nhật Constructor
        public KiemKeKhoViewModel(IDbContextFactory<AppDbContext> dbContextFactory, CurrentUserService currentUserService)
        {
            _dbContextFactory = dbContextFactory;
            _currentUserService = currentUserService;
        }

        public async Task OnNavigatedToAsync()
        {
            RefreshForm();
            await LoadDataAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadDataAsync()
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                Products.Clear();
                // AsNoTracking để tối ưu
                var list = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                foreach (var p in list) Products.Add(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách sản phẩm: " + ex.Message);
            }
        }

        partial void OnProductSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SuggestedProducts.Clear();
                SelectedProduct = null;
                return;
            }

            var keyword = value.Trim(); // Trim khoảng trắng

            // Logic lọc gợi ý
            SuggestedProducts.Clear();
            var results = Products.Where(p =>
                (p.ProductName != null && p.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (p.ProductCode != null && p.ProductCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .Take(20); // Chỉ lấy 20 kết quả đầu để nhẹ giao diện

            foreach (var item in results) SuggestedProducts.Add(item);

            // Logic tự động chọn nếu khớp hoàn toàn
            var match = Products.FirstOrDefault(p =>
                string.Equals(p.ProductName, keyword, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.ProductCode, keyword, StringComparison.OrdinalIgnoreCase));

            if (match != null) SelectedProduct = match;
            else
            {
                // Nếu không khớp thì reset số liệu
                if (SelectedProduct != null) SelectedProduct = null; // Chỉ set null nếu đang có giá trị để tránh loop
                SystemQty = 0;
                ActualQty = 0;
            }
        }

        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null)
            {
                SystemQty = 0;
                ActualQty = 0;
                return;
            }

            // Đồng bộ text hiển thị
            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
                ProductSearchText = value.ProductName ?? string.Empty;

            // Lấy tồn kho realtime
            Task.Run(async () =>
            {
                try
                {
                    await using var db = await _dbContextFactory.CreateDbContextAsync();
                    var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == value.Id);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SystemQty = p?.InitialQty ?? 0;
                        ActualQty = SystemQty; // Mặc định thực tế bằng hệ thống
                    });
                }
                catch { }
            });
        }

        [RelayCommand]
        private void AddToList()
        {
            ErrorMessage = string.Empty;
            if (SelectedProduct == null) { ErrorMessage = "Vui lòng chọn sản phẩm."; return; }
            if (ActualQty < 0) { ErrorMessage = "Số lượng thực tế không được âm."; return; }

            var existing = CheckDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
            if (existing != null)
            {
                existing.ActualQty = ActualQty;
                existing.SystemQty = SystemQty; // Cập nhật lại tồn hệ thống nếu có thay đổi

                // Hack để UI cập nhật
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

            // Reset vùng nhập liệu
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
            if (CheckDetails.Count == 0)
            {
                MessageBox.Show("Danh sách đang trống. Vui lòng thêm sản phẩm.", "Chưa có hàng hóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = $"Hành động này sẽ CẬP NHẬT LẠI TỒN KHO của {CheckDetails.Count} sản phẩm theo số liệu thực tế bạn đã nhập.\n\n" +
                  "Dữ liệu tồn kho cũ sẽ bị thay thế. Bạn có chắc chắn muốn tiếp tục?";

            var confirm = MessageBox.Show($"Lưu phiếu kiểm kê sẽ cập nhật tồn kho của {CheckDetails.Count} sản phẩm.\nBạn có chắc chắn không?",
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
                        CheckDate = DateTime.Now, // Lấy thời gian thực lúc lưu
                        CheckedBy = _currentUserService.CurrentUser?.Username ?? "Unknown",
                        Notes = "Kiểm kê kho định kỳ",
                        Details = new List<InventoryCheckDetailModel>()
                    };

                    foreach (var item in CheckDetails)
                    {
                        checkHeader.Details.Add(new InventoryCheckDetailModel
                        {
                            ProductId = item.ProductId,
                            SystemQty = item.SystemQty,
                            ActualQty = item.ActualQty
                        });

                        // Cập nhật tồn kho sản phẩm
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

                    RefreshForm();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    MessageBox.Show($"Lỗi chi tiết: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshForm()
        {
            CheckDetails.Clear();
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            ErrorMessage = string.Empty;
            SystemQty = 0;
            ActualQty = 0;
        }
    }
}