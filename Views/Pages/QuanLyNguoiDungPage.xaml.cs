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

namespace UiDesktopApp1.Views.Pages
{
    /// <summary>
    /// Interaction logic for QuanLyNguoiDungPage.xaml
    /// </summary>
    public partial class QuanLyNguoiDungPage : Page, IHasHeader
    {
        public QuanLyNguoiDungViewModel ViewModel { get; }
        private readonly QuanLyNguoiDungPageHeader _header;
        public QuanLyNguoiDungPage(QuanLyNguoiDungViewModel viewModel, QuanLyNguoiDungPageHeader header)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel; 
            _header = header;
        }
        public object? GetHeader() => _header;
    }
}
