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
using UiDesktopApp1.ViewModels.Pages.BaoCao;
using UiDesktopApp1.ViewModels.Pages.LienHe;
using UiDesktopApp1.Views.UserControls.LienHe;

namespace UiDesktopApp1.Views.Pages.LienHe
{
    /// <summary>
    /// Interaction logic for KhachHangPage.xaml
    /// </summary>
    public partial class KhachHangPage : Page, IHasHeader
    {
        public ViewModels.Pages.LienHe.KhachHangViewModel ViewModel { get; }
        public KhachHangPageHeader _header;
        public KhachHangPage(ViewModels.Pages.LienHe.KhachHangViewModel viewModel, KhachHangPageHeader header)
        {

            ViewModel = viewModel;
            DataContext = viewModel;
            _header = header;

            InitializeComponent();
        }

        public object? GetHeader() => _header;
    }
}
