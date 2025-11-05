using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Pages.LienHe
{
    public partial class ThemKhachHangViewModel : ObservableObject
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        [ObservableProperty]
        private CustomerModel customer = new();

        [ObservableProperty]
        private bool _isBusy;

        public ThemKhachHangViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        [RelayCommand]
        public async Task<ContentDialogResult> SaveAsync(ContentDialog dialog)
        {
            Customer.ValidateAll();
            if(Customer.HasErrors)
            {
                return ContentDialogResult.None;
            }

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                await db.Customers.AddAsync(Customer);
                await db.SaveChangesAsync();

                // Gửi tin nhắn để KhachHangViewModel biết và tải lại danh sách
                WeakReferenceMessenger.Default.Send(new CustomerCreatedMessage(Customer));

                IsBusy = false;
                return ContentDialogResult.Primary; // Đóng dialog và trả về kết quả thành công
            }
            catch (Exception)
            {
                IsBusy = false;
                return ContentDialogResult.None;
            }
        }
    }
}
