using UiDesktopApp1.ViewModels.Windows;
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
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.Views.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginViewModel ViewModel { get; set; }
        public LoginWindow(LoginViewModel viewModel)
        {
            
            DataContext = viewModel;
            ViewModel = viewModel;

            InitializeComponent();

            ViewModel.CloseAction = () => this.Close();
        }

        private void Window_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Ép kiểu 'sender' về đúng kiểu Wpf.Ui.Controls.PasswordBox
            if (ViewModel != null && sender is Wpf.Ui.Controls.PasswordBox passwordBox)
            {
                // Truy cập thuộc tính Password (kiểu string)
                ViewModel.Password = passwordBox.Password; // <-- THAY ĐỔI Ở ĐÂY
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
