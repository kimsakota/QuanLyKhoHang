using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using UiDesktopApp1.ViewModels.Pages.SanPham;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class SanPhamViewModel : ObservableObject, INavigationAware, IRecipient<ProductCreatedMessage>, IRecipient<ProductsNeedRefreshMessage>
    {
        private readonly INavigationService _navigationService;
        private readonly AppDbContext _db;
        private readonly ICollectionView _productsView;
        private bool _isInitialized = false;

        // Danh sách nguồn
        public ObservableCollection<ProductModel> Products { get; } = new();

        public ICollectionView ProductsView => _productsView;

        [ObservableProperty]
        private ObservableCollection<CategoryModel> categories = new();

        [ObservableProperty]
        private CategoryModel? selectedCategory;

        // Ô tìm kiếm
        [ObservableProperty] private string searchText = string.Empty;

        [ObservableProperty] private bool isBusy;

        public SanPhamViewModel(INavigationService navigationService, AppDbContext db)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            // Sau khi có dữ liệu, tạo view & filter
            _productsView = CollectionViewSource.GetDefaultView(Products);
            _productsView.Filter = FilterProducts;

            //Đăng ký nhận tin nhắn
            WeakReferenceMessenger.Default.Register<ProductCreatedMessage>(this);
            WeakReferenceMessenger.Default.Register<ProductsNeedRefreshMessage>(this);
        }

        #region Navigation
        public async Task OnNavigatedToAsync()
        {
            if(!_isInitialized)
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

        public void Receive(ProductCreatedMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                var newProduct = message.Value;
                newProduct.Image = ImageHelper.LoadBitmap(newProduct.ImagePath);
                Products.Add(newProduct);
                await LoadDataAsync();
            });
        }
        public void Receive(ProductsNeedRefreshMessage message)
        {
            // Khi nhận được tin nhắn này, chỉ cần chạy lại lệnh LoadDataAsync
            // Đảm bảo chạy trên UI thread
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadDataAsync();
            });
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Products.Clear();
                SearchText = string.Empty.Trim();
                var items = await _db.Products
                    .AsNoTracking()
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                foreach(var p in items)
                {
                    p.Image = ImageHelper.LoadBitmap(p.ImagePath);
                    Products.Add(p);
                }

                //_productsView.Refresh();
                SelectedCategory = Categories.FirstOrDefault();

                if (Categories.Count == 0)
                {
                    Categories.Add(new CategoryModel { Id = 0, Name = "Danh mục" });
                    var list = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
                    foreach (var cat in list)
                        Categories.Add(cat);
                    SelectedCategory = Categories.FirstOrDefault();
                }
            }
            finally { IsBusy = false; }
        }

        partial void OnSearchTextChanged(string value) => _productsView?.Refresh();

        private bool FilterProducts(object obj)
        {
            if (obj is not ProductModel p) return false;

            if(SelectedCategory != null && SelectedCategory.Id != 0)
                if(p.CategoryId != SelectedCategory.Id) return false;


            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                || (p.ProductCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
        }

        partial void OnSelectedCategoryChanged(CategoryModel? value)
        {
            _productsView?.Refresh();
        }
    }
}