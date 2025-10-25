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
using UiDesktopApp1.ViewModels.Pages.BaoCao;

namespace UiDesktopApp1.Views.Pages.BaoCao
{
    /// <summary>
    /// Interaction logic for KhachHangPage.xaml
    /// </summary>
    public partial class KhachHangPage : Page
    {
        public KhachHangViewModel ViewModel { get; }

        public KhachHangPage(KhachHangViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
