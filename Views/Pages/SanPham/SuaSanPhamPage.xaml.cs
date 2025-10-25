using UiDesktopApp1.ViewModels.Pages.SanPham;
using UiDesktopApp1.Views.UserControls.SanPham;
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

namespace UiDesktopApp1.Views.Pages.SanPham
{
    /// <summary>
    /// Interaction logic for SuaSanPhamPage.xaml
    /// </summary>
    public partial class SuaSanPhamPage : Page, IHasHeader
    {
        public SuaSanPhamViewModel ViewModel { get; set; }
        private SuaSanPhamPageHeader _header;
        public SuaSanPhamPage(SuaSanPhamViewModel viewModel, SuaSanPhamPageHeader header)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            _header = header;
        }
        public object? GetHeader() => _header;
    }
}
