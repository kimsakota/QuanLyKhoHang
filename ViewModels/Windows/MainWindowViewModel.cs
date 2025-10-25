using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media;
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Views.Pages;
using UiDesktopApp1.Views.UserControls;
using UiDesktopApp1.Views.UserControls.SanPham;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "KhoPro - Quản lý kho hàng";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Báo cáo",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home12 },
                MenuItems =
                {
                    new NavigationViewItem()
                    {
                        Content = "Tài chính",
                        TargetPageType = typeof(Views.Pages.BaoCao.TaiChinhPage)
                    },
                    new NavigationViewItem()
                    {
                        Content = "Tồn kho",
                        TargetPageType = typeof(Views.Pages.BaoCao.TonKhoPage)
                    },
                    new NavigationViewItem()
                    {
                        Content = "Khách hàng",
                        TargetPageType = typeof(Views.Pages.BaoCao.KhachHangPage)
                    }
                }
            },
            new NavigationViewItem()
            {
                Content = "Sản phẩm",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Box16 },
                TargetPageType = typeof(Views.Pages.SanPhamPage)
            },
            new NavigationViewItem()
            {
                Content = "Nhập kho",
                Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowLeft24 },
                TargetPageType = typeof(Views.Pages.NhapKhoPage)
            },
            new NavigationViewItem()
            {
                Content = "Xuất kho",
                Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowUp24 },
                TargetPageType = typeof(Views.Pages.XuatKhoPage)
            },
            new NavigationViewItem()
            {
                Content = "Kiểm kê kho",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardCheckmark24 },
                TargetPageType = typeof(Views.Pages.KiemKeKhoPage)
            },
            new NavigationViewItem()
            {
                Content = "Chuyển kho",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TrayItemRemove24 },
                TargetPageType = typeof(Views.Pages.ChuyenKhoPage)
            },
            new NavigationViewItem()
            {
                Content = "Lịch sử",
                Icon = new SymbolIcon { Symbol = SymbolRegular.History24 },
                TargetPageType = typeof(Views.Pages.LichSuPage)
            },
            new NavigationViewItem()
            {
                Content = "Chi phí",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Money24 },
                TargetPageType = typeof(Views.Pages.ChiPhiPage)
            },
            new NavigationViewItem()
            {
                Content = "Liên hệ",
                Icon = new SymbolIcon { Symbol = SymbolRegular.PersonCall24 },
                MenuItems = 
                {
                    new NavigationViewItem()
                    {
                        Content = "Khách hàng",
                        TargetPageType = typeof(Views.Pages.LienHe.KhachHangPage)
                    },
                    new NavigationViewItem()
                    {
                        Content = "Nhà cung cấp",
                        TargetPageType = typeof(Views.Pages.LienHe.NhaCungCapPage)
                    },
                    new NavigationViewItem()
                    {
                        Content = "Nhân viên",
                        TargetPageType = typeof(Views.Pages.LienHe.NhanVienPage)
                    }
                }
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<System.Windows.Controls.MenuItem> _trayMenuItems = new()
        {
            new System.Windows.Controls.MenuItem { Header = "Home", Tag = "tray_home" }
        };

        [ObservableProperty]
        private object? _currentPageHeader;

        private readonly IServiceProvider _serviceProvider;
        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void SetHeader(object header)
        {
            CurrentPageHeader = (header as IHasHeader)?.GetHeader();
        }
    }
}
