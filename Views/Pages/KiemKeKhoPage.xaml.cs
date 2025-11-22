using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
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

namespace UiDesktopApp1.Views.Pages
{
    /// <summary>
    /// Interaction logic for KiemKeKhoPage.xaml
    /// </summary>
    public partial class KiemKeKhoPage : Page, IHasHeader
    {
        public KiemKeKhoViewModel ViewModel { get; }
        private readonly UserControls.KiemKeKhoPageHeader _header;

        public KiemKeKhoPage(KiemKeKhoViewModel viewModel, UserControls.KiemKeKhoPageHeader header)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            _header = header;
            InitializeComponent();
        }
        public Object? GetHeader() => _header;
    }
}
