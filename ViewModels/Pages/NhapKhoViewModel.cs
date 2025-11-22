using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class NhapKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CurrentUserService _currentUserService; // Inject service
        private readonly IContentDialogService _contentDialogService;

        [ObservableProperty] private ObservableCollection<SupplierModel> _suppliers = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();

        [ObservableProperty] private string _supplierSearchText = string.Empty;
        [ObservableProperty] private string _productSearchText = string.Empty;

        [ObservableProperty] private SupplierModel? _selectedSupplier;
        [ObservableProperty] private ProductModel? _selectedProduct;

        [ObservableProperty] private DateTime _importDate = DateTime.Now;
        [ObservableProperty] private int _inputQuantity = 1;
        [ObservableProperty] private decimal _inputPrice = 0;
        [ObservableProperty] private string _errorMessage = string.Empty;

        [ObservableProperty] private ObservableCollection<ImportDetailModel> _importDetails = new();
        [ObservableProperty] private decimal _totalAmount;

        // Cập nhật Constructor để nhận CurrentUserService
        public NhapKhoViewModel(IDbContextFactory<AppDbContext> dbContextFactory, 
            CurrentUserService currentUserService,
            IContentDialogService contentDialogService)
        {
            _dbContextFactory = dbContextFactory;
            _currentUserService = currentUserService;
            _contentDialogService = contentDialogService;
        }

        public async Task OnNavigatedToAsync()
        {
            ImportDate = DateTime.Now; // Cập nhật giờ mới nhất khi vào trang
            await LoadDataAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadDataAsync()
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                Suppliers.Clear();
                // AsNoTracking giúp tải nhanh hơn cho danh sách chỉ đọc
                var suppliers = await db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
                foreach (var s in suppliers) Suppliers.Add(s);

                Products.Clear();
                var products = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                foreach (var p in products) Products.Add(p);
                
                // Reset các trường tìm kiếmd
                SupplierSearchText = string.Empty;
                ProductSearchText = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
        }

        partial void OnSupplierSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedSupplier = null;
                return;
            }

            var match = Suppliers.FirstOrDefault(s => s.Name != null && s.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedSupplier = match;
            else SelectedSupplier = null;
        }

        partial void OnProductSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedProduct = null;
                InputPrice = 0;
                return;
            }

            var match = Products.FirstOrDefault(p =>
                (p.ProductName != null && p.ProductName.Equals(value, StringComparison.OrdinalIgnoreCase)) ||
                (p.ProductCode != null && p.ProductCode.Equals(value, StringComparison.OrdinalIgnoreCase)));

            if (match != null) SelectedProduct = match;
            else
            {
                SelectedProduct = null;
                InputPrice = 0;
            }
        }

        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null) return;

            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
                ProductSearchText = value.ProductName ?? string.Empty;

            // Lấy giá nhập cũ nhất trong nền
            Task.Run(async () =>
            {
                try
                {
                    await using var db = await _dbContextFactory.CreateDbContextAsync();
                    var lastImport = await db.ImportDetails
                        .Include(d => d.Import)
                        .Where(d => d.ProductId == value.Id)
                        .OrderByDescending(d => d.Import!.ImportDate)
                        .FirstOrDefaultAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Nếu có giá cũ thì lấy, không thì lấy 70% giá bán
                        InputPrice = lastImport != null ? lastImport.UnitPrice : (value.SalePrice * 0.7m);
                    });
                }
                catch { }
            });
        }

        [RelayCommand]
        private void AddToImportList()
        {
            ErrorMessage = string.Empty;

            if (SelectedProduct == null)
            {
                ErrorMessage = "Vui lòng chọn sản phẩm cần nhập.";
                return;
            }
            if (InputQuantity <= 0)
            {
                ErrorMessage = "Số lượng nhập phải lớn hơn 0.";
                return;
            }

            var existingItem = ImportDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += InputQuantity;
                existingItem.UnitPrice = InputPrice; // Cập nhật giá mới nhất

                // Refresh UI dòng đó
                int index = ImportDetails.IndexOf(existingItem);
                ImportDetails.RemoveAt(index);
                ImportDetails.Insert(index, existingItem);
            }
            else
            {
                var newItem = new ImportDetailModel
                {
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct,
                    Quantity = InputQuantity,
                    UnitPrice = InputPrice
                };
                ImportDetails.Add(newItem);
            }

            ResetInputFields();
            CalculateTotal();
        }

        private void ResetInputFields()
        {
            InputQuantity = 1;
            InputPrice = 0;
            SelectedProduct = null;
            ProductSearchText = string.Empty;
        }

        [RelayCommand]
        private void RemoveItem(ImportDetailModel item)
        {
            if (ImportDetails.Contains(item))
            {
                ImportDetails.Remove(item);
                CalculateTotal();
            }
        }

        private void CalculateTotal() => TotalAmount = ImportDetails.Sum(x => x.Quantity * x.UnitPrice);

        [RelayCommand]
        private async Task SaveImportAsync()
        {
            
            if (SelectedSupplier == null)
            {
                if (!string.IsNullOrWhiteSpace(SupplierSearchText))
                {
                    var ask = MessageBox.Show($"Nhà cung cấp \"{SupplierSearchText}\" chưa có trong hệ thống. Bạn có muốn tạo mới?",
                                              "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if(ask == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await using var db = await _dbContextFactory.CreateDbContextAsync();
                            var newSupplier = new SupplierModel
                            {
                                Name = SupplierSearchText
                            };
                            db.Suppliers.Add(newSupplier);
                            await db.SaveChangesAsync();
                            Suppliers.Add(newSupplier);
                            SelectedSupplier = newSupplier;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lỗi khi tạo nhà cung cấp mới: {ex.Message}",
                                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        return; // Người dùng không muốn tạo mới, dừng lưu
                    }
                }
                else
                {
                    MessageBox.Show("Bạn chưa chọn Nhà cung cấp. Vui lòng chọn để tiếp tục.",
                                "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            if (ImportDetails.Count == 0)
            {
                MessageBox.Show("Danh sách nhập kho đang trống. Vui lòng thêm ít nhất một sản phẩm.",
                                "Chưa có hàng hóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Xác nhận nhập kho {ImportDetails.Count} mặt hàng?\nTổng tiền: {TotalAmount:N0} đ",
                                          "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    var newImport = new ImportModel
                    {
                        SupplierId = SelectedSupplier.Id,
                        ImportDate = ImportDate,
                        ImportedBy = _currentUserService.CurrentUser?.Username ?? "Unknown",
                        ImportDetails = new List<ImportDetailModel>()
                    };

                    foreach (var item in ImportDetails)
                    {
                        newImport.ImportDetails.Add(new ImportDetailModel
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });

                        var productInDb = await db.Products.FindAsync(item.ProductId);
                        if (productInDb != null)
                            productInDb.InitialQty += item.Quantity;
                    }

                    db.Imports.Add(newImport);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    MessageBox.Show("Lưu phiếu nhập thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshPage();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshPage()
        {
            ImportDetails.Clear();
            SelectedSupplier = null;
            SupplierSearchText = string.Empty;
            ResetInputFields();
            TotalAmount = 0;
            ErrorMessage = string.Empty;
            ImportDate = DateTime.Now;
        }
    }
}