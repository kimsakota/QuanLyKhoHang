using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using UiDesktopApp1.Models;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class XuatKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        // --- Danh sách nguồn ---
        [ObservableProperty] private ObservableCollection<CustomerModel> _customers = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();

        // --- Search Text ---
        [ObservableProperty] private string _customerSearchText = string.Empty;
        [ObservableProperty] private string _productSearchText = string.Empty;

        // --- Selection ---
        [ObservableProperty] private CustomerModel? _selectedCustomer;
        [ObservableProperty] private ProductModel? _selectedProduct;

        // --- Thông tin nhập liệu ---
        [ObservableProperty] private DateTime _exportDate = DateTime.Now;
        [ObservableProperty] private int _inputQuantity = 1;
        [ObservableProperty] private decimal _inputPrice = 0;
        [ObservableProperty] private int _currentStock = 0; // Hiển thị tồn kho hiện tại
        [ObservableProperty] private string _errorMessage = string.Empty;

        // --- Grid Data ---
        [ObservableProperty] private ObservableCollection<ExportDetailModel> _exportDetails = new();
        [ObservableProperty] private decimal _totalAmount;

        public XuatKhoViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
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

                Customers.Clear();
                var listCus = await db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
                foreach (var c in listCus) Customers.Add(c);

                Products.Clear();
                var listPro = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                foreach (var p in listPro) Products.Add(p);

                CustomerSearchText = string.Empty;
                ProductSearchText = string.Empty;
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
        }

        // Logic tìm kiếm Khách hàng
        partial void OnCustomerSearchTextChanged(string value)
        {
            var match = Customers.FirstOrDefault(c => c.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedCustomer = match;
            else SelectedCustomer = null;
        }

        // Logic tìm kiếm Sản phẩm
        partial void OnProductSearchTextChanged(string value)
        {
            var match = Products.FirstOrDefault(p =>
                p.ProductName.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCode.Equals(value, StringComparison.OrdinalIgnoreCase));

            if (match != null) SelectedProduct = match;
            else
            {
                SelectedProduct = null;
                InputPrice = 0;
                CurrentStock = 0;
            }
        }

        // Khi chọn sản phẩm -> Lấy giá bán & Tồn kho thực tế
        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null) return;

            if (ProductSearchText != value.ProductName)
                ProductSearchText = value.ProductName;

            // Lấy dữ liệu mới nhất từ DB để đảm bảo tồn kho chính xác
            Task.Run(async () =>
            {
                try
                {
                    await using var db = await _dbContextFactory.CreateDbContextAsync();
                    var realTimeProduct = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == value.Id);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (realTimeProduct != null)
                        {
                            InputPrice = realTimeProduct.SalePrice; // Giá bán
                            CurrentStock = realTimeProduct.InitialQty; // Tồn kho
                        }
                    });
                }
                catch { }
            });
        }

        partial void OnSelectedCustomerChanged(CustomerModel? value)
        {
            if (value != null) CustomerSearchText = value.Name;
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

            // --- KIỂM TRA TỒN KHO ---
            // Tính tổng số lượng đang chờ xuất trong lưới của sản phẩm này
            var pendingQty = ExportDetails.Where(x => x.ProductId == SelectedProduct.Id).Sum(x => x.Quantity);

            if (InputQuantity <= 0)
            {
                ErrorMessage = "Số lượng xuất phải lớn hơn 0.";
                return;
            }

            if ((pendingQty + InputQuantity) > CurrentStock)
            {
                ErrorMessage = $"Không đủ hàng! Tồn kho: {CurrentStock}, Đã chọn: {pendingQty}, Muốn xuất thêm: {InputQuantity}";
                return;
            }
            // ------------------------

            var existingItem = ExportDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += InputQuantity;
                existingItem.UnitPrice = InputPrice;

                var index = ExportDetails.IndexOf(existingItem);
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

            InputQuantity = 1;
            ProductSearchText = string.Empty;
            SelectedProduct = null;
            CurrentStock = 0;
            InputPrice = 0;
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
                // Logic tạo nhanh khách hàng nếu chưa có (tương tự nhập kho)
                if (!string.IsNullOrWhiteSpace(CustomerSearchText))
                {
                    var ask = MessageBox.Show($"Khách hàng '{CustomerSearchText}' chưa có. Tạo mới?", "Xác nhận", MessageBoxButton.YesNo);
                    if (ask == MessageBoxResult.Yes)
                        SelectedCustomer = new CustomerModel { Name = CustomerSearchText, PhoneNumber = "", Address = "" };
                    else return;
                }
                else
                {
                    MessageBox.Show("Chưa chọn khách hàng."); return;
                }
            }

            if (ExportDetails.Count == 0) return;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                using var trans = await db.Database.BeginTransactionAsync();

                try
                {
                    // Lưu khách hàng mới nếu cần
                    if (SelectedCustomer.Id == 0)
                    {
                        db.Customers.Add(SelectedCustomer);
                        await db.SaveChangesAsync();
                    }

                    var newExport = new ExportModel
                    {
                        CustomerId = SelectedCustomer.Id,
                        ExportDate = ExportDate,
                        ExportName = "Xuất bán hàng", // Hoặc binding từ UI nếu có ô ghi chú
                        ExportDetails = new List<ExportDetailModel>()
                    };

                    foreach (var item in ExportDetails)
                    {
                        // Kiểm tra tồn kho lần cuối trong DB (tránh trường hợp nhiều người cùng xuất)
                        var productInDb = await db.Products.FindAsync(item.ProductId);
                        if (productInDb == null) throw new Exception($"Sản phẩm {item.Product?.ProductCode} không tồn tại.");

                        if (productInDb.InitialQty < item.Quantity)
                        {
                            throw new Exception($"Sản phẩm '{productInDb.ProductName}' không đủ hàng để xuất. Tồn: {productInDb.InitialQty}, Cần: {item.Quantity}");
                        }

                        // TRỪ TỒN KHO
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

                    // Reset UI
                    ExportDetails.Clear();
                    TotalAmount = 0;
                    CustomerSearchText = string.Empty;
                    ProductSearchText = string.Empty;
                    SelectedCustomer = null;
                    SelectedProduct = null;
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}