using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Models;
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
    public partial class QuanLyNguoiDungViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService;
        private readonly CurrentUserService _currentUserService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<UserModel> _users = new();

        [ObservableProperty]
        private UserModel? _selectedUser;

        [ObservableProperty]
        private UserModel _userForDialog = new();

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

        private async Task<bool> SaveAsync(bool isEdit)
        {
            UserForDialog.ValidateAll();

            bool passwordError = false;

            if (!isEdit && string.IsNullOrWhiteSpace(DialogPassword))
                passwordError = true;

            if (UserForDialog.HasErrors || passwordError)
            {
                var allErrors = UserForDialog.GetErrors()
                                        .Select(e => e.ErrorMessage)
                                        .Where(msg => !string.IsNullOrWhiteSpace(msg))
                                        .Distinct()
                                        .ToList();
                if (passwordError)
                    allErrors.Add("Mật khẩu là bắt buộc khi thêm mới.");
                ErrorSummary = string.Join("\n", allErrors);
                return false;
            }

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                // Nếu người dùng có nhập mật khẩu, hash nó
                if (!string.IsNullOrWhiteSpace(DialogPassword))
                {
                    // Sử dụng BCrypt (giống như trong ApplicationHostService)
                    UserForDialog.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DialogPassword);
                }

                if (isEdit)
                {
                    db.Users.Update(UserForDialog);
                }
                else
                {
                    // Kiểm tra tên đăng nhập trùng
                    if (await db.Users.AnyAsync(u => u.Username == UserForDialog.Username))
                    {
                        ErrorSummary = "Tên đăng nhập này đã tồn tại.";
                        IsBusy = false;
                        return false;
                    }
                    db.Users.Add(UserForDialog);
                }

                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                ErrorSummary = "Lỗi khi lưu: " + ex.Message;
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddAsync()
        {
            var dialogContent = App.Services.GetRequiredService<ThemSuaNguoiDungDialog>();

            // Chuẩn bị dữ liệu cho dialog
            UserForDialog = new UserModel(); // Tạo object mới
            DialogPassword = string.Empty;
            ErrorSummary = string.Empty;

            // Hiển thị lại text trợ giúp cho mật khẩu
            var passwordHelpText = dialogContent.FindName("PasswordHelpText") as TextBlock;
            if (passwordHelpText != null)
            {
                passwordHelpText.Text = " *(Bắt buộc khi thêm mới)";
                passwordHelpText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkRed);
            }

            var dialog = new ContentDialog
            {
                Title = "Thêm người dùng mới",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    var ok = await SaveAsync(isEdit: false);
                    if (!ok)
                        e.Cancel = true;
                    else
                        Users.Add(UserForDialog); // Thêm vào danh sách UI
                }
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        [RelayCommand]
        private async Task EditAsync(UserModel user)
        {
            if (user == null) return;

            var dialogContent = App.Services.GetRequiredService<ThemSuaNguoiDungDialog>();

            // Chuẩn bị dữ liệu cho dialog
            DialogPassword = string.Empty; // Xóa mật khẩu cũ
            ErrorSummary = string.Empty;

            // Clone object để chỉnh sửa (tránh ảnh hưởng đến DataGrid)
            UserForDialog = new UserModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                PasswordHash = user.PasswordHash // Giữ lại hash cũ
            };

            // Thay đổi text trợ giúp mật khẩu
            var passwordHelpText = dialogContent.FindName("PasswordHelpText") as TextBlock;
            if (passwordHelpText != null)
            {
                passwordHelpText.Text = "(Để trống nếu không muốn thay đổi)";
                passwordHelpText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            }

            var dialog = new ContentDialog
            {
                Title = "Sửa thông tin người dùng",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    e.Cancel = true; // Giữ dialog mở
                    var ok = await SaveAsync(isEdit: true);
                    if (ok)
                    {
                        // Cập nhật lại item trong danh sách UI
                        var index = Users.IndexOf(user);
                        if (index != -1)
                            Users[index] = UserForDialog; // Thay thế bằng object đã sửa

                        e.Cancel = false; // Đóng dialog
                    }
                }
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        [RelayCommand]
        private async Task DeleteAsync(UserModel user)
        {
            if (user == null) return;

            // Không cho phép Admin tự xóa mình
            if (user.Id == _currentUserService.CurrentUser?.Id)
            {
                MessageBox.Show("Bạn không thể tự xóa tài khoản của chính mình.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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