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
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.Views.Pages
{
    /// <summary>
    /// Interaction logic for NhapKhoPage.xaml
    /// </summary>
    public partial class NhapKhoPage : Page, IHasHeader
    {
        public NhapKhoViewModel ViewModel { get; }
        private readonly NhapKhoPageHeader _header;
        public NhapKhoPage(NhapKhoViewModel viewModel, NhapKhoPageHeader header)
        {
            ViewModel = viewModel;
            DataContext = this;
            _header = header;

            InitializeComponent();
        }
        public object? GetHeader() => _header;
    }
}
