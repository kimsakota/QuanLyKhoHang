using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media;
using UiDesktopApp1.Contracts;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using UiDesktopApp1.Views.Pages;
using UiDesktopApp1.Views.UserControls;
using UiDesktopApp1.Views.UserControls.Dialog;
using UiDesktopApp1.Views.UserControls.SanPham;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "KhoPro - Quản lý kho hàng";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new();

        /*[ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            *//*new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }*//*
        };*/

        [ObservableProperty]
        private ObservableCollection<System.Windows.Controls.MenuItem> _trayMenuItems = new()
        {
            new System.Windows.Controls.MenuItem { Header = "Home", Tag = "tray_home" }
        };

        [ObservableProperty]
        private object? _currentPageHeader;

        [ObservableProperty]
        private string _currentUserName = String.Empty;

        private readonly IServiceProvider _serviceProvider;
        private readonly CurrentUserService _currentUserService;
        private readonly IContentDialogService _contentDialogService;
        public MainWindowViewModel(IServiceProvider serviceProvider,
            CurrentUserService currentUserService,
            IContentDialogService contentDialogService)
        {
            _serviceProvider = serviceProvider;
            _currentUserService = currentUserService;
            _contentDialogService = contentDialogService;
        }

        public void BuildMenu()
        {
            Roles role = _currentUserService.CurrentRole;
            CurrentUserName = _currentUserService.CurrentUser?.Username ?? "Tài khoản";

            MenuItems = GenerateMenuItems(role);
        }
        public void SetHeader(object header)
        {
            CurrentPageHeader = (header as IHasHeader)?.GetHeader();
        }

        private ObservableCollection<object> GenerateMenuItems(Roles role)
        {
            var menu = new ObservableCollection<object>();

            switch(role)
            {
                case Roles.Admin:
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Quản lý người dùng",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.PeopleSettings20 },
                        TargetPageType = typeof(Views.Pages.QuanLyNguoiDungPage)
                    });
                    break;
                case Roles.Manager:
                    // Manager: Báo cáo, SP, Nhập, Xuất, Kiểm kê
                    menu.Add(CreateBaoCaoMenuItem());
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Sản phẩm",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Box16 },
                        TargetPageType = typeof(Views.Pages.SanPhamPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Nhập kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowLeft24 },
                        TargetPageType = typeof(Views.Pages.NhapKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Xuất kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowUp24 },
                        TargetPageType = typeof(Views.Pages.XuatKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Kiểm kê kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardCheckmark24 },
                        TargetPageType = typeof(Views.Pages.KiemKeKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Lịch sử",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.History24 },
                        TargetPageType = typeof(Views.Pages.LichSuPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Chi phí",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Money24 },
                        TargetPageType = typeof(Views.Pages.ChiPhiPage)
                    });
                    menu.Add(new NavigationViewItem()
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
                    });
                    break;
                case Roles.Employee:
                    // Employee: Báo cáo, Nhập, Xuất, Kiểm kê
                    menu.Add(CreateBaoCaoMenuItem());
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Nhập kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowLeft24 },
                        TargetPageType = typeof(Views.Pages.NhapKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Xuất kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowUp24 },
                        TargetPageType = typeof(Views.Pages.XuatKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Kiểm kê kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardCheckmark24 },
                        TargetPageType = typeof(Views.Pages.KiemKeKhoPage)
                    });
                    break;
                default:
                    // Manager: Báo cáo, SP, Nhập, Xuất, Kiểm kê
                    menu.Add(CreateBaoCaoMenuItem());
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Sản phẩm",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Box16 },
                        TargetPageType = typeof(Views.Pages.SanPhamPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Nhập kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowLeft24 },
                        TargetPageType = typeof(Views.Pages.NhapKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Xuất kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.BoxArrowUp24 },
                        TargetPageType = typeof(Views.Pages.XuatKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Kiểm kê kho",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardCheckmark24 },
                        TargetPageType = typeof(Views.Pages.KiemKeKhoPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Lịch sử",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.History24 },
                        TargetPageType = typeof(Views.Pages.LichSuPage)
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Chi phí",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.Money24 },
                        TargetPageType = typeof(Views.Pages.ChiPhiPage)
                    });
                    menu.Add(new NavigationViewItem()
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
                    });
                    menu.Add(new NavigationViewItem()
                    {
                        Content = "Quản lý người dùng",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.PeopleSettings20 },
                        TargetPageType = typeof(Views.Pages.QuanLyNguoiDungPage)
                    });
                    break;
            }
            return menu;
        }

        private NavigationViewItem CreateBaoCaoMenuItem()
        {
            return new NavigationViewItem()
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
            };
        }

        [ObservableProperty]
        private bool _isFlyoutOpen = false;

        [RelayCommand]
        private void OnButtonClick(object sender)
        {
            if (!IsFlyoutOpen)
            {
                IsFlyoutOpen = true;
            }
        }

        [RelayCommand]
        private async Task NavigateToSettingsAsync() // Đổi tên và làm async
        {
            IsFlyoutOpen = false; // Tự động đóng flyout

            // Lấy dialog từ service provider
            var dialogControl = App.Services.GetRequiredService<SettingsDialog>();

            // Tạo ContentDialog
            var dialog = new ContentDialog
            {
                Title = "Cài đặt",
                Content = dialogControl,
                CloseButtonText = "Đóng",
                DefaultButton = ContentDialogButton.Close
            };

            // Hiển thị dialog
            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        [RelayCommand]
        private void LogOut()
        {
            // Xóa phiên đăng nhập
            _currentUserService.ClearCurrentUser();

            // Lấy đường dẫn của file .exe hiện tại
            var processPath = Environment.ProcessPath;
            if (processPath != null)
            {
                // Khởi động một tiến trình mới (mở lại ứng dụng)
                Process.Start(processPath);
            }

            // Đóng ứng dụng hiện tại
            Application.Current.Shutdown();
        }
    }
}
