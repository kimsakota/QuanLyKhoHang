using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using UiDesktopApp1.ViewModels.Pages;
using UiDesktopApp1.ViewModels.Pages.BaoCao;
using UiDesktopApp1.ViewModels.Pages.LienHe;
using UiDesktopApp1.ViewModels.Pages.SanPham;
using UiDesktopApp1.ViewModels.Windows;
using UiDesktopApp1.Views.Pages;
using UiDesktopApp1.Views.Pages.BaoCao;
using UiDesktopApp1.Views.Pages.LienHe;
using UiDesktopApp1.Views.Pages.SanPham;
using UiDesktopApp1.Views.UserControls;
using UiDesktopApp1.Views.UserControls.Dialog;
using UiDesktopApp1.Views.UserControls.LienHe;
using UiDesktopApp1.Views.UserControls.SanPham;
using UiDesktopApp1.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;

namespace UiDesktopApp1
{
    public partial class App : Application
    {
        private static readonly IHost _host = CreateHostBuilder(Array.Empty<string>()).Build();

        public static IServiceProvider Services => _host.Services;

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            await _host.StartAsync();
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Handle unhandled exceptions here if needed
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)!);
                    c.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    // c.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
                    // c.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddNavigationViewPageProvider();

                    services.AddHostedService<ApplicationHostService>();

                    services.AddSingleton<IContentDialogService, ContentDialogService>();

                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<ITaskBarService, TaskBarService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<INavigationWindow, MainWindow>();
                    services.AddSingleton<CurrentUserService>();
                    services.AddTransient<MainWindowViewModel>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<LoginViewModel>();

                    //Sử dụng sqlsever
                    var connStr = context.Configuration.GetConnectionString("DefaultConnection")
                                  ?? "Server=localhost\\SQLEXPRESS;Database=QuanLyKhoHang;Trusted_Connection=True;TrustServerCertificate=True;";
                    services.AddDbContextFactory<AppDbContext>(opt => opt.UseSqlServer(connStr));

                    services.AddSingleton<ApiService>();

                    // Sử dụng sqllite (SQLite)
                    /*var connStr = context.Configuration.GetConnectionString("DefaultConnection")
                                  ?? "Data Source=QuanLyKhoHang.db";
                    services.AddDbContextFactory<AppDbContext>(opt => opt.UseSqlite(connStr));*/

                    services.AddSingleton<TaiChinhPage>();
                    services.AddSingleton<TaiChinhViewModel>();
                    services.AddSingleton<TonKhoPage>();
                    services.AddSingleton<TonKhoViewModel>();
                    services.AddSingleton<Views.Pages.BaoCao.KhachHangPage>();
                    services.AddSingleton<ViewModels.Pages.BaoCao.KhachHangViewModel>();

                    services.AddSingleton<SanPhamPage>();
                    services.AddSingleton<SanPhamPageHeader>();
                    services.AddTransient<SanPhamViewModel>();
                    services.AddSingleton<QuanLySanPhamPage>();
                    services.AddSingleton<QuanLySanPhamViewModel>();
                    services.AddSingleton<QuanLySanPhamPageHeader>();
                    services.AddSingleton<ThemSuaSanPhamDialog>();

                    services.AddSingleton<NhapKhoPage>();
                    services.AddSingleton<NhapKhoPageHeader>();
                    services.AddSingleton<NhapKhoViewModel>();

                    services.AddSingleton<XuatKhoPage>();
                    services.AddSingleton<XuatKhoPageHeader>();
                    services.AddSingleton<XuatKhoViewModel>();

                    services.AddSingleton<KiemKeKhoPage>();
                    services.AddSingleton<KiemKeKhoPageHeader>();
                    services.AddSingleton<KiemKeKhoViewModel>();

                    services.AddSingleton<LichSuPage>();
                    services.AddSingleton<LichSuViewModel>();

                    //Liên hệ
                    services.AddSingleton<Views.Pages.LienHe.KhachHangPage>();
                    services.AddSingleton<ViewModels.Pages.LienHe.KhachHangViewModel>();
                    services.AddSingleton<KhachHangPageHeader>();
                    services.AddTransient<ThemSuaKhachHangDialog>();

                    services.AddSingleton<NhaCungCapPage>();
                    services.AddSingleton<NhaCungCapViewModel>();
                    services.AddSingleton<NhaCungCapPageHeader>();
                    services.AddTransient<ThemSuaNhaCungCapDialog>();

                    services.AddSingleton<NhanVienPage>();
                    services.AddSingleton<NhanVienPageHeader>();
                    services.AddScoped<NhanVienViewModel>();

                    //Quản lý người dùng
                    services.AddSingleton<QuanLyNguoiDungPage>();
                    services.AddSingleton<QuanLyNguoiDungViewModel>();
                    services.AddSingleton<QuanLyNguoiDungPageHeader>();
                    services.AddTransient<ThemSuaNguoiDungDialog>();

                    services.AddTransient<SettingsDialog>();
                    services.AddScoped<SettingsViewModel>();
                });
    }
}
