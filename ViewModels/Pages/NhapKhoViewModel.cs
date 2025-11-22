using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;
using UiDesktopApp1.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class NhapKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        [ObservableProperty]
        private ObservableCollection<SupplierModel> _suppliers = new();
        [ObservableProperty]
        private ObservableCollection<ProductModel> _products = new();

        [ObservableProperty] 
        private string _supplierSearchText = string.Empty;
        [ObservableProperty] 
        private string _productSearchText = string.Empty;

        [ObservableProperty] 
        private SupplierModel? _selectedSupplier;
        [ObservableProperty] 
        private ProductModel? _selectedProduct;
        [ObservableProperty]
        private DateTime _importDate = DateTime.Now;
        [ObservableProperty] 
        private int _inputQuantity = 1;
        [ObservableProperty] 
        private decimal _inputPrice = 0;
        [ObservableProperty] 
        private string _errorMessage = string.Empty;

        [ObservableProperty] 
        private ObservableCollection<ImportDetailModel> _importDetails = new();
        [ObservableProperty] 
        private decimal _totalAmount;

        public NhapKhoViewModel (INavigationService navigationService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _navigationService = navigationService;
            _dbContextFactory = dbContextFactory;
        }
        public async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        // 1. Tải dữ liệu ban đầu
        private async Task LoadDataAsync()
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                Suppliers.Clear();
                var suppliers = await db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
                foreach (var s in suppliers) Suppliers.Add(s);

                Products.Clear();
                var products = await db.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync();
                foreach (var p in products) Products.Add(p);

                ProductSearchText = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
        }

        partial void OnSupplierSearchTextChanged(string value)
        {
            // Thử tìm trong danh sách xem có khớp tên không
            var match = Suppliers.FirstOrDefault(s => s.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                SelectedSupplier = match;
            }
            else
            {
                // Nếu không khớp -> Có thể là nhập mới (Xử lý sau ở nút Lưu)
                // Hoặc gán tạm null để biết chưa chọn đúng danh mục cũ
                SelectedSupplier = null;
            }
        }

        partial void OnProductSearchTextChanged(string value)
        {
            
            // Tìm theo Tên hoặc Mã
            var match = Products.FirstOrDefault(p =>
                p.ProductName.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCode.Equals(value, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                SelectedProduct = match;
            }
            else
            {
                SelectedProduct = null;
                // Reset giá nhập nếu không tìm thấy SP
                InputPrice = 0;
            }
        }

        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null) return;

            // Đồng bộ lại text hiển thị nếu chọn từ code-behind (optional)
            if (ProductSearchText != value.ProductName)
                ProductSearchText = value.ProductName;

            // Chạy ngầm lấy giá cũ
            Task.Run(async () =>
            {
                try
                {
                    await using var db = await _dbContextFactory.CreateDbContextAsync();
                    var lastImport = await db.ImportDetails
                        .Include(d => d.Import)
                        .Where(d => d.ProductId == value.Id)
                        .OrderByDescending(d => d.Import.ImportDate)
                        .FirstOrDefaultAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        InputPrice = lastImport != null ? lastImport.UnitPrice : (value.SalePrice * 0.7m);
                    });
                }
                catch { }
            });
        }

        // 3. Thêm sản phẩm vào danh sách tạm
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

            // Kiểm tra sản phẩm đã có trong lưới chưa
            var existingItem = ImportDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);

            if (existingItem != null)
            {
                // Logic cộng dồn: Tăng số lượng, cập nhật giá mới nhất
                existingItem.Quantity += InputQuantity;
                existingItem.UnitPrice = InputPrice;

                // Hack nhỏ để UI cập nhật lại dòng đó (Refresh)
                int index = ImportDetails.IndexOf(existingItem);
                ImportDetails.RemoveAt(index);
                ImportDetails.Insert(index, existingItem);
            }
            else
            {
                // Tạo dòng mới
                var newItem = new ImportDetailModel
                {
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct, // Gán object để hiển thị Tên, Mã trên Grid
                    Quantity = InputQuantity,
                    UnitPrice = InputPrice
                };
                ImportDetails.Add(newItem);
            }

            // Reset form nhập liệu để nhập tiếp cho nhanh
            InputQuantity = 1;
            // Giữ nguyên InputPrice hoặc reset về 0 tùy trải nghiệm, ở đây giữ nguyên tiện nhập nhiều dòng

            CalculateTotal();
        }

        // 4. Xóa dòng
        [RelayCommand]
        private void RemoveItem(ImportDetailModel item)
        {
            if (ImportDetails.Contains(item))
            {
                ImportDetails.Remove(item);
                CalculateTotal();
            }
        }

        // 5. Tính tổng tiền
        private void CalculateTotal()
        {
            TotalAmount = ImportDetails.Sum(x => x.Quantity * x.UnitPrice);
        }

        // 6. LƯU PHIẾU NHẬP (Quan trọng nhất)
        [RelayCommand]
        private async Task SaveImportAsync()
        {
            // Validation đầu vào
            if (SelectedSupplier == null)
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (ImportDetails.Count == 0)
            {
                MessageBox.Show("Danh sách hàng hóa đang trống!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Xác nhận nhập kho {ImportDetails.Count} mặt hàng?\nTổng tiền: {TotalAmount:N0} đ",
                                          "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Sử dụng Transaction để đảm bảo tính toàn vẹn dữ liệu (hoặc lưu tất cả hoặc không lưu gì cả)
                using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    // A. Tạo phiếu nhập (Header)
                    var newImport = new ImportModel
                    {
                        SupplierId = SelectedSupplier.Id,
                        ImportDate = ImportDate,
                        ImportedBy = "Admin", // Sau này thay bằng CurrentUser.Username
                        ImportDetails = new List<ImportDetailModel>() // Khởi tạo list rỗng
                    };

                    // B. Xử lý chi tiết & Cập nhật tồn kho
                    foreach (var item in ImportDetails)
                    {
                        // 1. Thêm chi tiết
                        newImport.ImportDetails.Add(new ImportDetailModel
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });

                        // 2. Tìm và cập nhật tồn kho sản phẩm
                        var productInDb = await db.Products.FindAsync(item.ProductId);
                        if (productInDb != null)
                        {
                            productInDb.InitialQty += item.Quantity;
                            // Có thể cập nhật giá vốn bình quân tại đây nếu muốn logic phức tạp hơn
                        }
                    }

                    db.Imports.Add(newImport);
                    await db.SaveChangesAsync(); // Lưu tất cả thay đổi vào DB
                    await transaction.CommitAsync(); // Xác nhận transaction

                    MessageBox.Show("Lưu phiếu nhập thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // C. Reset giao diện về trạng thái ban đầu
                    RefreshPage();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // Hoàn tác nếu lỗi
                    throw ex; // Ném lỗi ra ngoài để catch bên dưới hiển thị
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu phiếu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RefreshPage()
        {
            ImportDetails.Clear();
            SelectedSupplier = null;
            SelectedProduct = null;
            TotalAmount = 0;
            InputQuantity = 1;
            InputPrice = 0;
            ErrorMessage = string.Empty;
        }


    }
}