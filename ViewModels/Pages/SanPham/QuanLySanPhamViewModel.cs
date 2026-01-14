using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // Cần thiết cho GetRequiredService
using Microsoft.Win32; // Cho OpenFileDialog
using System;
using System.Windows; // Cho MessageBox
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging; // Cho BitmapImage
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using UiDesktopApp1.Services;
using UiDesktopApp1.Views.UserControls.SanPham; // Import Dialog View
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using UiDesktopApp1.Views.Pages;
using MessageBoxButton = System.Windows.MessageBoxButton; // Import ContentDialog

namespace UiDesktopApp1.ViewModels.Pages.SanPham
{
    public partial class QuanLySanPhamViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        //private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ApiService _apiService;
        private readonly CurrentUserService _currentUserService;
        private readonly IContentDialogService _contentDialogService; // Thêm service dialog
        private readonly ICollectionView _productsView;
        private bool _isInitialized = false;

        // --- Danh sách ---
        public ObservableCollection<ProductModel> Products { get; } = new();
        public ICollectionView ProductsView => _productsView;

        [ObservableProperty] private ObservableCollection<CategoryModel> categories = new();
        [ObservableProperty] private CategoryModel? selectedCategory;
        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int selectedCount = 0;
        [ObservableProperty] private bool _isAllItemsSelected;
        private bool _isSyncingSelection = false;

        // --- Properties cho Dialog ---
        [ObservableProperty] private ProductModel _productForDialog = new();
        [ObservableProperty] private BitmapImage? _productImage;
        [ObservableProperty] private string _errorSummary = string.Empty;
        [ObservableProperty] private string _errorSummary1 = string.Empty;

        [ObservableProperty] private bool _isAddingCategory = false;
        [ObservableProperty] private string _newCategoryName = string.Empty;

        public bool IsUserNotEmployee => !_currentUserService.IsEmployee;

