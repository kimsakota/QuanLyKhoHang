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

namespace UiDesktopApp1.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ThemSuaNguoiDungDialog.xaml
    /// </summary>
    public partial class ThemSuaNguoiDungDialog : UserControl
    {
        public QuanLyNguoiDungViewModel ViewModel { get; }
        public ThemSuaNguoiDungDialog(QuanLyNguoiDungViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }
    }
}
