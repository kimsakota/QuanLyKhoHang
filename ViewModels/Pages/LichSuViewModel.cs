using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiDesktopApp1.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.Services;
using UiDesktopApp1.Views.UserControls.Dialog;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class LichSuViewModel : ObservableObject, INavigationAware
    {
        private readonly ApiService _apiService;
        private readonly IContentDialogService _contentDialogService;

        [ObservableProperty]
        private ObservableCollection<HistoryItemDto> _historyList = new();

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

        public LichSuViewModel(ApiService apiService, IContentDialogService contentDialogService)
        {
            _apiService = apiService;
            _contentDialogService = contentDialogService;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        // Auto load khi thay đổi bộ lọc
        partial void OnStartDateChanged(DateTime value) => _ = LoadDataAsync();
        partial void OnEndDateChanged(DateTime value) => _ = LoadDataAsync();
        partial void OnSelectedTypeIndexChanged(int value) => _ = LoadDataAsync();

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                HistoryList.Clear();

                // Tạo query string
                string query = $"History?fromDate={StartDate:yyyy-MM-dd}&toDate={EndDate:yyyy-MM-dd}&type={SelectedTypeIndex}";
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query += $"&search={Uri.EscapeDataString(SearchText)}";
                }

                // Gọi API
                var list = await _apiService.GetAllAsync<HistoryItemDto>(query);

                if (list != null)
                {
                    foreach (var item in list)
                    {
                        HistoryList.Add(item);
                    }
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
        private async Task ViewDetails(HistoryItemDto? item)
        {
            if (item == null) return;

            try
            {
                // Gọi API lấy chi tiết
                // Endpoint: api/History/Details?id=...&type=...
                string query = $"History/Details?id={item.Id}&type={Uri.EscapeDataString(item.Type)}";

                // Vì GetByIdAsync của bạn trả về T, ta có thể dùng nó hoặc tạo hàm GetDetailAsync riêng.
                // Ở đây tôi dùng HttpClient trực tiếp thông qua helper nếu GetByIdAsync chỉ nhận endpoint/{id}
                // Tuy nhiên, để tận dụng ApiService, ta có thể dùng GetReportAsync (vì nó trả về 1 object T)
                // Hoặc bạn có thể dùng GetByIdAsync nếu bạn sửa Route API thành History/{id}?type=... 

                // Ở đây tôi dùng GetFromJsonAsync thông qua ApiService (bạn có thể cần public _httpClient hoặc thêm hàm generic GetAsync)
                // Giả sử dùng tạm GetReportAsync vì nó trả về 1 object T
                // var detailDto = await _apiService.GetReportAsync<TransactionDetailDto>("History/Details", StartDate, EndDate); // Sai param

                // Tốt nhất: Thêm hàm GetSingleAsync vào ApiService.cs:
                // public async Task<T?> GetSingleAsync<T>(string endpoint) { ... }

                // Giả sử bạn đã thêm hàm GetSingleAsync hoặc dùng GetAllAsync rồi lấy phần tử đầu (không tối ưu)
                // Cách workaround với hàm hiện tại trong ApiService của bạn:
                // Bạn có thể dùng GetByIdAsync với endpoint đã format sẵn query string nếu hàm đó không tự append /{id}

                // Cách đúng nhất với ApiService hiện tại của bạn là dùng GetReportAsync nhưng sửa lại tham số, 
                // hoặc tốt hơn là thêm method này vào ApiService.cs:
                /*
                public async Task<T?> GetByQueryAsync<T>(string endpoint)
                {
                    try { return await _httpClient.GetFromJsonAsync<T>(endpoint, _jsonOptions); }
                    catch { return default; }
                }
                */

                // Giả sử bạn đã thêm hàm GetByQueryAsync vào ApiService
                // var detailDto = await _apiService.GetByQueryAsync<TransactionDetailDto>(query); 

                // Nếu chưa sửa ApiService, ta dùng tạm GetByIdAsync với 1 chút trick nếu ApiService nối chuỗi:
                // Nếu ApiService là: return await _httpClient.GetFromJsonAsync<T>($"{endpoint}/{id}", ...);
                // Thì không trick được.

                // ==> Giải pháp: Bạn hãy dùng HttpClient hoặc thêm hàm mới vào ApiService.
                // Ở đây tôi giả định bạn sẽ thêm hàm `GetAsync<T>(string url)` vào ApiService.

                // TẠM THỜI: Tôi sẽ dùng GetAllAsync (trả về List) cho 1 API trả về Object => Sẽ lỗi JSON.
                // BẠN CẦN THÊM HÀM NÀY VÀO APISERVICE:
                /* public async Task<T?> GetAsync<T>(string url) 
                   {
                        try { return await _httpClient.GetFromJsonAsync<T>(url, _jsonOptions); }
                        catch { return default; }
                   }
                */

                // Sau đó gọi:
                var detailDto = await _apiService.GetAsync<TransactionDetailDto>(query);

                if (detailDto == null) return;

                // Hiển thị Dialog
                var dialogControl = new ChiTietGiaoDichDialog { DataContext = detailDto };
#pragma warning disable CA1416 // Validate platform compatibility
                var dialog = new ContentDialog
                {
                    Title = $"Chi tiết phiếu {item.Type}",
                    Content = dialogControl,
                    CloseButtonText = "Đóng",
                    DefaultButton = ContentDialogButton.Close
                };
#pragma warning restore CA1416 // Validate platform compatibility

                await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải chi tiết: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}