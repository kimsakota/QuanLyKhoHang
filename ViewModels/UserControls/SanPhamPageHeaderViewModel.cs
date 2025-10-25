using Microsoft.Extensions.DependencyInjection;
using UiDesktopApp1.Views.Pages.SanPham;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.ViewModels.Windows;
using UiDesktopApp1.Views.UserControls.SanPham;
using Wpf.Ui;

namespace UiDesktopApp1.ViewModels.UserControls
{
    public partial class SanPhamPageHeaderViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public SanPhamPageHeaderViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        private void AddProduct()
        {
            _navigationService.Navigate(typeof(ThemSanPhamPage));
        }

        [RelayCommand]
        private void Manage()
        {
            _navigationService.Navigate(typeof(QuanLySanPhamPage));
        }
    }
}
