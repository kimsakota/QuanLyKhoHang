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
using UiDesktopApp1.Views.UserControls.LienHe;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Pages.LienHe
{
    public partial class NhaCungCapViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService;
        private bool _isInitialized = false;

        public ObservableCollection<SupplierModel> Suppliers { get; private set; } = new();

        public ICollectionView SuppliersView { get; }

        [ObservableProperty]
        private SupplierModel? _selectedSupplier;
        [ObservableProperty]
        private SupplierModel _supplier = new();
        [ObservableProperty]
        private bool isBusy = false;
        [ObservableProperty]
        private string _errorSummary = string.Empty;
        [ObservableProperty]
        private string _searchText = string.Empty;

        public NhaCungCapViewModel(IDbContextFactory<AppDbContext> dbContextFactory,
            IContentDialogService contentDialogService)
        {
            _dbContextFactory = dbContextFactory;
            _contentDialogService = contentDialogService;
            SuppliersView = CollectionViewSource.GetDefaultView(Suppliers);
            SuppliersView.Filter = FilterSuppliers;
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
            IsBusy = true;
            try
            {
                Suppliers.Clear();

                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var supplierList = await dbContext.Suppliers.AsNoTracking().ToListAsync();

                foreach (var supplier in supplierList)
                    Suppliers.Add(supplier);
            }
            finally { IsBusy = false; }
        }

        
        private async Task<bool> SaveAsync(bool isEdit)
        {
            
            Supplier.ValidateAll();
            if (Supplier.HasErrors)
            {
                var allErrors = Supplier.GetErrors()
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
                    db.Suppliers.Update(Supplier);
                else
                    db.Suppliers.Add(Supplier);
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
        private async Task AddAsync()
        {
            var dialogContent = App.Services.GetRequiredService<ThemSuaNhaCungCapDialog>();

            var dialog = new ContentDialog
            {
                Title = "Thêm khách hàng mới",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.Closing += async (s, e) => 
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    var ok = await SaveAsync(isEdit: false);
                    if (!ok)
                        e.Cancel = true;
                    else
                    {
                        Suppliers.Add(Supplier);
                        //Suppliers.Insert(0, Supplier);

                        SearchText = string.Empty;
                        SelectedSupplier = Supplier;
                    }
                }
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);

            Supplier = new SupplierModel();
            ErrorSummary = string.Empty;
        }

        [RelayCommand]
        private async Task EditAsync()
        {
            if (SelectedSupplier == null) return;
            var dialogContent = App.Services.GetRequiredService<ThemSuaNhaCungCapDialog>();

            Supplier = new SupplierModel
            {
                Id = SelectedSupplier.Id,
                Name = SelectedSupplier.Name,
                ContactPerson = SelectedSupplier.ContactPerson,
                PhoneNumber = SelectedSupplier.PhoneNumber,
                Email = SelectedSupplier.Email,
                Address = SelectedSupplier.Address,
                TaxCode = SelectedSupplier.TaxCode,
                BankName = SelectedSupplier.BankName,
                AccountName = SelectedSupplier.AccountName,
                AccountNumber = SelectedSupplier.AccountNumber,
                Notes = SelectedSupplier.Notes
            };

            var dialog = new ContentDialog
            {
                Title = "Sửa thông tin khách hàng",
                Content = dialogContent,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary
            };

            
            dialog.Closing += async (s, e) =>
            {
                if (e.Result == ContentDialogResult.Primary)
                {
                    var ok = await SaveAsync(isEdit: true);
                    if (!ok)
                        e.Cancel = true;
                    
                }
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);

            Supplier = new SupplierModel();
            ErrorSummary = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedSupplier == null) return;
            var result = System.Windows.MessageBox.Show("Bạn có chắc muốn xóa không?",
                                                        "Xác nhận xóa",
                                                        System.Windows.MessageBoxButton.YesNo,
                                                        System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var supplierToDelete = SelectedSupplier;

                await using var db = await _dbContextFactory.CreateDbContextAsync();

                db.Suppliers.Attach(supplierToDelete);
                db.Suppliers.Remove(supplierToDelete);

                await db.SaveChangesAsync();

                Suppliers.Remove(supplierToDelete);

                SelectedSupplier = null;
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

        
        partial void OnSearchTextChanged(string value) => SuppliersView?.Refresh();

        private bool FilterSuppliers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true; // Hiển thị tất cả nếu ô tìm kiếm trống

            if (obj is SupplierModel supplier)
            {
                // Tìm kiếm theo Tên hoặc SĐT
                return (supplier.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                       (supplier.PhoneNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }
            return false;
        }
    }
}
