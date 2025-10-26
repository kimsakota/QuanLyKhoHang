using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UiDesktopApp1.Views.Pages.SanPham;
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
using UiDesktopApp1.ViewModels.UserControls;
using UiDesktopApp1.ViewModels.UserControls.SanPham;
using UiDesktopApp1.ViewModels.Windows;
using UiDesktopApp1.Views.Pages;
using UiDesktopApp1.Views.Pages.BaoCao;
using UiDesktopApp1.Views.Pages.LienHe;
using UiDesktopApp1.Views.UserControls;
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

                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<ITaskBarService, TaskBarService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddScoped<IAuthenticationService, AuthenticationService>();
                    services.AddTransient<INavigationWindow, MainWindow>();
                    services.AddSingleton<CurrentUserService>();
                    services.AddTransient<MainWindowViewModel>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<LoginViewModel>();

                    var connStr = context.Configuration.GetConnectionString("DefaultConnection")
                                  ?? "Server=localhost\\SQLEXPRESS;Database=QuanLyKhoHang;Trusted_Connection=True;TrustServerCertificate=True;";
                    services.AddDbContextFactory<AppDbContext>(opt => opt.UseSqlServer(connStr));

                    services.AddSingleton<TaiChinhPage>();
                    services.AddScoped<TaiChinhViewModel>();
                    services.AddSingleton<TonKhoPage>();
                    services.AddScoped<TonKhoViewModel>();
                    services.AddSingleton<Views.Pages.BaoCao.KhachHangPage>();
                    services.AddScoped<ViewModels.Pages.BaoCao.KhachHangViewModel>();
                    services.AddSingleton<SanPhamPage>();
                    services.AddTransient<SanPhamViewModel>();
                    services.AddSingleton<NhapKhoPage>();
                    services.AddScoped<NhapKhoViewModel>();
                    services.AddSingleton<XuatKhoPage>();
                    services.AddScoped<XuatKhoViewModel>();
                    services.AddSingleton<KiemKeKhoPage>();
                    services.AddScoped<KiemKeKhoViewModel>();
                    services.AddSingleton<ChuyenKhoPage>();
                    services.AddScoped<ChuyenKhoViewModel>();
                    services.AddSingleton<LichSuPage>();
                    services.AddScoped<LichSuViewModel>();
                    services.AddSingleton<ChiPhiPage>();
                    services.AddScoped<ChiPhiViewModel>();

                    services.AddSingleton<Views.Pages.LienHe.KhachHangPage>();
                    services.AddScoped<ViewModels.Pages.LienHe.KhachHangViewModel>();
                    services.AddSingleton<NhaCungCapPage>();
                    services.AddScoped<NhaCungCapViewModel>();
                    services.AddSingleton<NhanVienPage>();
                    services.AddScoped<NhanVienViewModel>();

                    services.AddSingleton<SettingsPage>();
                    services.AddScoped<SettingsViewModel>();

                    services.AddTransient<ThemSanPhamPage>();
                    services.AddScoped<ThemSanPhamViewModel>();

                    services.AddSingleton<SanPhamPageHeader>();
                    services.AddScoped<SanPhamPageHeaderViewModel>();
                    services.AddSingleton<NhapKhoPageHeader>();
                    services.AddScoped<NhapKhoPageHeaderViewModel>();
                    services.AddTransient<ThemSanPhamPageHeader>();
                    services.AddScoped<ThemSanPhamPageHeaderViewModel>();
                    services.AddSingleton<ThemDanhMucPage>();
                    services.AddScoped<ThemDanhMucViewModel>();
                    services.AddSingleton<QuanLySanPhamPage>();
                    services.AddScoped<QuanLySanPhamViewModel>();
                    services.AddSingleton<QuanLySanPhamPageHeader>();
                    services.AddSingleton<SuaSanPhamPage>();
                    services.AddScoped<SuaSanPhamViewModel>();
                    services.AddSingleton<SuaSanPhamPageHeader>();

                });
    }
}
