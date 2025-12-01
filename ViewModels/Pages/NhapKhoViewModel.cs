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
using UiDesktopApp1.ViewModels.Pages.LienHe;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class NhapKhoViewModel : ObservableObject, INavigationAware
    {
        //private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ApiService _apiService;
        private readonly CurrentUserService _currentUserService;
        private readonly NhaCungCapViewModel _nhaCungCapViewModel;
        private bool _isInitialized = false;

        [ObservableProperty] private ObservableCollection<SupplierModel> _suppliers = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();

        [ObservableProperty] private string _supplierSearchText = string.Empty;
        [ObservableProperty] private string _productSearchText = string.Empty;

        [ObservableProperty] private SupplierModel? _selectedSupplier;
        [ObservableProperty] private ProductModel? _selectedProduct;

        [ObservableProperty] private int _inputQuantity = 1;
        [ObservableProperty] private decimal _inputPrice = 0;
        [ObservableProperty] private string _errorMessage = string.Empty;

        [ObservableProperty] private ObservableCollection<ImportDetailModel> _importDetails = new();
        [ObservableProperty] private decimal _totalAmount;

        public NhapKhoViewModel(CurrentUserService currentUserService,
            NhaCungCapViewModel nhaCungCapViewModel,
            ApiService apiService)
        {
            _currentUserService = currentUserService;
            _nhaCungCapViewModel = nhaCungCapViewModel;
            _apiService = apiService;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                RefreshForm(); 
                await LoadDataAsync(); 
                _isInitialized = true;
            }
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadDataAsync()
        {
            try
            {
                //await using var db = await _dbContextFactory.CreateDbContextAsync();

                Suppliers.Clear();
                //var suppliers = await db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
                var suppliers = await _apiService.GetAllAsync<SupplierModel>("Suppliers");
                foreach (var s in suppliers) Suppliers.Add(s);

                Products.Clear();
                //var products = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                var products = await _apiService.GetAllAsync<ProductModel>("Products");
                foreach (var p in products) Products.Add(p);
            }
            catch (Exception ex) { ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}"; }
        }

        // Logic tìm kiếm Nhà cung cấp
        partial void OnSupplierSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedSupplier = null;
                return;
            }
            // Tìm kiếm chính xác hơn
            var match = Suppliers.FirstOrDefault(s => s.Name != null && s.Name.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedSupplier = match;
            else SelectedSupplier = null;
        }

        // Logic tìm kiếm Sản phẩm
        partial void OnProductSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedProduct = null;
                InputPrice = 0;
                return;
            }

            var keyword = value.Trim();
            var match = Products.FirstOrDefault(p =>
                (p.ProductName != null && p.ProductName.Equals(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (p.ProductCode != null && p.ProductCode.Equals(keyword, StringComparison.OrdinalIgnoreCase)));

            if (match != null) SelectedProduct = match;
            else
            {
                SelectedProduct = null;
                InputPrice = 0;
            }
        }

        partial async Task OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null) return;

            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
                ProductSearchText = value.ProductName ?? string.Empty;

            // Lấy giá nhập gần nhất
            await _apiService.GetLastImportPriceAsync<decimal>(value.Id);
        }

        // Đồng bộ lại tên hiển thị khi chọn NCC
        partial void OnSelectedSupplierChanged(SupplierModel? value)
        {
            if (value != null && !string.Equals(SupplierSearchText, value.Name, StringComparison.OrdinalIgnoreCase))
            {
                SupplierSearchText = value.Name ?? string.Empty;
            }
        }

        partial void OnSuppliersChanged(ObservableCollection<SupplierModel>? oldValue, ObservableCollection<SupplierModel> newValue)
        {
            throw new NotImplementedException();
        }
        [RelayCommand]
        private void AddToImportList()
        {
            ErrorMessage = string.Empty;

            if (SelectedProduct == null)
            {
                ErrorMessage = "Vui lòng chọn sản phẩm.";
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
                existingItem.UnitPrice = InputPrice;

                int index = ImportDetails.IndexOf(existingItem);
                ImportDetails.RemoveAt(index);
                ImportDetails.Insert(index, existingItem);
            }
            else
            {
                ImportDetails.Add(new ImportDetailModel
                {
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct,
                    Quantity = InputQuantity,
                    UnitPrice = InputPrice
                });
            }

            ResetInputFields(); // Xóa trắng ô nhập liệu để nhập tiếp
            CalculateTotal();
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
                if(!string.IsNullOrWhiteSpace(SupplierSearchText))
                    MessageBox.Show($"Nhà cung cấp '{SupplierSearchText}' chưa có.", "Xác nhận", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show("Vui lòng chọn Nhà cung cấp.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ImportDetails.Count == 0)
            {
                MessageBox.Show("Danh sách nhập kho đang trống. Vui lòng thêm sản phẩm.", "Chưa có hàng hóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirmation
            var confirm = MessageBox.Show($"Xác nhận nhập kho {ImportDetails.Count} mặt hàng?\nTổng tiền: {TotalAmount:N0} đ",
                                          "Xác nhận nhập kho", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            // Processing
            try
            {
                //await using var db = await _dbContextFactory.CreateDbContextAsync();
                //using var transaction = await db.Database.BeginTransactionAsync();

                var newImport = new ImportModel
                {
                    SupplierId = SelectedSupplier.Id,
                    ImportDate = DateTime.Now,
                    ImportedBy = _currentUserService.CurrentUser?.Username ?? "Unknown", // Lưu Username
                    ImportDetails = ImportDetails.Select(d => new ImportDetailModel
                    {
                        ProductId = d.ProductId,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                    }).ToList()
                };

                /*foreach (var item in ImportDetails)
                {
                    newImport.ImportDetails.Add(new ImportDetailModel
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });

                    // Cập nhật tồn kho: Cộng thêm
                    var productInDb = await db.Products.FindAsync(item.ProductId);
                    if (productInDb != null)
                    {
                        productInDb.InitialQty += item.Quantity;
                    }
                }*/

                var transaction = await _apiService.AddAsync<ImportModel>("Imports", newImport);

                

                MessageBox.Show("Lưu phiếu nhập thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshForm(); // Làm mới trang sau khi lưu thành công
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper: Xóa trắng toàn bộ form (dùng khi mới vào trang hoặc sau khi Lưu)
        private void RefreshForm()
        {
            ImportDetails.Clear();
            TotalAmount = 0;

            // Thông tin Header
            SelectedSupplier = null;
            SupplierSearchText = string.Empty;
            ErrorMessage = string.Empty;

            // Thông tin Input
            ResetInputFields();
        }

        // Helper: Chỉ xóa các ô nhập liệu sản phẩm (dùng sau khi Thêm vào danh sách)
        private void ResetInputFields()
        {
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            InputQuantity = 1;
            InputPrice = 0;
        }

        [RelayCommand]
        private async Task QuickAddSupplier()
        {
            var newSupplier = await _nhaCungCapViewModel.AddFromExternalAsync();

            if(newSupplier != null)
            {
                if(!Suppliers.Any(s => s.Id == newSupplier.Id))
                    Suppliers.Add(newSupplier);

                SelectedSupplier = newSupplier;
                SupplierSearchText = newSupplier.Name ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task RefreshDataAsync() 
        {
            var ask = MessageBox.Show("Làm mới dữ liệu sẽ xóa toàn bộ thông tin đang nhập. Tiếp tục?", 
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ask != MessageBoxResult.Yes) return;
            RefreshForm(); 
            await LoadDataAsync();
        }
    }
}