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
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class XuatKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CurrentUserService _currentUserService;
        private readonly IContentDialogService _contentDialogService;

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

        public XuatKhoViewModel(IDbContextFactory<AppDbContext> dbContextFactory,
            CurrentUserService currentUserService,
            IContentDialogService contentDialogService)
        {
            _dbContextFactory = dbContextFactory;
            _currentUserService = currentUserService;
            _contentDialogService = contentDialogService;
        }

        public async Task OnNavigatedToAsync()
        {
            ExportDate = DateTime.Now; // Cập nhật giờ hiện tại
            RefreshForm(); // Reset form sạch sẽ
            await LoadDataAsync();
        }

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

                // Không xóa SearchText ở đây để tránh mất dữ liệu nếu người dùng đang nhập mà trang reload
            }
            catch (Exception ex) { ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}"; }
        }

        // Logic tìm kiếm Khách hàng (An toàn Null & IgnoreCase)
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

        // Khi chọn sản phẩm -> Lấy giá bán & Tồn kho thực tế từ DB
        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null) return;

            // Đồng bộ text hiển thị
            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
                ProductSearchText = value.ProductName ?? string.Empty;

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
                            InputPrice = realTimeProduct.SalePrice; // Giá bán mặc định
                            CurrentStock = realTimeProduct.InitialQty; // Tồn kho thực tế
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

            // --- KIỂM TRA TỒN KHO ---
            // Tính tổng số lượng đang chờ xuất trong lưới của sản phẩm này
            var pendingQty = ExportDetails.Where(x => x.ProductId == SelectedProduct.Id).Sum(x => x.Quantity);

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

                // Refresh Grid
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

            // Reset vùng nhập liệu sản phẩm
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
                if (!string.IsNullOrWhiteSpace(CustomerSearchText))
                {
                    var ask = MessageBox.Show($"Khách hàng \"{CustomerSearchText}\" chưa có trong hệ thống. Bạn có muốn tạo mới?",
                                              "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (ask == MessageBoxResult.Yes)
                    {
                        SelectedCustomer = new CustomerModel { Name = CustomerSearchText, PhoneNumber = "", Address = "" };
                    }
                    else return;
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn Khách hàng nhận hàng.",
                                    "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (ExportDetails.Count == 0)
            {
                MessageBox.Show("Danh sách xuất kho đang trống. Vui lòng thêm ít nhất một sản phẩm.",
                                "Chưa có hàng hóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = $"Bạn đang chuẩn bị xuất kho {ExportDetails.Count} mặt hàng.\n" +
                  $"Tổng tiền: {TotalAmount:N0} VNĐ.\n\n" +
                  "Bạn có chắc chắn muốn thực hiện không?";

            var confirm = MessageBox.Show($"Xác nhận xuất kho {ExportDetails.Count} mặt hàng?\nTổng tiền: {TotalAmount:N0} đ",
                                          "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                using var trans = await db.Database.BeginTransactionAsync();

                try
                {
                    // 1. Lưu khách hàng mới nếu cần (ID == 0)
                    if (SelectedCustomer.Id == 0)
                    {
                        db.Customers.Add(SelectedCustomer);
                        await db.SaveChangesAsync();
                    }

                    // 2. Tạo phiếu xuất
                    var newExport = new ExportModel
                    {
                        CustomerId = SelectedCustomer.Id,
                        ExportDate = ExportDate,
                        ExportedBy = _currentUserService.CurrentUser?.Username ?? "Unknown", 
                        ExportDetails = new List<ExportDetailModel>()
                    };

                    // 3. Xử lý chi tiết & Trừ kho
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

                    // 4. Reset toàn bộ form
                    RefreshForm();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hàm reset form sạch sẽ
        private void RefreshForm()
        {
            ExportDetails.Clear();
            TotalAmount = 0;
            CustomerSearchText = string.Empty;
            SelectedCustomer = null;

            // Input fields
            ProductSearchText = string.Empty;
            SelectedProduct = null;
            InputQuantity = 1;
            InputPrice = 0;
            CurrentStock = 0;
            ErrorMessage = string.Empty;
        }
    }
}