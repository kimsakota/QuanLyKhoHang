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
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages.LienHe
{
    public partial class NhanVienViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService;
        private bool _isInitialized = false;

        public ObservableCollection<UserModel> Employees { get; private set; } = new();

        public ICollectionView EmployeeView { get; }

        [ObservableProperty]
        private UserModel? _selectedEmployee;
        [ObservableProperty]
        private UserModel _employee = new();
        [ObservableProperty]
        private bool isBusy = false;
        [ObservableProperty]
        private string _errorSummary = string.Empty;
        [ObservableProperty]
        private string _searchText = string.Empty;

        public NhanVienViewModel(IDbContextFactory<AppDbContext> dbContextFactory,
            IContentDialogService contentDialogService)
        {
            _dbContextFactory = dbContextFactory;
            _contentDialogService = contentDialogService;
            EmployeeView = CollectionViewSource.GetDefaultView(Employees);
            EmployeeView.Filter = FilterStaffs;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        public async Task OnNavigatedToAsync()
        {
            if(!_isInitialized)
            {
                await LoadDataAsync();
                _isInitialized = true;
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var employees = await dbContext.Users
                    .Where(u => u.Role == "Employee")
                    .ToListAsync();
                Employees.Clear();

                foreach (var employee in employees)
                    Employees.Add(employee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải nhân viên: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Add() => Notify();
        [RelayCommand]
        private void Edit() => Notify();
        [RelayCommand]
        private void Delete() => Notify(); 
        public void Notify() => MessageBox.Show("Đang phát triển!", "Lời nhắc", MessageBoxButton.OK, MessageBoxImage.Warning);

        [RelayCommand]
        private async Task RefreshList() => await LoadDataAsync();

        partial void OnSearchTextChanged(string value) => EmployeeView.Refresh();

        private bool FilterStaffs(object obj)
        {
            if(string.IsNullOrWhiteSpace(SearchText))
                return true;

            if (obj is UserModel staff) 
            {
                return staff.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                       staff.Username?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
            }
            return false;
        }
    }
}
