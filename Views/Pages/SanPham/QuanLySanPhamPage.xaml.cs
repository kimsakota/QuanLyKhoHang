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
using UiDesktopApp1.Contracts;
using UiDesktopApp1.ViewModels.Pages.SanPham;
using UiDesktopApp1.Views.UserControls.SanPham;

namespace UiDesktopApp1.Views.Pages.SanPham
{
    /// <summary>
    /// Interaction logic for QuanLySanPhamPage.xaml
    /// </summary>
    public partial class QuanLySanPhamPage : Page, IHasHeader
    {
        public QuanLySanPhamViewModel ViewModel { get; set; }
        private QuanLySanPhamPageHeader _header;
        public QuanLySanPhamPage(QuanLySanPhamViewModel viewModel, QuanLySanPhamPageHeader header)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            _header = header;
        }
        public object? GetHeader() => _header;

    }
}
