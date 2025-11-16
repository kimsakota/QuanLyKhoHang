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

namespace UiDesktopApp1.Views.UserControls
{
    /// <summary>
    /// Interaction logic for QuanLyNguoiDungPageHeader.xaml
    /// </summary>
    public partial class QuanLyNguoiDungPageHeader : UserControl
    {
        public QuanLyNguoiDungViewModel ViewModel { get; }
        public QuanLyNguoiDungPageHeader(QuanLyNguoiDungViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            ViewModel = viewModel;
        }
    }
}
