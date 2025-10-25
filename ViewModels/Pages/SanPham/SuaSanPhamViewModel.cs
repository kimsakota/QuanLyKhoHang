using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using UiDesktopApp1.Views.Pages.SanPham;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.SanPham
{
    public partial class SuaSanPhamViewModel : ObservableObject, IRecipient<EditProductMessage>, IRecipient<CategoryCreatedMessage>
    {
        private readonly INavigationService _nav;
        //private readonly AppDbContext _db;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        [ObservableProperty]
        private ProductModel product = new(); // Sản phẩm đang được chỉnh sửa

        [ObservableProperty]
        private ObservableCollection<CategoryModel> categories = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private BitmapImage? image;

        public SuaSanPhamViewModel(INavigationService nav, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _nav = nav;
            _dbContextFactory = dbContextFactory;

            WeakReferenceMessenger.Default.Register<EditProductMessage>(this);
            //WeakReferenceMessenger.Default.Register<CategoryCreatedMessage>(this);

            WeakReferenceMessenger.Default.Register<CategoryCreatedMessage>(this, (r, m) =>
            {
                // Cập nhật danh sách + chọn ngay danh mục mới
                Categories.Add(m.Value);
                Product.CategoryId = m.Value.Id;
                OnPropertyChanged(nameof(Product));
            });
            //_ = LoadCategoriesAsync();
        }

        public void Receive(EditProductMessage message)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadProductAsync(message.ProductId);
            });
        }

        public void Receive(CategoryCreatedMessage message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Categories.Add(message.Value);
                Product.CategoryId = message.Value.Id;
                OnPropertyChanged(nameof(Product));
            });
        }

        public async Task LoadProductAsync(int productId)
        {
            IsBusy = true;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                var categoriesList = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
                var productFromDb = await db.Products.FirstOrDefaultAsync(p => p.Id == productId);

                if (productFromDb != null)
                {
                    // 3. Gán ItemsSource (Categories) TRƯỚC
                    Categories.Clear();
                    foreach (var item in categoriesList)
                        Categories.Add(item);

                    // 4. Gán SelectedValue (Product) SAU
                    // Bằng cách gán Product, binding cho SelectedValue="{Binding Product.CategoryId}"
                    // sẽ được kích hoạt sau khi ItemsSource đã đầy đủ.
                    Product = productFromDb;
                    Image = ImageHelper.LoadBitmap(Product.ImagePath);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cancel();
                }
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void GoAddCategory()
        {
            _nav.Navigate(typeof(ThemDanhMucPage));
        }

        [RelayCommand]
        private void OpenPicture()
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Chọn ảnh sản phẩm",
                Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                Multiselect = false
            };
            if (ofd.ShowDialog() == true)
            {
                Product.ImagePath = ofd.FileName;
                Image = ImageHelper.LoadBitmap(ofd.FileName);
            }
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            Product.ValidateAll();
            if (Product.HasErrors)
            {
                var allErrors = Product.GetErrors()
                                       .Select(e => e.ErrorMessage)
                                       .Where(msg => !string.IsNullOrWhiteSpace(msg))
                                       .Distinct();
                var errorText = string.Join("\n• ", allErrors);
                MessageBox.Show("Vui lòng sửa các lỗi sau trước khi lưu:\n• " + errorText,
                    "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                db.Products.Update(Product);
                await db.SaveChangesAsync();

                // Gửi tin nhắn để trang danh sách tự làm mới
                WeakReferenceMessenger.Default.Send(new ProductsNeedRefreshMessage());

                //Điều hướng về trang quản lý
                _nav.Navigate(typeof(QuanLySanPhamPage));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu:\n{ex.Message}",
                    "KhoPro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _nav.Navigate(typeof(QuanLySanPhamPage));
        }

        [RelayCommand]
        private void GenerateRandomCode()
        {
            Product.ProductCode = $"SP-{DateTime.Now:yyMMddHHmmss}";
        }
    }
}