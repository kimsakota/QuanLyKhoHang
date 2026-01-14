using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using UiDesktopApp1.Services;
using UiDesktopApp1.Views.UserControls;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class QuanLyNguoiDungViewModel : ObservableValidator, INavigationAware
    {
        //private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ApiService _apiService;
        private readonly IContentDialogService _contentDialogService;
        private readonly CurrentUserService _currentUserService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<UserModel> _users = new();

        [ObservableProperty]
        private UserModel? _selectedUser = null;

        [ObservableProperty]
        private UserModel _userForDialog = new();

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [ObservableProperty]
        private string _dialogPassword = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _errorSummary = string.Empty;

        public List<string> AvailableRoles { get; } = new List<string>
        {
            Roles.Admin.ToString(),
            Roles.Manager.ToString(),
            Roles.Employee.ToString()
        };

        public QuanLyNguoiDungViewModel(IContentDialogService contentDialogService,
                                        CurrentUserService currentUserService,
                                        ApiService apiService)
        {
            _contentDialogService = contentDialogService;
            _currentUserService = currentUserService;
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
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                Users.Clear();
                /*await using var db = await _dbContextFactory.CreateDbContextAsync();
                var userList = await db.Users.AsNoTracking().ToListAsync();*/
                var userList = await _apiService.GetAllAsync<UserModel>("Users");

                foreach (var user in userList)
                    Users.Add(user);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task AddAsync() => await ShowUserDialogAsync(null);

        [RelayCommand]
        private async Task EditAsync() => await ShowUserDialogAsync(SelectedUser);

        private async Task ShowUserDialogAsync(UserModel? user)
        {
            if(user == null && SelectedUser != null) SelectedUser = null;
            ClearErrors();
            UserForDialog = user == null ? new UserModel() : new UserModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                PasswordHash = user.PasswordHash
            };

            DialogPassword = string.Empty;
            ErrorSummary = string.Empty;

            var dialogContent = App.Services.GetRequiredService<ThemSuaNguoiDungDialog>();  

            var passwordHelpText = dialogContent.FindName("PasswordHelpText") as TextBlock;
            if(passwordHelpText != null)
            {
                bool isNewUser = user == null;
                passwordHelpText.Text = isNewUser ?
                    "Mật khẩu ban đầu cho người dùng mới." :
                    "Để trống nếu không muốn thay đổi mật khẩu.";
                passwordHelpText.Foreground = isNewUser ?
                    new SolidColorBrush(Colors.Red) :
                    new SolidColorBrush(Colors.Gray);
            }

            var dialog = new ContentDialog
            {
                Title = user == null ? "Thêm người dùng mới" : "Sửa thông tin người dùng",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    bool success = await HandleSaveToDbAsync();
                    if (!success) e.Cancel = true;
                }
            };
            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        private async Task<bool> HandleSaveToDbAsync()
        {
            UserForDialog.ValidateAll();
            ClearErrors(nameof(DialogPassword));

            var errors = UserForDialog.GetErrors()
                                        .Select(e => e.ErrorMessage)
                                        .Where(msg => !string.IsNullOrWhiteSpace(msg))
                                        .Distinct()
                                        .ToList();
            
            if (string.IsNullOrWhiteSpace(UserForDialog.PasswordHash) && string.IsNullOrWhiteSpace(DialogPassword))
            {
                ValidateProperty(DialogPassword, nameof(DialogPassword));
                var passwordError = GetErrors(nameof(DialogPassword))
                                    .Select(e => e.ErrorMessage)
                                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(passwordError))
                    errors.Add(passwordError);
            }

            if (errors.Any())
            {
                ErrorSummary = string.Join("\n", errors);
                return false;
            }
            IsBusy = true;
            try
            {
                //await using var db = await _dbContextFactory.CreateDbContextAsync();
                bool isNew = UserForDialog.Id == 0;
                if(isNew)
                {
                    /*if(await db.Users.AnyAsync(u => u.Username == UserForDialog.Username))
                    {
                        ErrorSummary = "Tên đăng nhập này đã tồn tại.";
                        return false;
                    }*/
                    if(await _apiService.CheckExistsAsync("Users", "Username" ,UserForDialog.Username!))
                    {
                        ErrorSummary = "Tên đăng nhập này đã tồn tại.";
                        return false;
                    }
                    UserForDialog.PasswordHash = DialogPassword;
                    //UserForDialog.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DialogPassword);
                    //db.Users.Add(UserForDialog);
                    //await db.SaveChangesAsync();
                    if(UserForDialog != null)
                    {
                        var result = await _apiService.AddAsync<UserModel, UserModel>("Users", UserForDialog);
                        if(result != null)
                        {
                            Users.Add(result);
                            SelectedUser = Users[Users.Count - 1];
                        }
                    }
                }
                else
                {
                    //db.Users.Update(UserForDialog);
                    //await db.SaveChangesAsync();
                    UserForDialog.PasswordHash = DialogPassword;
                    await _apiService.UpdateAsync("Users", UserForDialog.Id, UserForDialog);

                    var index = Users.IndexOf(SelectedUser!);
                    if(index >= 0) Users[index] = UserForDialog;
                    SelectedUser = UserForDialog;
                }
                return true;
            }
            catch(Exception ex)
            {
                ErrorSummary = "Lỗi khi lưu: " + ex.Message;
                return false;
            }
            finally
            {
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new NotifyRefreshMessage(RefreshType.ProductList));
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedUser == null) return;
            if (_currentUserService.CurrentUser != null &&
                SelectedUser.Username == _currentUserService.CurrentUser.Username)
            {
                MessageBox.Show("Bạn không thể xóa tài khoản đang đăng nhập!",
                    "Thao tác bị từ chối", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa người dùng '{SelectedUser.FullName}' ({SelectedUser.Username}) không?\nHành động này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            IsBusy = true;
            try
            {
                /*await using var db = await _dbContextFactory.CreateDbContextAsync();
                
                await db.Users.Where(u => u.Id == SelectedUser.Id)
                              .ExecuteDeleteAsync();*/
                await _apiService.DeleteAsync("Users", SelectedUser.Id);
                Users.Remove(SelectedUser);
                SelectedUser = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa người dùng:\n{ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}