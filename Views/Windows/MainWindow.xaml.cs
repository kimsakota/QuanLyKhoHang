using UiDesktopApp1.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            
            //Tự động chuyển theme theo máy
            //SystemThemeWatcher.Watch(this);

            InitializeComponent();

            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);

            // Đăng ký sự kiện: "Này RootNavigation, khi nào điều hướng xong thì gọi OnNavigated nhé"
            RootNavigation.Navigated += OnNavigated;
        }

        // Phương thức xử lý sự kiện
        private void OnNavigated(object sender, NavigatedEventArgs e)
        {
            // Gọi phương thức public trong ViewModel để cập nhật header
            ViewModel.SetHeader(e.Page);
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        public bool? ShowDialogWindow() => ShowDialog();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
            => RootNavigation;

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            RootNavigation.SetServiceProvider(serviceProvider);
        }
    }
}
