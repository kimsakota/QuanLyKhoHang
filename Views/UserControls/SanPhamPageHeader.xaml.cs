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
using UiDesktopApp1.ViewModels.UserControls;

namespace UiDesktopApp1.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SanPhamPageHeader.xaml
    /// </summary>
    public partial class SanPhamPageHeader : UserControl
    {
        public SanPhamPageHeaderViewModel ViewModel { get; }
        public SanPhamPageHeader(SanPhamPageHeaderViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
