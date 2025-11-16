using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class QuanLyNguoiDungViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService;
        private readonly CurrentUserService _currentUserService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<UserModel> _users = new();

        [ObservableProperty]
        private UserModel _selectedUser = new();

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _errorSummary = string.Empty;

        public QuanLyNguoiDungViewModel(IDbContextFactory<AppDbContext> dbContextFactory,
                                        IContentDialogService contentDialogService,
                                        CurrentUserService currentUserService)
        {
            _dbContextFactory = dbContextFactory;
            _contentDialogService = contentDialogService;
            _currentUserService = currentUserService;
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
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Users.Clear();
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var userList = await db.Users.AsNoTracking().ToListAsync();
                foreach (var user in userList)
                {
                    Users.Add(user);
                }
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task AddAsync()
        {
            // TODO: Tạo một UserControl dialog 'ThemSuaNguoiDungDialog'
            // tương tự như 'ThemSuaKhachHangDialog'
            // để nhập Username, Password, và Role.
            System.Windows.MessageBox.Show("Chức năng thêm người dùng mới cần được triển khai.", "Thông báo");
        }

        [RelayCommand]
        private async Task EditAsync(UserModel user)
        {
            if (user == null) return;
            // TODO: Mở dialog 'ThemSuaNguoiDungDialog' với thông tin của 'user'
            System.Windows.MessageBox.Show($"Chức năng sửa cho: {user.Username} cần được triển khai.", "Thông báo");
        }

        [RelayCommand]
        private async Task DeleteAsync(UserModel user)
        {
            if (user == null) return;

            // Không cho phép Admin tự xóa mình
            if (user.Id == _currentUserService.CurrentUser?.Id)
            {
                MessageBox.Show("Bạn không thể tự xóa tài khoản Admin của chính mình.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Bạn có chắc muốn xóa người dùng '{user.Username}' không?",
                                         "Xác nhận xóa",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                db.Users.Attach(user);
                db.Users.Remove(user);
                await db.SaveChangesAsync();

                Users.Remove(user); // Cập nhật UI
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }
    }
}