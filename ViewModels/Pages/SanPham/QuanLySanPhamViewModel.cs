using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore; // <-- CẦN THÊM USING NÀY
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using UiDesktopApp1.Views.Pages.SanPham;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using UiDesktopApp1;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using UiDesktopApp1.Contracts;

namespace UiDesktopApp1.ViewModels.Pages.SanPham
{
    public partial class QuanLySanPhamViewModel : ObservableObject, INavigationAware, IRecipient<ProductsNeedRefreshMessage>
    {
        private readonly INavigationService _navigationService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ICollectionView _productsView;
        private bool _isInitialized = false;

        // Danh sách nguồn
        public ObservableCollection<ProductModel> Products { get; } = new();

        public ICollectionView ProductsView => _productsView;

        [ObservableProperty]
        private ObservableCollection<CategoryModel> categories = new();

        [ObservableProperty]
        private CategoryModel? selectedCategory;

        [ObservableProperty] private string searchText = string.Empty;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int selectedCount = 0;
        [ObservableProperty] private bool _isAllItemsSelected;

        public QuanLySanPhamViewModel(INavigationService navigationService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));

            _productsView = CollectionViewSource.GetDefaultView(Products);
            _productsView.SortDescriptions.Add(new SortDescription(nameof(ProductModel.ProductName), ListSortDirection.Ascending));
            _productsView.Filter = FilterProducts;

            //Đăng ký nhận tin nhắn "cần làm mới"
            WeakReferenceMessenger.Default.Register<ProductsNeedRefreshMessage>(this);

            Products.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (ProductModel item in e.NewItems)
                        item.PropertyChanged += Product_PropertyChanged;

                if (e.OldItems != null)
                    foreach (ProductModel item in e.OldItems)
                        item.PropertyChanged -= Product_PropertyChanged;

                UpdateSelections();
            };
        }

        // Hàm nhận tin nhắn làm mới (từ trang Sửa hoặc Thêm)
        public void Receive(ProductsNeedRefreshMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadDataAsync();
            });
        }

        private void Product_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductModel.IsSelected))
            {
                UpdateSelections();
            }
        }

        private void UpdateSelections()
        {
            UpdateSelectedCount();
            UpdateIsAllItemsSelected();
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = _productsView.Cast<ProductModel>().Count(p => p.IsSelected);
        }

        private void UpdateIsAllItemsSelected()
        {
            var viewItems = _productsView.Cast<ProductModel>().ToList();
            bool allSelected = viewItems.Count > 0 && viewItems.All(p => p.IsSelected);
            #pragma warning disable
            SetProperty(ref _isAllItemsSelected, allSelected, nameof(IsAllItemsSelected));
        }

        partial void OnIsAllItemsSelectedChanged(bool value)
        {
            var viewItems = _productsView.Cast<ProductModel>().ToList();
            foreach (var item in viewItems)
            {
                item.IsSelected = value;
            }
        }

        #region Navigation
        public async Task OnNavigatedToAsync()
        {
            // Logic này giữ nguyên: chỉ tải lần đầu, các lần sau sẽ được làm mới
            // thông qua tin nhắn "ProductsNeedRefreshMessage"
            if (!_isInitialized)
            {
                await LoadDataAsync();
                _isInitialized = true;
            }
        }
        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
        #endregion

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                // Tạo một AppDbContext mới chỉ dùng cho hàm này
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                foreach (var p in Products)
                    p.PropertyChanged -= Product_PropertyChanged;

                Products.Clear();
                SearchText = string.Empty.Trim();

                // Thêm .Include(p => p.Category) để hiển thị Tên Danh mục
                var items = await db.Products
                    .Include(p => p.Category)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                foreach (var p in items)
                {
                    p.Image = ImageHelper.LoadBitmap(p.ImagePath);
                    p.PropertyChanged += Product_PropertyChanged;
                    Products.Add(p);
                }

                // Tải danh mục (dùng 'db' mới)
                if (Categories.Count == 0)
                {
                    Categories.Add(new CategoryModel { Id = 0, Name = "Danh mục" });
                    var list = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
                    foreach (var cat in list)
                        Categories.Add(cat);
                }
                SelectedCategory = Categories.FirstOrDefault();
            }
            finally
            {
                IsBusy = false;
                UpdateSelections();
            }
        }


        partial void OnSearchTextChanged(string value)
        {
            _productsView?.Refresh();
            DeselectHiddenItems();
            UpdateIsAllItemsSelected();
            UpdateSelectedCount();
        }

        private bool FilterProducts(object obj)
        {
            if (obj is not ProductModel p) return false;

            if (SelectedCategory != null && SelectedCategory.Id != 0)
                if (p.CategoryId != SelectedCategory.Id) return false;


            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                || (p.ProductCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
        }

        partial void OnSelectedCategoryChanged(CategoryModel? value)
        {
            _productsView?.Refresh();
            DeselectHiddenItems();
            UpdateIsAllItemsSelected();
            UpdateSelectedCount();
        }

        // Đổi tên từ Exit -> GoBack (Rõ nghĩa hơn)
        [RelayCommand]
        private void GoBack()
        {
            _navigationService.Navigate(typeof(UiDesktopApp1.Views.Pages.SanPhamPage));
        }

        [RelayCommand]
        private void SelectAll()
        {
            var itemsToDeselect = _productsView.Cast<ProductModel>().Where(p => p.IsSelected).ToList();
            foreach (var product in itemsToDeselect)
                product.IsSelected = false;
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            // 1. Lấy danh sách sản phẩm (từ UI) để biết ID
            var selectedProductsFromVM = _productsView.Cast<ProductModel>().Where(p => p.IsSelected).ToList();
            if (selectedProductsFromVM.Count == 0) return;

            var result = System.Windows.MessageBox.Show($"Bạn có chắc chắn muốn xóa {selectedProductsFromVM.Count} sản phẩm đã chọn không?",
                                                       "Xác nhận xóa",
                                                       System.Windows.MessageBoxButton.YesNo,
                                                       System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                var productIdsToDelete = selectedProductsFromVM.Select(p => p.Id).ToList();
                var productsToDeleteFromDb = await db.Products
                    .Where(p => productIdsToDelete.Contains(p.Id))
                    .ToListAsync();

                db.Products.RemoveRange(productsToDeleteFromDb);
                await db.SaveChangesAsync();

                foreach (var product in selectedProductsFromVM)
                {
                    product.PropertyChanged -= Product_PropertyChanged;
                    Products.Remove(product);
                }

                WeakReferenceMessenger.Default.Send(new ProductsNeedRefreshMessage());
                UpdateSelections();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Đã xảy ra lỗi khi xóa sản phẩm:\n{ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                await LoadDataAsync(); 
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DeselectHiddenItems()
        {
            var currentlySelected = Products.Where(p => p.IsSelected).ToList();
            if (currentlySelected.Count == 0) return;

            var visibleItems = _productsView.Cast<ProductModel>().ToHashSet();

            foreach (var item in currentlySelected)
            {
                if (!visibleItems.Contains(item))
                {
                    item.IsSelected = false;
                }
            }
        }

        [RelayCommand]
        private void EditProduct(ProductModel? product)
        {
            if (product == null) return;

            _navigationService.Navigate(typeof(SuaSanPhamPage));
            WeakReferenceMessenger.Default.Send(new EditProductMessage(product.Id));
        }
    }
}