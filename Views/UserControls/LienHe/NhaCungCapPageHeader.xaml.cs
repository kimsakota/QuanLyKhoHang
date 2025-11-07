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
using UiDesktopApp1.ViewModels.Pages.LienHe;

namespace UiDesktopApp1.Views.UserControls.LienHe
{
    /// <summary>
    /// Interaction logic for NhaCungCapPageHeader.xaml
    /// </summary>
    public partial class NhaCungCapPageHeader : UserControl
    {
        public NhaCungCapViewModel ViewModel { get; set; }
        public NhaCungCapPageHeader(NhaCungCapViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
