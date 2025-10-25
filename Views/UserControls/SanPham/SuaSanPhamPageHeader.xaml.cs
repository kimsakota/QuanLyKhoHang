using UiDesktopApp1.ViewModels.Pages.SanPham;
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

namespace UiDesktopApp1.Views.UserControls.SanPham
{
    /// <summary>
    /// Interaction logic for SuaSanPhamPageHeader.xaml
    /// </summary>
    public partial class SuaSanPhamPageHeader : UserControl
    {
        public SuaSanPhamViewModel ViewModel { get; set; }
        public SuaSanPhamPageHeader(SuaSanPhamViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            ViewModel = viewModel;
        }
    }
}
