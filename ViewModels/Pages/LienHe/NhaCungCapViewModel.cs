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
using MessageBox = System.Windows.MessageBox;

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
            //return throw new NotImplementedException();
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

        [RelayCommand]
        private async Task AddAsync() => await ShowSupplierDialogAsync(null);

        [RelayCommand]
        private async Task EditAsync() => await ShowSupplierDialogAsync(SelectedSupplier);

        private async Task ShowSupplierDialogAsync(SupplierModel? supplier)
        {
            if(supplier == null && SelectedSupplier != null)
                SelectedSupplier = null;
            
            Supplier = supplier != null ? new SupplierModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                PhoneNumber = supplier.PhoneNumber,
                Email = supplier.Email,
                Address = supplier.Address,
                TaxCode = supplier.TaxCode,
                BankName = supplier.BankName,
                AccountName = supplier.AccountName,
                AccountNumber = supplier.AccountNumber,
                Notes = supplier.Notes
            } : new SupplierModel();

            var dialogContent = App.Services.GetRequiredService<ThemSuaNhaCungCapDialog>();
            ErrorSummary = string.Empty;

            var dialog = new ContentDialog
            {
                Title = supplier == null ? "Thêm nhà cung cấp mới" : "Sửa thông tin nhà cung cấp",
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
                bool isEdit = Supplier.Id != 0;
                if (isEdit)
                    db.Suppliers.Update(Supplier);
                else
                    db.Suppliers.Add(Supplier);
                await db.SaveChangesAsync();
                if (isEdit)
                {
                    var index = Suppliers.IndexOf(SelectedSupplier!);
                    Suppliers[index] = Supplier;
                    SelectedSupplier = Suppliers[index];
                }
                else
                {
                    Suppliers.Add(Supplier);
                    SearchText = string.Empty;
                    SelectedSupplier = Suppliers[Suppliers.Count - 1];
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
            if (SelectedSupplier == null) return;
            var result = MessageBox.Show("Bạn có chắc muốn xóa không?",
                                                        "Xác nhận xóa",
                                                        System.Windows.MessageBoxButton.YesNo,
                                                        MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                
                await db.Suppliers.Where(s => s.Id == SelectedSupplier.Id).ExecuteDeleteAsync();
                Suppliers.Remove(SelectedSupplier);
                SelectedSupplier = null;
            }
            catch (Exception)
            {
                MessageBox.Show("Đã xảy ra lỗi khi xóa nhà cung cấp. Dữ liệu sẽ được tải lại.",
                                "Lỗi",
                                System.Windows.MessageBoxButton.OK,
                                MessageBoxImage.Error);
                await LoadDataAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshList() => await LoadDataAsync();
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

        public async Task<SupplierModel?> AddFromExternalAsync()
        {
            await ShowSupplierDialogAsync(null);
            return Supplier;
        }
    }
}
