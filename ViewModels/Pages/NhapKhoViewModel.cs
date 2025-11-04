using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class NhapKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        [ObservableProperty]
        private DateTime _ngayNhap;
        public NhapKhoViewModel (INavigationService navigationService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _navigationService = navigationService;
            _dbContextFactory = dbContextFactory;

        }

        public Task OnNavigatedToAsync()
        {
            NgayNhap = DateTime.Now;
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }

    }
}
