using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UiDesktopApp1.Views.Windows;
using System.Diagnostics;
using UiDesktopApp1.Models;
using UiDesktopApp1.Views.Pages;
using Wpf.Ui;

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
            if (!cancellationToken.IsCancellationRequested)
                await InitializeDatabaseAndSeedUserAsync(cancellationToken);
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
                        if (!await dbContext.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
                        {
                            var adminUser = new UserModel
                            {
                                Username = "admin",
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), // Mật khẩu là 123
                                Role = "Admin"
                            };
                            dbContext.Users.Add(adminUser);
                            await dbContext.SaveChangesAsync(cancellationToken);
                            Debug.WriteLine("Admin user created.");
                        }
                        else
                        {
                            Debug.WriteLine("Admin user already exists.");
                        }
                    }
                    else // Bảng đã có dữ liệu, kiểm tra cụ thể user admin
                    {
                        if (!await dbContext.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
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
            // Đảm bảo code chạy trên UI thread nếu cần tương tác UI ngay lập tức
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Lấy LoginWindow (đã được inject ViewModel)
                var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();

                // Hiển thị LoginWindow và ĐỢI cho đến khi nó đóng lại
                // ShowDialog() phải chạy trên UI thread
                bool? dialogResult = loginWindow.ShowDialog(); // ShowDialog có thể trả về true/false/null

                // SAU KHI LoginWindow ĐÃ ĐÓNG, kiểm tra kết quả đăng nhập
                // IsLoginSuccessful được đặt trong LoginViewModel
                if (loginWindow.ViewModel != null && loginWindow.ViewModel.IsLoginSuccessful)
                {
                    // Lấy MainWindow (đã đăng ký là INavigationWindow)
                    var navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();
                    navigationWindow.ShowWindow(); // Chỉ hiển thị MainWindow

                    // === SỬ DỤNG INavigationService ĐỂ ĐIỀU HƯỚNG ===
                    bool navigationResult = _navigationService.Navigate(typeof(SanPhamPage)); // Điều hướng đến trang mặc định
                    if (!navigationResult)
                    {
                        Debug.WriteLine("Initial navigation failed using INavigationService.");
                        // Có thể hiển thị thông báo lỗi khác nếu cần
                        MessageBox.Show("Không thể điều hướng đến trang chính.", "Lỗi Điều hướng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    // ===============================================
                }
                else
                {
                    // Nếu không đăng nhập thành công (người dùng đóng cửa sổ), đóng ứng dụng
                    Debug.WriteLine("Login not successful or cancelled. Shutting down.");
                    Application.Current.Shutdown();
                }
            });
        }
    }
}
