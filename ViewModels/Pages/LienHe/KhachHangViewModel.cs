using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UiDesktopApp1.Models;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.LienHe
{
    public partial class KhachHangViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private bool _isInitialized = false;

        public ObservableCollection<CustomerModel> Customers { get; } = new();

        public ICollectionView CustomersView { get; }

        [ObservableProperty]
        private CustomerModel? _selectedCustomer;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public KhachHangViewModel(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

            CustomersView = CollectionViewSource.GetDefaultView(Customers);
            CustomersView.Filter = FilterCustomers;
        }

        public async Task OnNavigatedToAsync()
        {
            if(!_isInitialized)
            {
                await LoadDataAsync();
                _isInitialized = true;
            }
        }

        public Task OnNavigatedFromAsync()
        {
            throw new NotImplementedException();
        }

        private async Task LoadDataAsync()
        {
            Customers.Clear();

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var customerList = await dbContext.Customers.AsNoTracking().ToListAsync();

            foreach (var customer in customerList)
                Customers.Add(customer);

        }

        //public void OnSearchTextChanged(string value)
        //{
        //    CustomersView.Refresh();
        //}

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
