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

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class SanPhamViewModel : ObservableObject, INavigationAware, IRecipient<ProductCreatedMessage>, IRecipient<ProductsNeedRefreshMessage>
    {
        private readonly INavigationService _navigationService;
        //private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CurrentUserService _currentUserService;
        private readonly ApiService _apiService;
        private readonly ICollectionView _productsView;
        private bool _isInitialized = false;

        public ObservableCollection<ProductModel> Products { get; } = new();
        public ICollectionView ProductsView => _productsView;

        [ObservableProperty]
        private ObservableCollection<CategoryModel> categories = new();

        [ObservableProperty]
        private CategoryModel? selectedCategory;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public bool IsUserNotEmployee => !_currentUserService.IsEmployee;

        public SanPhamViewModel(
            INavigationService navigationService,
            CurrentUserService currentUserService,
            ApiService apiService)
        {
            _navigationService = navigationService;
            _currentUserService = currentUserService;
            _apiService = apiService;

            _productsView = CollectionViewSource.GetDefaultView(Products);
            _productsView.Filter = FilterProducts;

            WeakReferenceMessenger.Default.Register<ProductCreatedMessage>(this);
            WeakReferenceMessenger.Default.Register<ProductsNeedRefreshMessage>(this);
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

        public void Receive(ProductCreatedMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () => await LoadDataAsync());
        }

        public void Receive(ProductsNeedRefreshMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () => await LoadDataAsync());
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                //await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                
                Products.Clear();

                // Load Categories
                if (Categories.Count == 0)
                {
                    Categories.Add(new CategoryModel { Id = 0, Name = "Tất cả danh mục" });
                    //var list = await dbContext.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

                    var cats = await _apiService.GetAllAsync<CategoryModel>("Categories");

                    if (cats == null) return;
                    foreach (var cat in cats) Categories.Add(cat);
                    SelectedCategory = Categories.First();
                }

                // Load Products
                /*var items = await dbContext.Products
                    .AsNoTracking()
                    .Include(p => p.Category) // Include để hiển thị tên danh mục nếu cần
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();*/
                var items = await _apiService.GetAllAsync<ProductModel>("Products");

                if (items == null) return;
                foreach (var p in items)
                {
                    p.Image = ImageHelper.LoadBitmap(p.ImagePath);
                    Products.Add(p);
                }

                SearchText = string.Empty;
                _productsView.Refresh();
            }
            finally { IsBusy = false; }
        }

        // --- LOGIC TÌM KIẾM MỚI ---

        [RelayCommand]
        private void Search()
        {
            _productsView.Refresh();
        }

        // Bỏ logic auto search khi gõ text
        partial void OnSearchTextChanged(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                Search();
        }

        // Vẫn giữ auto search khi chọn danh mục
        partial void OnSelectedCategoryChanged(CategoryModel? value)
        {
            _productsView.Refresh();
        }

        private bool FilterProducts(object obj)
        {
            if (obj is not ProductModel p) return false;

            // 1. Lọc theo Danh mục
            if (SelectedCategory != null && SelectedCategory.Id != 0)
                if (p.CategoryId != SelectedCategory.Id) return false;

            // 2. Lọc theo Text (chỉ chạy khi nhấn tìm kiếm)
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                || (p.ProductCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
        }

        // --- NAVIGATION COMMANDS ---

        [RelayCommand]
        private void Manage() => _navigationService.Navigate(typeof(QuanLySanPhamPage));
    }
}