using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UiDesktopApp1.DTOs;
using UiDesktopApp1.Models;
using UiDesktopApp1.Services;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class KiemKeKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly ApiService _apiService;
        private readonly CurrentUserService _currentUserService;
        private bool _isInitialized = false;

        [ObservableProperty] private ObservableCollection<ProductModel> _products = new();
        [ObservableProperty] private ObservableCollection<ProductModel> _suggestedProducts = new();
        [ObservableProperty] private string _productSearchText = string.Empty;
        [ObservableProperty] private ProductModel? _selectedProduct;

        [ObservableProperty] private DateTime _checkDate = DateTime.Now;
        [ObservableProperty] private int _systemQty = 0;
        [ObservableProperty] private int _actualQty = 0;
        [ObservableProperty] private string _errorMessage = string.Empty;

        [ObservableProperty] private ObservableCollection<InventoryCheckDetailModel> _checkDetails = new();

        public KiemKeKhoViewModel(ApiService apiService, CurrentUserService currentUserService)
        {
            _apiService = apiService;
            _currentUserService = currentUserService;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                RefreshForm();
                await LoadDataAsync();
                _isInitialized = true;
            }
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private async Task LoadDataAsync()
        {
            try
            {
                Products.Clear();
                // Gọi API lấy danh sách sản phẩm
                var list = await _apiService.GetAllAsync<ProductModel>("Products");
                foreach (var p in list) Products.Add(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách sản phẩm: " + ex.Message);
            }
        }

        partial void OnProductSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SuggestedProducts.Clear();
                SelectedProduct = null;
                return;
            }

            var keyword = value.Trim();

            // Lọc gợi ý (Client-side filtering cho nhanh, vì đã load hết Products về)
            SuggestedProducts.Clear();
            var results = Products.Where(p =>
                (p.ProductName != null && p.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (p.ProductCode != null && p.ProductCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .Take(20);

            foreach (var item in results) SuggestedProducts.Add(item);

            // Tự động chọn nếu khớp chính xác
            var match = Products.FirstOrDefault(p =>
                string.Equals(p.ProductName, keyword, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.ProductCode, keyword, StringComparison.OrdinalIgnoreCase));

            if (match != null) SelectedProduct = match;
            else
            {
                if (SelectedProduct != null) SelectedProduct = null;
                SystemQty = 0;
                ActualQty = 0;
            }
        }

        // Khi chọn sản phẩm, gọi API lấy tồn kho mới nhất
        partial void OnSelectedProductChanged(ProductModel? value)
        {
            if (value == null)
            {
                SystemQty = 0;
                ActualQty = 0;
                return;
            }

            if (!string.Equals(ProductSearchText, value.ProductName, StringComparison.OrdinalIgnoreCase))
                ProductSearchText = value.ProductName ?? string.Empty;

            Task.Run(async () =>
            {
                try
                {
                    // Gọi API lấy chi tiết sản phẩm để có tồn kho chính xác nhất từ Server
                    var p = await _apiService.GetByIdAsync<ProductModel>("Products", value.Id);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SystemQty = p?.InitialQty ?? 0;
                        ActualQty = SystemQty; // Mặc định thực tế = hệ thống
                    });
                }
                catch { }
            });
        }

        [RelayCommand]
        private void AddToList()
        {
            ErrorMessage = string.Empty;
            if (SelectedProduct == null) { ErrorMessage = "Vui lòng chọn sản phẩm."; return; }
            if (ActualQty < 0) { ErrorMessage = "Số lượng thực tế không được âm."; return; }

            var existing = CheckDetails.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
            if (existing != null)
            {
                existing.ActualQty = ActualQty;
                existing.SystemQty = SystemQty; // Cập nhật lại tồn hệ thống

                // Refresh UI
                int index = CheckDetails.IndexOf(existing);
                CheckDetails.RemoveAt(index);
                CheckDetails.Insert(index, existing);
            }
            else
            {
                CheckDetails.Add(new InventoryCheckDetailModel
                {
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct,
                    SystemQty = SystemQty,
                    ActualQty = ActualQty
                });
            }

            // Reset input
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            SystemQty = 0;
            ActualQty = 0;
        }

        [RelayCommand]
        private void RemoveItem(InventoryCheckDetailModel item)
        {
            if (CheckDetails.Contains(item)) CheckDetails.Remove(item);
        }

        [RelayCommand]
        private async Task SaveCheckAsync()
        {
            if (CheckDetails.Count == 0)
            {
                MessageBox.Show("Danh sách đang trống. Vui lòng thêm sản phẩm.", "Chưa có hàng hóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Lưu phiếu kiểm kê sẽ CẬP NHẬT TỒN KHO của {CheckDetails.Count} sản phẩm.\nBạn có chắc chắn không?",
                "Xác nhận cân bằng kho", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // 1. Map dữ liệu sang DTO
                var request = new CreateInventoryCheckRequest
                {
                    CheckDate = DateTime.Now,
                    Notes = "Kiểm kê kho định kỳ",
                    Details = CheckDetails.Select(d => new InventoryCheckDetailDto
                    {
                        ProductId = d.ProductId,
                        SystemQty = d.SystemQty,
                        ActualQty = d.ActualQty
                    }).ToList()
                };

                // 2. Gọi API
                // Endpoint: api/InventoryChecks
                // Trả về object hoặc mô hình cụ thể (ở đây dùng object vì chỉ cần check null)
                var result = await _apiService.AddAsync<CreateInventoryCheckRequest, object>("InventoryChecks", request);

                if (result != null)
                {
                    MessageBox.Show("Đã lưu và cân bằng kho thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshForm();
                    await LoadDataAsync(); // Tải lại danh sách sản phẩm để cập nhật số tồn mới
                }
                else
                {
                    MessageBox.Show("Lỗi khi lưu phiếu kiểm kê. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshForm()
        {
            CheckDetails.Clear();
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            ErrorMessage = string.Empty;
            SystemQty = 0;
            ActualQty = 0;
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            var ask = MessageBox.Show("Làm mới dữ liệu sẽ xóa toàn bộ thông tin đang nhập. Tiếp tục?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ask != MessageBoxResult.Yes) return;
            RefreshForm();
            await LoadDataAsync();
        }
    }

}