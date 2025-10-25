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

        //private INavigationWindow _navigationWindow;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
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

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            // === THÊM KHỐI NÀY ĐỂ TẠO ADMIN USER ===
            try
            {
                // Tạo một "scope" mới để lấy DbContext và chạy async
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Lấy DbContextFactory thay vì DbContext
                    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync();

                    // Kiểm tra xem user "admin" đã tồn tại chưa
                    if (!await dbContext.Users.AnyAsync(u => u.Username == "admin"))
                    {
                        // Nếu chưa, tạo mới
                        var adminUser = new UserModel
                        {
                            Username = "admin",
                            // Hash mật khẩu "123"
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                            Role = "Admin" // Gán quyền Admin
                        };
                        dbContext.Users.Add(adminUser);
                        await dbContext.SaveChangesAsync();
                        Debug.WriteLine("Admin user created."); // Ghi log
                    }
                    else
                    {
                        Debug.WriteLine("Admin user already exists."); // Ghi log
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu CSDL chưa được tạo, hoặc có lỗi kết nối
                Debug.WriteLine($"Error seeding user: {ex.Message}");
                // (Bạn có thể hiển thị MessageBox ở đây nếu muốn)
            }
            // === KẾT THÚC KHỐI SEED ===


            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.ShowDialog();

            if (loginWindow.ViewModel.IsLoginSuccessful)
            {
                var navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();
                navigationWindow.ShowWindow();
                navigationWindow.Navigate(typeof(SanPhamPage));
            }
            else
            {
                Application.Current.Shutdown();
            }
            await Task.CompletedTask;
        }
    }
}
