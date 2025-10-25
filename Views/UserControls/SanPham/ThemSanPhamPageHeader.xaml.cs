using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UiDesktopApp1.ViewModels.Pages;
using UiDesktopApp1.ViewModels.Pages.SanPham;

namespace UiDesktopApp1.Views.UserControls.SanPham
{
    /// <summary>
    /// Interaction logic for ThemSanPhamPageHeader.xaml
    /// </summary>
    public partial class ThemSanPhamPageHeader : UserControl
    {
        public ThemSanPhamViewModel ViewModel { get; set; }
        public ThemSanPhamPageHeader(ThemSanPhamViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
