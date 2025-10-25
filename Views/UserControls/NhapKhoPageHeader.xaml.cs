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
using UiDesktopApp1.ViewModels.UserControls;

namespace UiDesktopApp1.Views.UserControls
{
    /// <summary>
    /// Interaction logic for NhapKhoPageHeader.xaml
    /// </summary>
    public partial class NhapKhoPageHeader : UserControl
    {
        public NhapKhoPageHeader(NhapKhoPageHeaderViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
