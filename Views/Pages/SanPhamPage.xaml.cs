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
using UiDesktopApp1.ViewModels.Pages;
using UiDesktopApp1.Views.UserControls;
using UiDesktopApp1.Views.UserControls.SanPham;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.Views.Pages
{
    /// <summary>
    /// Interaction logic for SanPhamPage.xaml
    /// </summary>
    public partial class SanPhamPage : Page, IHasHeader
    {
        public SanPhamViewModel ViewModel { get; }
        private readonly SanPhamPageHeader _header;

        public SanPhamPage(SanPhamViewModel viewModel, SanPhamPageHeader header)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
            _header = header;
        }
        public object? GetHeader() => _header;
    }
}
