using Azure.Core;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
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

        public ObservableCollection<CustomerModel> Customers { get; private set; } = new();

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

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var customerList = await dbContext.Customers.AsNoTracking().ToListAsync();

                foreach (var customer in customerList)
                    Customers.Add(customer);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task AddAsync() => await ShowCustomerDialogAsync(null);

        [RelayCommand]
        private async Task EditAsync() => await ShowCustomerDialogAsync(SelectedCustomer);

        private async Task ShowCustomerDialogAsync(CustomerModel? customer)
        {
            if(customer == null && SelectedCustomer != null)
                SelectedCustomer = null;

            Customer = customer != null ? new CustomerModel
            {
                Id = customer.Id,
                Name = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                Address = customer.Address,
                Notes = customer.Notes,
            } : new CustomerModel();

            ErrorSummary = string.Empty;
            var dialogContent = App.Services.GetRequiredService<ThemSuaKhachHangDialog>();

            var dialog = new ContentDialog
            {
                Title = customer == null ? "Thêm khách hàng mới" : "Sửa thông tin khách hàng",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    bool success = await HandleSaveToDbAsync();
                    if (!success) e.Cancel = true;
                }
            };
            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }

        private async Task<bool> HandleSaveToDbAsync()
        {
            Customer.ValidateAll();

            if(Customer.HasErrors)
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
                bool isEdit = Customer.Id != 0;
                if (isEdit)
                {
                    db.Customers.Update(Customer);
                    await db.SaveChangesAsync();

                    var index = Customers.IndexOf(SelectedCustomer!);
                    if(index >= 0)
                    {
                        Customers[index] = Customer;
                        SelectedCustomer = Customers[index];
                    }
                }
                else
                {
                    db.Customers.Add(Customer);
                    await db.SaveChangesAsync();
                    Customers.Add(Customer);
                    SearchText = string.Empty;
                    SelectedCustomer = Customers[Customers.Count - 1];
                }

                return true;
            }
            catch (Exception ex)
            {
                ErrorSummary = "Lỗi khi lưu: " + ex.Message;
                return false;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task DeleteAsync()
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
                
                await db.Customers.Where(c => c.Id == SelectedCustomer.Id)
                                  .ExecuteDeleteAsync();
                Customers.Remove(SelectedCustomer);
                SelectedCustomer = null;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Đã có lỗi xảy ra khi xóa khách hàng. Vui lòng thử lại.",
                    "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshList() => await LoadDataAsync();

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

        public async Task<CustomerModel?> AddFromExternalAsync()
        {
            await ShowCustomerDialogAsync(null);
            return SelectedCustomer;
        }
    }
}
