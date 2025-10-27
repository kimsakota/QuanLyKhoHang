using Microsoft.EntityFrameworkCore;
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Controls;
using Wpf.Ui;

namespace UiDesktopApp1.ViewModels.Windows
{
    public partial class LoginViewModel : ObservableObject
    {
        public Action? CloseAction { get; set; }
        public bool IsLoginSuccessful { get; private set; } = false;

        [ObservableProperty]
        private string _username = "admin";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoggingIn = false;

        public string? Password { get; set; } 
        // ===========================================

        private readonly IAuthenticationService _authService;
        private readonly CurrentUserService _currentUserService;

        public LoginViewModel(IAuthenticationService authService, CurrentUserService currentUserService)
        {
            _authService = authService;
            _currentUserService = currentUserService;
        }

        private bool CanLogin() => !IsLoggingIn;

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            IsLoggingIn = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Tên đăng nhập không được để trống.";
                IsLoggingIn = false;
                return;
            }
            // === KIỂM TRA string Password ===
            if (string.IsNullOrEmpty(Password)) // Dùng IsNullOrEmpty cho string
            {
                ErrorMessage = "Mật khẩu không được để trống.";
                IsLoggingIn = false;
                return;
            }
            // ===============================

            try
            {
                // === GỌI SERVICE VỚI string Password ===
                UserModel? user = await _authService.AuthenticateAsync(Username, Password); // Truyền thẳng string
                // =======================================

                if (user != null)
                {
                    _currentUserService.SetCurrentUser(user);
                    IsLoginSuccessful = true;
                    CloseAction?.Invoke();
                }
                else
                {
                    ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: Vui lòng thử lại.";
                System.Diagnostics.Debug.WriteLine($"Login Error: {ex}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
    }
}
