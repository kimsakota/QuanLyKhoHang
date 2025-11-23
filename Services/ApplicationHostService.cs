using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UiDesktopApp1.Views.Windows;
using System.Diagnostics;
using UiDesktopApp1.Models;
using UiDesktopApp1.Views.Pages;
using Wpf.Ui;
using UiDesktopApp1.Views.Pages.LienHe;

namespace UiDesktopApp1.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly INavigationService _navigationService;

        //private INavigationWindow _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider, INavigationService navigationService)
        {
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;

        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Bước 1: Đảm bảo CSDL sẵn sàng
            await InitializeDatabaseAndSeedUserAsync(cancellationToken);

            // Bước 2: Xử lý hiển thị cửa sổ
            if (!cancellationToken.IsCancellationRequested) 
                await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        // === Hàm Khởi tạo CSDL và Seed User ===
        private async Task InitializeDatabaseAndSeedUserAsync(CancellationToken cancellationToken)
        {
            // Sử dụng scope để lấy DbContextFactory một cách an toàn
            using (var scope = _serviceProvider.CreateScope())
            {
                // Lấy DbContextFactory từ scope
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                try
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken); // Truyền cancellationToken

                    // Áp dụng các migration còn thiếu vào CSDL
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    Debug.WriteLine("Database migration applied successfully.");

                    // Kiểm tra và tạo user "admin" nếu chưa có
                    if (!await dbContext.Users.AnyAsync(cancellationToken)) // Kiểm tra nhanh hơn nếu bảng trống
                    {
                        if (!await dbContext.Users.AnyAsync(u => u.Username == "test", cancellationToken))
                        {
                            var adminUser = new UserModel
                            {
                                FullName = "Kim Linh",  
                                Username = "test",
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), // Mật khẩu là 123
                                Role = "Test",
                                Email = "linh.nk233495@sis.hust.edu.vn",
                                PhoneNumber = "0384022083"
                            };
                            dbContext.Users.Add(adminUser);
                            await dbContext.SaveChangesAsync(cancellationToken);
                            Debug.WriteLine("Test user created.");
                        }
                        else
                        {
                            Debug.WriteLine("Test user already exists.");
                        }

                    }
                    else // Bảng đã có dữ liệu, kiểm tra cụ thể user admin
                    {
                        if (!await dbContext.Users.AnyAsync(u => u.Username == "test", cancellationToken))
                        {
                            var adminUser = new UserModel { /* ... */ }; // Tạo user admin như trên
                            dbContext.Users.Add(adminUser);
                            await dbContext.SaveChangesAsync(cancellationToken);
                            Debug.WriteLine("Admin user created.");
                        }
                        else
                        {
                            Debug.WriteLine("Admin user already exists.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Database initialization cancelled.");
                    // Không cần làm gì thêm nếu bị hủy
                }
                catch (Exception ex) // Bắt các lỗi khác (VD: SQL Server không chạy)
                {
                    // Xử lý lỗi nghiêm trọng khi không thể cập nhật/kết nối CSDL
                    Debug.WriteLine($"CRITICAL ERROR: Database initialization failed: {ex}");
                    // Hiển thị lỗi trên UI thread
                    Application.Current.Dispatcher.Invoke(() => {
                        MessageBox.Show($"Không thể khởi tạo hoặc cập nhật cơ sở dữ liệu:\n{ex.Message}\n\nVui lòng kiểm tra kết nối SQL Server và thử lại.",
                                        "Lỗi Khởi Động Nghiêm Trọng", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown(1); // Thoát ứng dụng với mã lỗi
                    });
                }
            }
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            int a = 1;
            if (a == 0) 
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {

                    // Lấy MainWindow trực tiếp
                    var navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();

                    var mainWindowViewModel = (navigationWindow as MainWindow)?.ViewModel;
                    if (mainWindowViewModel != null)
                    {
                        Debug.WriteLine("Building menu with updated roles...");
                        mainWindowViewModel.BuildMenu();
                    }

                    navigationWindow.ShowWindow(); // Hiển thị MainWindow

                    // Điều hướng đến trang mặc định
                    bool navigationResult = _navigationService.Navigate(typeof(SanPhamPage));
                    if (!navigationResult)
                    {
                        Debug.WriteLine("Initial navigation failed using INavigationService.");
                    }
                });

            else 
                await Application.Current.Dispatcher.InvokeAsync(() => // Đảm bảo chạy trên UI thread
                {
                    var navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();

                    //navigationWindow.ShowWindow(); 

                    // 1. Lấy LoginWindow
                    var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();

                    // 2. Hiển thị và ĐỢI LoginWindow đóng
                    Debug.WriteLine("Showing LoginWindow...");
                    loginWindow.ShowDialog(); // Chặn ở đây
                    Debug.WriteLine("LoginWindow closed.");

                    // 3. KIỂM TRA KẾT QUẢ NGAY SAU KHI ĐÓNG
                    // Dùng ?? false để tránh lỗi nếu ViewModel bị null (dù không nên)
                    bool isLoginSuccess = loginWindow.ViewModel?.IsLoginSuccessful ?? false;
                    Debug.WriteLine($"Login successful: {isLoginSuccess}"); // In ra Output để kiểm tra

                    if (isLoginSuccess) // Chỉ tiếp tục nếu IsLoginSuccessful là true
                    {
                        loginWindow.Close();

                        var mainWindowViewModel = (navigationWindow as MainWindow)?.ViewModel;
                        if (mainWindowViewModel != null)
                        {
                            Debug.WriteLine("Building menu with updated roles...");
                            mainWindowViewModel.BuildMenu();
                        }

                        navigationWindow.ShowWindow();
                        var currentUserService = _serviceProvider.GetRequiredService<CurrentUserService>();
                        Type startPage;
                        switch (currentUserService.CurrentRole)
                        {
                            case Roles.Admin:
                                startPage = typeof(Views.Pages.QuanLyNguoiDungPage);
                                break;
                            case Roles.Manager:
                                startPage = typeof(Views.Pages.SanPhamPage); // Manager bắt đầu ở trang Sản phẩm
                                break;
                            case Roles.Test:
                                startPage = typeof(Views.Pages.SanPhamPage); // Manager bắt đầu ở trang Sản phẩm
                                break;
                            default: // Employee
                                startPage = typeof(Views.Pages.NhapKhoPage); // Employee bắt đầu ở trang Nhập kho
                                break;
                        }
                        _navigationService.Navigate(startPage);
                        //var navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();
                        //navigationWindow.ShowWindow();
                        // Điều hướng đến trang mặc định
                        //_navigationService.Navigate(typeof(SanPhamPage));
                    }
                    else
                    {
                        // Nếu không thành công, đóng ứng dụng
                        Debug.WriteLine("Login failed or cancelled. Shutting down.");
                        Application.Current.Shutdown();
                    }
                });
        }
    }
}
