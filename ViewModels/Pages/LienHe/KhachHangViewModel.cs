using Azure.Core;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UiDesktopApp1.Models;
using UiDesktopApp1.Models.Messages;
using UiDesktopApp1.Views.UserControls.LienHe;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Pages.LienHe
{
    public partial class KhachHangViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService; 
        private bool _isInitialized = false;

        public ObservableCollection<CustomerModel> Customers { get; } = new();

        public ICollectionView CustomersView { get; }

        [ObservableProperty]
        private CustomerModel? _selectedCustomer;
        [ObservableProperty]
        private CustomerModel _customer = new();
        [ObservableProperty] 
        private bool isBusy = false;
        [ObservableProperty]
        private string _errorSummary = string.Empty;
        [ObservableProperty]
        private string _searchText = string.Empty;

        public KhachHangViewModel(IDbContextFactory<AppDbContext> dbContextFactory,
            IContentDialogService contentDialogService)
        {
            _dbContextFactory = dbContextFactory;
            _contentDialogService = contentDialogService; 
            CustomersView = CollectionViewSource.GetDefaultView(Customers);
            CustomersView.Filter = FilterCustomers;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                await LoadDataAsync();
                _isInitialized = true;
            }
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }

        private async Task LoadDataAsync()
        {
            Customers.Clear();

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var customerList = await dbContext.Customers.AsNoTracking().ToListAsync();

            foreach (var customer in customerList)
                Customers.Add(customer);
        }

        private async Task<bool> SaveAsync(bool isEdit)
        {
            Customer.ValidateAll();
            if (Customer.HasErrors)
            {
                var allErrors = Customer.GetErrors()
                                        .Select(e => e.ErrorMessage)
                                        .Where(msg => !string.IsNullOrWhiteSpace(msg))
                                        .Distinct();
                ErrorSummary = string.Join("\n", allErrors);
                return false;
            }

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                if (isEdit)
                    db.Customers.Update(Customer);
                else db.Customers.Add(Customer);

                await db.SaveChangesAsync();
                await LoadDataAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorSummary = "Lỗi khi lưu: " + ex.Message;
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddCustomerAsync()
        {
            var dialogContent = App.Services.GetRequiredService<ThemKhachHangDialog>();

            var dialog = new ContentDialog
            {
                Title = "Thêm khách hàng mới",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy"
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    var ok = await SaveAsync(isEdit: false);

                    if (!ok) e.Cancel = true;
                }
            };

            var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);

            Customer = new CustomerModel();
            ErrorSummary = string.Empty;
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedCustomer == null) return;
            var result = System.Windows.MessageBox.Show("Bạn có chắc muốn xóa không?",
                                                        "Xác nhận xóa",
                                                        System.Windows.MessageBoxButton.YesNo,
                                                        System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var customerIdToDelete = SelectedCustomer.Id;
                var customerToDeleteFromDb = await db.Customers
                    .Where(p => customerIdToDelete == p.Id)
                    .ToListAsync();
                db.Customers.RemoveRange(customerToDeleteFromDb);
                await db.SaveChangesAsync();
                await LoadDataAsync();
            }
            catch (Exception)
            {
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand] 
        private async Task Edit()
        {
            if (SelectedCustomer == null) return;
            var dialogContent = App.Services.GetRequiredService<ThemKhachHangDialog>();

            var dialog = new ContentDialog
            {
                Title = "Sửa thông tin khách hàng",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy"
            };

            Customer = new CustomerModel
            {
                Id = SelectedCustomer.Id,
                Name = SelectedCustomer.Name,
                PhoneNumber = SelectedCustomer.PhoneNumber,
                Address = SelectedCustomer.Address
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    var ok = await SaveAsync(isEdit: true);

                    if (!ok) e.Cancel = true;
                }
            };

            var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            Customer = new CustomerModel();
            ErrorSummary = string.Empty;
        }
        partial void OnSearchTextChanged(string value) => CustomersView?.Refresh();

        private bool FilterCustomers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true; // Hiển thị tất cả nếu ô tìm kiếm trống

            if (obj is CustomerModel customer)
            {
                // Tìm kiếm theo Tên hoặc SĐT
                return (customer.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                       (customer.PhoneNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }
            return false;
        }
    }
}
