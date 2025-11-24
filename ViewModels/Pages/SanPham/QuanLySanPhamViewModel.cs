using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using UiDesktopApp1.Services;
using UiDesktopApp1.Views.Pages.SanPham;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.SanPham
{
    public partial class QuanLySanPhamViewModel : ObservableObject, INavigationAware, IRecipient<ProductCreatedMessage>, IRecipient<ProductsNeedRefreshMessage>
    {
        private readonly INavigationService _navigationService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CurrentUserService _currentUserService;
        private readonly ICollectionView _productsView;
        private bool _isInitialized = false;

        // Danh sách sản phẩm gốc
        public ObservableCollection<ProductModel> Products { get; } = new();

        // View để hiển thị và lọc (bind vào DataGrid)
        public ICollectionView ProductsView => _productsView;

        [ObservableProperty]
        private ObservableCollection<CategoryModel> categories = new();

        [ObservableProperty]
        private CategoryModel? selectedCategory;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        // Các thuộc tính hỗ trợ Selection (Chọn nhiều dòng)
        [ObservableProperty]
        private int selectedCount = 0;

        [ObservableProperty]
        private bool _isAllItemsSelected;

        // Thuộc tính phân quyền: True nếu KHÔNG phải là Nhân viên (dùng để ẩn/hiện nút)
        public bool IsUserNotEmployee => !_currentUserService.IsEmployee;

        public QuanLySanPhamViewModel(
            INavigationService navigationService,
            IDbContextFactory<AppDbContext> dbContextFactory,
            CurrentUserService currentUserService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

            // Cấu hình ICollectionView
            _productsView = CollectionViewSource.GetDefaultView(Products);
            _productsView.SortDescriptions.Add(new SortDescription(nameof(ProductModel.ProductName), ListSortDirection.Ascending));
            _productsView.Filter = FilterProducts;

            // Đăng ký nhận tin nhắn từ các trang khác
            WeakReferenceMessenger.Default.Register<ProductCreatedMessage>(this);
            WeakReferenceMessenger.Default.Register<ProductsNeedRefreshMessage>(this);
        }

        #region Navigation & Messaging

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                await LoadDataAsync();
                _isInitialized = true;
            }
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        public void Receive(ProductCreatedMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () => await LoadDataAsync());
        }

        public void Receive(ProductsNeedRefreshMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () => await LoadDataAsync());
        }

        #endregion

        #region Data Loading

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Hủy đăng ký sự kiện cũ để tránh memory leak
                foreach (var p in Products) p.PropertyChanged -= Product_PropertyChanged;
                Products.Clear();

                // Load Categories nếu chưa có
                if (Categories.Count == 0)
                {
                    Categories.Add(new CategoryModel { Id = 0, Name = "Tất cả danh mục" });
                    var cats = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
                    foreach (var c in cats) Categories.Add(c);
                    SelectedCategory = Categories.FirstOrDefault();
                }

                // Load Products (Kèm Category để hiển thị tên)
                var items = await db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.Id) // Sản phẩm mới nhất lên đầu
                    .ToListAsync();

                foreach (var p in items)
                {
                    p.Image = ImageHelper.LoadBitmap(p.ImagePath);
                    p.PropertyChanged += Product_PropertyChanged; // Đăng ký sự kiện chọn dòng
                    Products.Add(p);
                }

                // Reset trạng thái
                UpdateSelections();
                _productsView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Search Logic

        // Command được gọi khi nhấn nút Tìm hoặc nhấn Enter
        [RelayCommand]
        private void Search()
        {
            _productsView.Refresh();
            DeselectHiddenItems(); // Bỏ chọn các item bị ẩn sau khi lọc
            UpdateSelections();
        }

        // Xử lý khi text thay đổi
        partial void OnSearchTextChanged(string value)
        {
            // Nếu người dùng xóa hết chữ (ô trống), tự động kích hoạt tìm kiếm để Reset danh sách
            if (string.IsNullOrWhiteSpace(value))
            {
                Search();
            }
            // Ngược lại (khi đang gõ), KHÔNG làm gì cả => Đợi nhấn Enter hoặc nút Tìm
        }

        // Tự động lọc khi chọn danh mục
        partial void OnSelectedCategoryChanged(CategoryModel? value)
        {
            Search();
        }

        private bool FilterProducts(object obj)
        {
            if (obj is not ProductModel p) return false;

            // 1. Lọc theo Danh mục
            if (SelectedCategory != null && SelectedCategory.Id != 0)
            {
                if (p.CategoryId != SelectedCategory.Id) return false;
            }

            // 2. Lọc theo Text (Chỉ chạy khi Search() được gọi)
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                || (p.ProductCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
        }

        #endregion

        #region Selection Logic

        private void Product_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductModel.IsSelected))
            {
                UpdateSelections();
            }
        }

        private void UpdateSelections()
        {
            // Chỉ tính toán trên các item đang hiển thị (đã lọc)
            var viewItems = _productsView.Cast<ProductModel>().ToList();
            SelectedCount = viewItems.Count(p => p.IsSelected);

            // Cập nhật checkbox "Chọn tất cả" ở header
            // Dùng SetProperty trực tiếp field để tránh trigger OnIsAllItemsSelectedChanged vô tận
            if (viewItems.Count > 0 && viewItems.All(p => p.IsSelected))
            {
                SetProperty(ref _isAllItemsSelected, true, nameof(IsAllItemsSelected));
            }
            else
            {
                SetProperty(ref _isAllItemsSelected, false, nameof(IsAllItemsSelected));
            }
        }

        // Khi người dùng tích vào checkbox "Chọn tất cả" ở Header
        partial void OnIsAllItemsSelectedChanged(bool value)
        {
            var viewItems = _productsView.Cast<ProductModel>().ToList();
            foreach (var item in viewItems)
            {
                item.IsSelected = value;
            }
            UpdateSelections();
        }

        private void DeselectHiddenItems()
        {
            // Bỏ chọn những item không còn nằm trong View (bị lọc đi)
            var visibleItems = _productsView.Cast<ProductModel>().ToHashSet();
            var selectedHiddenItems = Products.Where(p => p.IsSelected && !visibleItems.Contains(p)).ToList();

            foreach (var item in selectedHiddenItems)
            {
                item.IsSelected = false;
            }
        }

        [RelayCommand]
        private void SelectAll()
        {
            // Hủy chọn tất cả
            foreach (var product in Products)
            {
                product.IsSelected = false;
            }
            UpdateSelections();
        }

        #endregion

        #region Action Commands

        [RelayCommand]
        private void AddProduct()
        {
            _navigationService.Navigate(typeof(ThemSanPhamPage));
        }

        [RelayCommand]
        private void Manage()
        {
            _navigationService.Navigate(typeof(QuanLySanPhamPage));
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.Navigate(typeof(UiDesktopApp1.Views.Pages.SanPhamPage));
        }

        [RelayCommand]
        private void EditProduct(ProductModel? product)
        {
            if (product == null) return;
            _navigationService.Navigate(typeof(SuaSanPhamPage));
            WeakReferenceMessenger.Default.Send(new EditProductMessage(product.Id));
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            // Lấy danh sách các item ĐANG HIỂN THỊ và ĐƯỢC CHỌN
            var selectedItems = _productsView.Cast<ProductModel>().Where(p => p.IsSelected).ToList();

            if (selectedItems.Count == 0) return;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa {selectedItems.Count} sản phẩm đã chọn không?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Lấy ID để xóa trong DB
                var idsToDelete = selectedItems.Select(p => p.Id).ToList();

                // Thực hiện xóa trong DB
                await db.Products
                        .Where(p => idsToDelete.Contains(p.Id))
                        .ExecuteDeleteAsync();

                // Xóa khỏi danh sách hiển thị (ObservableCollection)
                foreach (var item in selectedItems)
                {
                    // Hủy đăng ký sự kiện trước khi xóa
                    item.PropertyChanged -= Product_PropertyChanged;
                    Products.Remove(item);
                }

                UpdateSelections();

                // Thông báo cho các View khác nếu cần
                WeakReferenceMessenger.Default.Send(new ProductsNeedRefreshMessage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi khi xóa:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                await LoadDataAsync(); // Tải lại dữ liệu nếu có lỗi để đảm bảo đồng bộ
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion
    }
}