using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Models;
using UiDesktopApp1.Views.UserControls.Dialog;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    // --- DTO Classes (Đã tối ưu để tránh warning) ---
    public class HistoryItem
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public decimal? TotalAmount { get; set; }
        public string Creator { get; set; } = string.Empty;
    }

    public class TransactionDetailDTO
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Creator { get; set; } = string.Empty;
        public decimal? TotalAmount { get; set; }

        // Khởi tạo list ngay để tránh null reference
        public List<DetailItem> Details { get; set; } = new();
        public List<InventoryDetailItem> InventoryDetails { get; set; } = new();
    }

    public class DetailItem
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class InventoryDetailItem
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int SystemQty { get; set; }
        public int ActualQty { get; set; }
        public int Diff => ActualQty - SystemQty;
    }

    // --- ViewModel Chính ---
    public partial class LichSuViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService;

        // Định nghĩa hằng số để tránh sai sót khi gõ chuỗi
        private const string TYPE_IMPORT = "Nhập kho";
        private const string TYPE_EXPORT = "Xuất kho";
        private const string TYPE_CHECK = "Kiểm kê";

        [ObservableProperty]
        private ObservableCollection<HistoryItem> _historyList = new();

        [ObservableProperty]
        private DateTime _startDate = DateTime.Now.AddDays(-30);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Now;

        // 0=Tất cả, 1=Nhập, 2=Xuất, 3=Kiểm kê
        [ObservableProperty]
        private int _selectedTypeIndex = 0;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        public LichSuViewModel(IDbContextFactory<AppDbContext> dbContextFactory, IContentDialogService contentDialogService)
        {
            _dbContextFactory = dbContextFactory;
            _contentDialogService = contentDialogService;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        // --- Tự động tải lại khi thay đổi bộ lọc (Optional) ---
        partial void OnStartDateChanged(DateTime value) => _ = LoadDataAsync();
        partial void OnEndDateChanged(DateTime value) => _ = LoadDataAsync();
        partial void OnSelectedTypeIndexChanged(int value) => _ = LoadDataAsync();
        // Với SearchText, thường ta dùng button hoặc chờ Enter để tránh lag, nên không auto load ở đây.

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var list = new List<HistoryItem>();

                // Thời gian cuối ngày: 23:59:59
                var endDateTime = EndDate.Date.AddDays(1).AddTicks(-1);

                // 1. Lấy dữ liệu NHẬP KHO
                if (SelectedTypeIndex == 0 || SelectedTypeIndex == 1)
                {
                    var imports = await db.Imports
                        .AsNoTracking() // Tăng tốc độ đọc
                        .Include(i => i.Supplier)
                        .Include(i => i.ImportDetails)
                        .Where(i => i.ImportDate >= StartDate.Date && i.ImportDate <= endDateTime)
                        .ToListAsync();

                    list.AddRange(imports.Select(i => new HistoryItem
                    {
                        Id = i.Id,
                        Type = TYPE_IMPORT,
                        TransactionCode = $"IMP-{i.Id:D5}",
                        Date = i.ImportDate,
                        PartnerName = i.Supplier?.Name ?? "N/A",
                        Creator = i.ImportedBy ?? "Unknown",
                        TotalAmount = i.ImportDetails.Sum(d => d.Quantity * d.UnitPrice)
                    }));
                }

                // 2. Lấy dữ liệu XUẤT KHO
                if (SelectedTypeIndex == 0 || SelectedTypeIndex == 2)
                {
                    var exports = await db.Exports
                        .AsNoTracking()
                        .Include(e => e.Customer)
                        .Include(e => e.ExportDetails)
                        .Where(e => e.ExportDate >= StartDate.Date && e.ExportDate <= endDateTime)
                        .ToListAsync();

                    list.AddRange(exports.Select(e => new HistoryItem
                    {
                        Id = e.Id,
                        Type = TYPE_EXPORT,
                        TransactionCode = $"EXP-{e.Id:D5}",
                        Date = e.ExportDate,
                        PartnerName = e.Customer?.Name ?? "Khách lẻ",
                        Creator = e.ExportedBy ?? "Unknown", 
                        TotalAmount = e.ExportDetails.Sum(d => d.Quantity * d.UnitPrice)
                    }));
                }

                // 3. Lấy dữ liệu KIỂM KÊ
                if (SelectedTypeIndex == 0 || SelectedTypeIndex == 3)
                {
                    var checks = await db.InventoryChecks
                        .AsNoTracking()
                        .Where(c => c.CheckDate >= StartDate.Date && c.CheckDate <= endDateTime)
                        .ToListAsync();

                    list.AddRange(checks.Select(c => new HistoryItem
                    {
                        Id = c.Id,
                        Type = TYPE_CHECK,
                        TransactionCode = $"CHK-{c.Id:D5}",
                        Date = c.CheckDate,
                        PartnerName = "Kiểm kê nội bộ",
                        Creator = c.CheckedBy ?? "Unknown",
                        TotalAmount = null
                    }));
                }

                // 4. Lọc theo từ khóa (Tìm kiếm an toàn Null)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var key = SearchText.Trim();
                    list = list.Where(x =>
                        (x.PartnerName != null && x.PartnerName.Contains(key, StringComparison.OrdinalIgnoreCase)) ||
                        (x.TransactionCode != null && x.TransactionCode.Contains(key, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 5. Sắp xếp & Cập nhật UI
                list = list.OrderByDescending(x => x.Date).ToList();

                HistoryList.Clear();
                foreach (var item in list)
                {
                    HistoryList.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lịch sử: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ViewDetails(HistoryItem? item)
        {
            if (item == null) return;

            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();

                var dto = new TransactionDetailDTO
                {
                    TransactionCode = item.TransactionCode,
                    Type = item.Type,
                    PartnerName = item.PartnerName,
                    Date = item.Date,
                    Creator = item.Creator,
                    TotalAmount = item.TotalAmount
                };

                switch (item.Type)
                {
                    case TYPE_IMPORT:
                        var importDetails = await db.ImportDetails
                            .AsNoTracking()
                            .Include(d => d.Product)
                            .Where(d => d.ImportId == item.Id)
                            .ToListAsync();

                        dto.Details = importDetails.Select(d => new DetailItem
                        {
                            ProductCode = d.Product?.ProductCode ?? "---",
                            ProductName = d.Product?.ProductName ?? "Sản phẩm đã xóa",
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice
                        }).ToList();
                        break;

                    case TYPE_EXPORT:
                        var exportDetails = await db.ExportDetails
                            .AsNoTracking()
                            .Include(d => d.Product)
                            .Where(d => d.ExportId == item.Id)
                            .ToListAsync();

                        dto.Details = exportDetails.Select(d => new DetailItem
                        {
                            ProductCode = d.Product?.ProductCode ?? "---",
                            ProductName = d.Product?.ProductName ?? "Sản phẩm đã xóa",
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice
                        }).ToList();
                        break;

                    case TYPE_CHECK:
                        var checkDetails = await db.InventoryCheckDetails
                            .AsNoTracking()
                            .Include(d => d.Product)
                            .Where(d => d.InventoryCheckId == item.Id)
                            .ToListAsync();

                        dto.InventoryDetails = checkDetails.Select(d => new InventoryDetailItem
                        {
                            ProductCode = d.Product?.ProductCode ?? "---",
                            ProductName = d.Product?.ProductName ?? "Sản phẩm đã xóa",
                            SystemQty = d.SystemQty,
                            ActualQty = d.ActualQty
                        }).ToList();
                        break;
                }

                var dialogControl = new ChiTietGiaoDichDialog { DataContext = dto };
                var dialog = new ContentDialog
                {
                    Title = $"Chi tiết phiếu {item.Type}",
                    Content = dialogControl,
                    CloseButtonText = "Đóng",
                    DefaultButton = ContentDialogButton.Close
                };

                await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Không thể tải chi tiết: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}