        public QuanLySanPhamViewModel(
            INavigationService navigationService,
            CurrentUserService currentUserService,
            IContentDialogService contentDialogService,
            ApiService apiService) 
        {
            _navigationService = navigationService;
            _currentUserService = currentUserService;
            _contentDialogService = contentDialogService;
            _apiService = apiService;

            _productsView = CollectionViewSource.GetDefaultView(Products);
            //_productsView.SortDescriptions.Add(new SortDescription(nameof(ProductModel.Id), ListSortDirection.Descending));
            _productsView.Filter = FilterProducts;

            // Chỉ cần lắng nghe tin nhắn nếu bạn thêm danh mục từ nơi khác
            //WeakReferenceMessenger.Default.Register<ProductCreatedMessage>(this);
            _apiService = apiService;
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
                //await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Load Categories
                Categories.Clear();
                // Thêm item mặc định cho bộ lọc
                Categories.Add(new CategoryModel { Id = 0, Name = "Tất cả danh mục" });

                //var cats = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

                var cats = await _apiService.GetAllAsync<CategoryModel>("Categories");

                //Có thể sử dụng OderBy ở đây nếu API chưa sắp xếp

                foreach (var c in cats) Categories.Add(c);
                SelectedCategory = Categories.FirstOrDefault();

                // Load Products
                foreach (var p in Products) p.PropertyChanged -= Product_PropertyChanged;
                Products.Clear();
                //var items = await db.Products.AsNoTracking().Include(p => p.Category).OrderByDescending(p => p.Id).ToListAsync();
                var items = await _apiService.GetAllAsync<ProductModel>("Products");

                foreach (var p in items)
                {
                    p.Image = ImageHelper.LoadBitmap(p.ImagePath);
                    p.PropertyChanged += Product_PropertyChanged;
                    Products.Add(p);
                }
                UpdateSelections();
                SearchText = string.Empty;
                _productsView.Refresh();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Lỗi: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void Search() { _productsView.Refresh(); UpdateSelections(); }

        partial void OnSearchTextChanged(string value) { if (string.IsNullOrWhiteSpace(value)) Search(); }
        partial void OnSelectedCategoryChanged(CategoryModel? value) { Search(); }

        private bool FilterProducts(object obj)
        {
            if (obj is not ProductModel p) return false;
            if (SelectedCategory != null && SelectedCategory.Id != 0 && p.CategoryId != SelectedCategory.Id) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) || (p.ProductCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
        }

        private void Product_PropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(ProductModel.IsSelected)) UpdateSelections(); }

        private void UpdateSelections()
        {
            var viewItems = _productsView.Cast<ProductModel>().ToList();
            SelectedCount = viewItems.Count(p => p.IsSelected);

            _isSyncingSelection = true;

            if (viewItems.Count > 0 && viewItems.All(p => p.IsSelected))
                IsAllItemsSelected = true;
            else
                IsAllItemsSelected = false;

            _isSyncingSelection = false;
        }

        [RelayCommand] private void SelectAll() { foreach (var p in Products) p.IsSelected = false; UpdateSelections(); }

        partial void OnIsAllItemsSelectedChanged(bool value)
        {
            if (_isSyncingSelection) return;

            var viewItems = _productsView.Cast<ProductModel>().ToList();
            foreach (var item in viewItems)
                item.IsSelected = value;

            // Cập nhật lại số lượng đã chọn
            SelectedCount = viewItems.Count(p => p.IsSelected);
        }

        // 1. Mở Dialog Thêm
        [RelayCommand]
        private async Task AddProductAsync()
        {
            await ShowProductDialogAsync(null);
        }

        // 2. Mở Dialog Sửa
        [RelayCommand]
        private async Task EditProductAsync(ProductModel? product) 
        {
            if (product == null) return;
            await ShowProductDialogAsync(product);
        }

        private async Task ShowProductDialogAsync(ProductModel? existingProduct)
        {
            ErrorSummary = string.Empty;

            // Chuẩn bị dữ liệu cho Dialog
            if (existingProduct == null)
            {
                // Chế độ THÊM
                ProductForDialog = new ProductModel { ProductCode = $"SP-{DateTime.Now:yyMMddHHmmss}" };
                ProductImage = ImageHelper.LoadBitmap(ProductForDialog.ImagePath);
            }
            else
            {
                // Chế độ SỬA: Clone dữ liệu để không ảnh hưởng list bên ngoài khi chưa lưu
                ProductForDialog = new ProductModel
                {
                    Id = existingProduct.Id,
                    ProductCode = existingProduct.ProductCode,
                    ProductName = existingProduct.ProductName,
                    InitialQty = existingProduct.InitialQty,
                    SalePrice = existingProduct.SalePrice,
                    CategoryId = existingProduct.CategoryId,
                    ExpiryDate = existingProduct.ExpiryDate,
                    Description = existingProduct.Description,
                    ImagePath = existingProduct.ImagePath
                };
                ProductImage = ImageHelper.LoadBitmap(ProductForDialog.ImagePath);
            }

            // Lấy View từ DI
            var dialogContent = App.Services.GetRequiredService<ThemSuaSanPhamDialog>();
            // Gán DataContext của Dialog chính là ViewModel này
            dialogContent.DataContext = this;

            var dialog = new ContentDialog
            {
                Title = existingProduct == null ? "Thêm sản phẩm mới" : "Sửa thông tin sản phẩm",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    // Xử lý Lưu
                    bool success = await HandleSaveProductAsync();
                    if (!success) e.Cancel = true; // Giữ dialog nếu lỗi
                }
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        private async Task<bool> HandleSaveProductAsync()
        {
            ProductForDialog.ValidateAll();
            if (ProductForDialog.HasErrors)
            {
                ErrorSummary = string.Join("\n", ProductForDialog.GetErrors().Select(e => e.ErrorMessage));
                return false;
            }

            IsBusy = true;
            try
            {
                //await using var db = await _dbContextFactory.CreateDbContextAsync();

                if (ProductForDialog.Id == 0 && ProductForDialog.ProductCode != null) // Thêm mới
                {
                    // Kiểm tra trùng mã
                    /*if (await db.Products.AnyAsync(p => p.ProductCode == ProductForDialog.ProductCode))
                    {
                        ErrorSummary = "Mã sản phẩm đã tồn tại.";
                        return false;
                    }*/
                    if(await _apiService.CheckExistsAsync("Products", "ProductCode", ProductForDialog.ProductCode))
                    {
                        ErrorSummary = "Mã sản phẩm đã tồn tại.";
                        return false;
                    }

                    /*db.Products.Add(ProductForDialog);
                    await db.SaveChangesAsync();*/
                    var addedProduct = await _apiService.AddAsync<ProductModel, ProductModel>("Products", ProductForDialog);
                    if (addedProduct == null)
                    {
                        ErrorSummary = "Lỗi khi thêm sản phẩm vào hệ thống, vui lòng thử lại sau!";
                        return false;
                    }

                    // Cập nhật UI
                    addedProduct.Image = ImageHelper.LoadBitmap(addedProduct.ImagePath);
                    // Map Category name để hiển thị
                    addedProduct.Category = Categories.FirstOrDefault(c => c.Id == addedProduct.CategoryId);
                    
                    addedProduct.PropertyChanged += Product_PropertyChanged;
                    Products.Insert(0, addedProduct); // Thêm lên đầu
                    
                    if(ProductsView is ICollectionView view) view.Refresh();
                }
                else // Cập nhật
                {
                    /*db.Products.Update(ProductForDialog);
                    await db.SaveChangesAsync();*/
                    var updatedProduct = await _apiService.UpdateAsync("Products", ProductForDialog.Id, ProductForDialog);
                    

                    // Cập nhật UI: Tìm item cũ và thay thế thông tin
                    var itemToUpdate = Products.FirstOrDefault(p => p.Id == ProductForDialog.Id);
                    if (itemToUpdate != null)
                    {
                        itemToUpdate.ProductCode = ProductForDialog.ProductCode;
                        itemToUpdate.ProductName = ProductForDialog.ProductName;
                        itemToUpdate.InitialQty = ProductForDialog.InitialQty;
                        itemToUpdate.SalePrice = ProductForDialog.SalePrice;
                        itemToUpdate.CategoryId = ProductForDialog.CategoryId;
                        itemToUpdate.ExpiryDate = ProductForDialog.ExpiryDate;
                        itemToUpdate.Description = ProductForDialog.Description;
                        itemToUpdate.ImagePath = ProductForDialog.ImagePath;
                        itemToUpdate.Image = ImageHelper.LoadBitmap(ProductForDialog.ImagePath);

                        // Update Category name hiển thị
                        itemToUpdate.Category = Categories.FirstOrDefault(c => c.Id == ProductForDialog.CategoryId);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorSummary = $"Lỗi hệ thống: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenPicture()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Chọn ảnh sản phẩm",
                Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
            };
            if (ofd.ShowDialog() == true)
            {
                ProductForDialog.ImagePath = ofd.FileName;
                ProductImage = ImageHelper.LoadBitmap(ofd.FileName);
            }
        }

        [RelayCommand]
        private void GenerateCode()
        {
            ProductForDialog.ProductCode = $"SP-{DateTime.Now:yyMMddHHmmss}";
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            var selectedItems = _productsView.Cast<ProductModel>().Where(p => p.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            var result = System.Windows.MessageBox.Show($"Bạn có chắc chắn muốn xóa {selectedItems.Count} sản phẩm?", "Xác nhận xóa", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                int deletedCount = 0;
                var errorMessages = new List<string>(); // Danh sách chứa lỗi của từng item

                foreach (var item in selectedItems)
                {
                    try
                    {
                        // Gọi hàm DeleteAsync. Nếu lỗi, nó sẽ nhảy xuống catch ngay lập tức
                        await _apiService.DeleteAsync("Products", item.Id);

                        // Nếu chạy đến đây nghĩa là thành công (không bị Exception)
                        item.PropertyChanged -= Product_PropertyChanged;
                        Products.Remove(item);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Bắt lỗi chi tiết từ ApiService (VD: "Sản phẩm đã tồn tại trong lịch sử nhập")
                        errorMessages.Add($"- {item.ProductName}: {ex.Message}");
                    }
                }

                UpdateSelections();

                // Hiển thị kết quả sau khi chạy xong hết
                if (deletedCount < selectedItems.Count)
                {
                    string msg = $"Đã xóa {deletedCount}/{selectedItems.Count} sản phẩm.\n\nCác sản phẩm không thể xóa do ràng buộc:";

                    if (errorMessages.Any())
                    {
                        // Chỉ hiện tối đa 5 lỗi để tránh bảng thông báo quá dài
                        msg += "\n" + string.Join("\n", errorMessages.Take(5));
                        if (errorMessages.Count > 5) msg += "\n...";
                    }

                    System.Windows.MessageBox.Show(msg, "Kết quả xóa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) // Catch lỗi hệ thống lớn (mất mạng, crash app...)
            {
                System.Windows.MessageBox.Show($"Lỗi hệ thống: {ex.Message}");
                await LoadDataAsync();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.Navigate(typeof(SanPhamPage));
        }

        [RelayCommand]
        private void OpenAddCategory()
        {
            NewCategoryName = string.Empty;
            IsAddingCategory = true; // Hiển thị overlay
        }

        [RelayCommand]
        private void CloseAddCategory()
        {
            IsAddingCategory = false; // Ẩn overlay
        }

        [RelayCommand]
        private async Task AddCategoryAsync()
        {
            var success = await HandleSaveCategoryAsync();
            if (success)
            {
                ErrorSummary1 = string.Empty;
                IsAddingCategory = false; // Ẩn overlay nếu thành công
            }
                
        }

        private async Task<bool> HandleSaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                ErrorSummary1 = "Danh mục không được để trống";
                return false;
            }
            IsBusy = true;

            try
            {
                /*await using var db = await _dbContextFactory.CreateDbContextAsync();
                if (await db.Categories.AnyAsync(c => c.Name == NewCategoryName))
                {
                    ErrorSummary1 = $"Danh mục '{NewCategoryName}' đã tồn tại";
                    return false;
                }*/
                
                if (await _apiService.CheckExistsAsync("Categories", "Name", NewCategoryName))
                {
                    ErrorSummary1 = $"Danh mục '{NewCategoryName}' đã tồn tại";
                    return false;
                }

                var newCategory = new CategoryModel { Name = NewCategoryName.Trim() };

                var result = await _apiService.AddAsync<CategoryModel, CategoryModel>("Categories", newCategory);
                if(result == null)
                {
                    ErrorSummary1 = "Thêm thất bại! Vui lòng kiểm tra kết nối hoặc thử lại sau.";
                    return false;
                }

                // Cập nhật UI
                Categories.Add(newCategory);
                ProductForDialog.CategoryId = newCategory.Id;
                return true;
            }
            catch (Exception ex)
            {
                ErrorSummary = "Lỗi hệ thống: " + ex.Message;
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}