using System.Windows.Controls;
using UiDesktopApp1.ViewModels.Pages.BaoCao;

namespace UiDesktopApp1.Views.Pages.BaoCao
{
    /// <summary>
    /// Interaction logic for NhaCungCapPage.xaml
    /// </summary>
    public partial class NhaCungCapPage : Page
    {
        public NhaCungCapViewModel ViewModel { get; }

        public NhaCungCapPage(NhaCungCapViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();
        }
    }
}