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

namespace UiDesktopApp1.Views.Pages
{
    /// <summary>
    /// Interaction logic for XuatKhoPage.xaml
    /// </summary>
    public partial class XuatKhoPage : Page
    {
        public XuatKhoViewModel ViewModel { get; }

        public XuatKhoPage(XuatKhoViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();
        }
    }
}
