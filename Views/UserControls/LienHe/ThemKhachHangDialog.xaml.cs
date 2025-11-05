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
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.Views.UserControls.LienHe
{
    /// <summary>
    /// Interaction logic for ThemKhachHangDialog.xaml
    /// </summary>
    public partial class ThemKhachHangDialog : ContentControl
    {
        public ThemKhachHangViewModel ViewModel { get; }

        public ThemKhachHangDialog(ThemKhachHangViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

       
    }
}
