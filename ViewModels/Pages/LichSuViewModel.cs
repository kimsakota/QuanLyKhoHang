using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using UiDesktopApp1.Models;
using UiDesktopApp1.Views.UserControls.Dialog;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    // DTO cho danh sách tổng hợp
    public class HistoryItem
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Nhập kho", "Xuất kho", "Kiểm kê"
        public DateTime Date { get; set; }
        public string PartnerName { get; set; } = string.Empty; // Với kiểm kê thì là "Nội bộ" hoặc để trống
        public decimal? TotalAmount { get; set; } // Có thể null nếu là kiểm kê
        public string Creator { get; set; } = string.Empty;
    }

    // DTO cho chi tiết
    public class TransactionDetailDTO
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Để Binding ẩn hiện Grid tương ứng
        public string PartnerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Creator { get; set; } = string.Empty;

        // Dùng cho Nhập/Xuất
        public decimal? TotalAmount { get; set; }
        public List<DetailItem> Details { get; set; } = new();

        // Dùng cho Kiểm kê
        public List<InventoryDetailItem> InventoryDetails { get; set; } = new();
    }

    // Item chi tiết cho Nhập/Xuất
    public class DetailItem
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    // Item chi tiết cho Kiểm kê
    public class InventoryDetailItem
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int SystemQty { get; set; }
        public int ActualQty { get; set; }
        public int Diff => ActualQty - SystemQty; // Chênh lệch
    }

    public partial class LichSuViewModel : ObservableObject, INavigationAware
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IContentDialogService _contentDialogService;

        [ObservableProperty] private ObservableCollection<HistoryItem> _historyList = new();
        [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-30);
        [ObservableProperty] private DateTime _endDate = DateTime.Now;

        // 0=Tất cả, 1=Nhập, 2=Xuất, 3=Kiểm kê
        [ObservableProperty] private int _selectedTypeIndex = 0;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isBusy = false;

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

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var list = new List<HistoryItem>();

                // 1. Nhập kho
                if (SelectedTypeIndex == 0 || SelectedTypeIndex == 1)
                {
                    var imports = await db.Imports
                        .Include(i => i.Supplier)
                        .Include(i => i.ImportDetails)
                        .Where(i => i.ImportDate >= StartDate.Date && i.ImportDate <= EndDate.Date.AddDays(1).AddTicks(-1))
                        .ToListAsync();

                    list.AddRange(imports.Select(i => new HistoryItem
                    {
                        Id = i.Id,
                        Type = "Nhập kho",
                        TransactionCode = $"IMP-{i.Id:D5}",
                        Date = i.ImportDate,
                        PartnerName = i.Supplier?.Name ?? "N/A",
                        Creator = i.ImportedBy ?? "Admin",
                        TotalAmount = i.ImportDetails.Sum(d => d.Quantity * d.UnitPrice)
                    }));
                }

                // 2. Xuất kho
                if (SelectedTypeIndex == 0 || SelectedTypeIndex == 2)
                {
                    var exports = await db.Exports
                        .Include(e => e.Customer)
                        .Include(e => e.ExportDetails)
                        .Where(e => e.ExportDate >= StartDate.Date && e.ExportDate <= EndDate.Date.AddDays(1).AddTicks(-1))
                        .ToListAsync();

                    list.AddRange(exports.Select(e => new HistoryItem
                    {
                        Id = e.Id,
                        Type = "Xuất kho",
                        TransactionCode = $"EXP-{e.Id:D5}",
                        Date = e.ExportDate,
                        PartnerName = e.Customer?.Name ?? "Khách lẻ",
                        Creator = "Admin",
                        TotalAmount = e.ExportDetails.Sum(d => d.Quantity * d.UnitPrice)
                    }));
                }

                // 3. Kiểm kê kho
                if (SelectedTypeIndex == 0 || SelectedTypeIndex == 3)
                {
                    // Giả sử đã chạy migration tạo bảng InventoryChecks
                    // Nếu chưa chạy migration thì đoạn này sẽ lỗi runtime, hãy đảm bảo DB đã update
                    try
                    {
                        var checks = await db.InventoryChecks
                            .Where(c => c.CheckDate >= StartDate.Date && c.CheckDate <= EndDate.Date.AddDays(1).AddTicks(-1))
                            .ToListAsync();

                        list.AddRange(checks.Select(c => new HistoryItem
                        {
                            Id = c.Id,
                            Type = "Kiểm kê",
                            TransactionCode = $"CHK-{c.Id:D5}",
                            Date = c.CheckDate,
                            PartnerName = "---", // Kiểm kê nội bộ không có đối tác
                            Creator = c.CheckedBy ?? "Admin",
                            TotalAmount = null // Không có tổng tiền
                        }));
                    }
                    catch { /* Bỏ qua nếu bảng chưa tồn tại */ }
                }

                // Lọc SearchText
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var key = SearchText.ToLower();
                    list = list.Where(x => x.PartnerName.ToLower().Contains(key) ||
                                           x.TransactionCode.ToLower().Contains(key)).ToList();
                }

                // Sắp xếp
                list = list.OrderByDescending(x => x.Date).ToList();
                HistoryList = new ObservableCollection<HistoryItem>(list);
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

                if (item.Type == "Nhập kho")
                {
                    var details = await db.ImportDetails.Include(d => d.Product).Where(d => d.ImportId == item.Id).ToListAsync();
                    dto.Details = details.Select(d => new DetailItem { ProductCode = d.Product?.ProductCode, ProductName = d.Product?.ProductName, Quantity = d.Quantity, UnitPrice = d.UnitPrice }).ToList();
                }
                else if (item.Type == "Xuất kho")
                {
                    var details = await db.ExportDetails.Include(d => d.Product).Where(d => d.ExportId == item.Id).ToListAsync();
                    dto.Details = details.Select(d => new DetailItem { ProductCode = d.Product?.ProductCode, ProductName = d.Product?.ProductName, Quantity = d.Quantity, UnitPrice = d.UnitPrice }).ToList();
                }
                else // Kiểm kê
                {
                    var details = await db.InventoryCheckDetails.Include(d => d.Product).Where(d => d.InventoryCheckId == item.Id).ToListAsync();
                    dto.InventoryDetails = details.Select(d => new InventoryDetailItem
                    {
                        ProductCode = d.Product?.ProductCode,
                        ProductName = d.Product?.ProductName,
                        SystemQty = d.SystemQty,
                        ActualQty = d.ActualQty
                    }).ToList();
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
                System.Windows.MessageBox.Show($"Lỗi: {ex.Message}");
            }
        }
    }
}