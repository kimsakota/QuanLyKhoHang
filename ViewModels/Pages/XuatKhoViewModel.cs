using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
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
    public partial class XuatKhoViewModel : ObservableObject, INavigationAware
    {
        //private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ApiService _apiService;
        private readonly CurrentUserService _currentUserService;
        private readonly KhachHangViewModel _khachHangViewModel;
        private bool _isInitialized = false;

        [ObservableProperty] private ObservableCollection<CustomerModel> _customers = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();

        [ObservableProperty] private string _customerSearchText = string.Empty;
        [ObservableProperty] private string _productSearchText = string.Empty;

        [ObservableProperty] private CustomerModel? _selectedCustomer;
        [ObservableProperty] private ProductModel? _selectedProduct;

        [ObservableProperty] private int _inputQuantity = 1;
        [ObservableProperty] private decimal _inputPrice = 0;
        [ObservableProperty] private int _currentStock = 0;
        [ObservableProperty] private string _errorMessage = string.Empty;

        [ObservableProperty] private ObservableCollection<ExportDetailModel> _exportDetails = new();
        [ObservableProperty] private decimal _totalAmount;

        public XuatKhoViewModel(ApiService apiService,
            CurrentUserService currentUserService,
            KhachHangViewModel khachHangViewModel)
        {
            _apiService = apiService;
            _currentUserService = currentUserService;
            _khachHangViewModel = khachHangViewModel;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                RefreshForm(); // Reset toàn bộ form khi vào trang
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

                Customers.Clear();
                //var listCus = await db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
                var customers = await _apiService.GetAllAsync<CustomerModel>("Exports");
                foreach (var c in customers) Customers.Add(c);

                Products.Clear();
                //var listPro = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                var products = await _apiService.GetAllAsync<ProductModel>("Products");
                foreach (var p in products) Products.Add(p);
            }
            catch (Exception ex) { ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}"; }
        }

        // Logic tìm kiếm Khách hàng
        partial void OnCustomerSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedCustomer = null;
                return;
            }
            var match = Customers.FirstOrDefault(c => c.Name != null && c.Name.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedCustomer = match;
            else SelectedCustomer = null;
        }

        // Logic tìm kiếm Sản phẩm
        partial void OnProductSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedProduct = null;
                InputPrice = 0;
                CurrentStock = 0;
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
                CurrentStock = 0;
            }
        }

        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null) return;

            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
                ProductSearchText = value.ProductName ?? string.Empty;

            // Lấy giá bán & Tồn kho
            Task.Run(async () =>
            {
                try
                {
                    //await using var db = await _dbContextFactory.CreateDbContextAsync();
                    var realTimeProduct = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == value.Id);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (realTimeProduct != null)
                        {
                            InputPrice = realTimeProduct.SalePrice;
                            CurrentStock = realTimeProduct.InitialQty;
                        }
                    });
                }
                catch { }
            });
        }


        partial void OnSelectedCustomerChanged(CustomerModel? value)
        {
            if (value != null && !string.Equals(CustomerSearchText, value.Name, StringComparison.OrdinalIgnoreCase))
            {
                CustomerSearchText = value.Name ?? string.Empty;
            }
        }

        [RelayCommand]
        private void AddToExportList()
        {
            ErrorMessage = string.Empty;

            if (SelectedProduct == null)
            {
                ErrorMessage = "Vui lòng chọn sản phẩm.";
                return;
            }
            if (InputQuantity <= 0)
            {
                ErrorMessage = "Số lượng xuất phải lớn hơn 0.";
                return;
            }

            // Kiểm tra tồn kho tạm tính
            var pendingQty = ExportDetails.Where(x => x.ProductId == SelectedProduct.Id).Sum(x => x.Quantity);
            if ((pendingQty + InputQuantity) > CurrentStock)
            {
                ErrorMessage = $"Không đủ hàng! Tồn kho: {CurrentStock}, Đã chọn: {pendingQty}, Muốn xuất thêm: {InputQuantity}";
                return;
            }

            var existingItem = ExportDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += InputQuantity;
                existingItem.UnitPrice = InputPrice;

                int index = ExportDetails.IndexOf(existingItem);
                ExportDetails.RemoveAt(index);
                ExportDetails.Insert(index, existingItem);
            }
            else
            {
                ExportDetails.Add(new ExportDetailModel
                {
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct,
                    Quantity = InputQuantity,
                    UnitPrice = InputPrice
                });
            }

            ResetInputFields();
            CalculateTotal();
        }

        [RelayCommand]
        private void RemoveItem(ExportDetailModel item)
        {
            if (ExportDetails.Contains(item))
            {
                ExportDetails.Remove(item);
                CalculateTotal();
            }
        }

        private void CalculateTotal() => TotalAmount = ExportDetails.Sum(x => x.Quantity * x.UnitPrice);

        [RelayCommand]
        private async Task SaveExportAsync()
        {
            if (SelectedCustomer == null)
            {
                if (!string.IsNullOrWhiteSpace(CustomerSearchText))
                    MessageBox.Show($"Khách hàng '{CustomerSearchText}' chưa có.", "Xác nhận", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show("Vui lòng chọn Khách hàng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ExportDetails.Count == 0)
            {
                MessageBox.Show("Danh sách xuất kho đang trống. Vui lòng thêm sản phẩm.", "Chưa có hàng hóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirmation
            var confirm = MessageBox.Show($"Xác nhận xuất kho {ExportDetails.Count} mặt hàng?\nTổng tiền: {TotalAmount:N0} đ",
                                          "Xác nhận xuất kho", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            // Processing
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                using var trans = await db.Database.BeginTransactionAsync();

                try
                {
                    if (SelectedCustomer.Id == 0) // Lưu khách mới
                    {
                        db.Customers.Add(SelectedCustomer);
                        await db.SaveChangesAsync();
                    }

                    var newExport = new ExportModel
                    {
                        CustomerId = SelectedCustomer.Id,
                        ExportDate = DateTime.Now,
                        ExportedBy = _currentUserService.CurrentUser?.Username ?? "Unknown", // Lưu Username
                        ExportDetails = new List<ExportDetailModel>()
                    };

                    foreach (var item in ExportDetails)
                    {
                        var productInDb = await db.Products.FindAsync(item.ProductId);
                        if (productInDb == null) throw new Exception($"Sản phẩm {item.Product?.ProductCode} không tồn tại.");

                        if (productInDb.InitialQty < item.Quantity)
                            throw new Exception($"Sản phẩm '{productInDb.ProductName}' không đủ hàng. Tồn: {productInDb.InitialQty}");

                        // Cập nhật tồn kho: Trừ đi
                        productInDb.InitialQty -= item.Quantity;

                        newExport.ExportDetails.Add(new ExportDetailModel
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });
                    }

                    db.Exports.Add(newExport);
                    await db.SaveChangesAsync();
                    await trans.CommitAsync();

                    MessageBox.Show("Xuất kho thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshForm();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshForm()
        {
            ExportDetails.Clear();
            TotalAmount = 0;

            SelectedCustomer = null;
            CustomerSearchText = string.Empty;
            ErrorMessage = string.Empty;

            ResetInputFields();
        }

        private void ResetInputFields()
        {
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            InputQuantity = 1;
            InputPrice = 0;
            CurrentStock = 0;
        }

        [RelayCommand]
        private async Task QuickAddCustomer()
        {
            var newCustomer = await _khachHangViewModel.AddFromExternalAsync();
            if (newCustomer != null)
            {
                if(!Customers.Any(c => c.Id == newCustomer.Id))
                    Customers.Add(newCustomer);
                SelectedCustomer = newCustomer;
                CustomerSearchText = newCustomer.Name ?? string.Empty;
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