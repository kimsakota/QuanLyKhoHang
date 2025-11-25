using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using UiDesktopApp1.Models; // Model ở Client giữ nguyên

namespace UiDesktopApp1.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            // Thay IP này bằng IP máy tính của bạn (xem bằng ipconfig)
            string apiUrl = configuration["AppSettings:ApiBaseUrl"];
            if (string.IsNullOrEmpty(apiUrl))
            {
                apiUrl = "http://LAPTOP-5S9SACTI:5263/";
            }
            _httpClient.BaseAddress = new Uri(apiUrl);
            //_httpClient.BaseAddress = new Uri("https://unsilvered-unprocessed-heath.ngrok-free.dev/");
        }

        // Ví dụ hàm lấy danh sách sản phẩm
        public async Task<List<ProductModel>?> GetAllProductsAsync()
        {
            try
            {
                // Gọi vào đường dẫn của Controller vừa tạo ở Bước 7 phần trên
                return await _httpClient.GetFromJsonAsync<List<ProductModel>>("api/ProductModels");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối Server: " + ex.Message);
                return new List<ProductModel>();
            }
        }

        public async Task<List<CategoryModel>?> GetAllCategoriesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<CategoryModel>>("api/CategoryModels");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối Server: " + ex.Message);
                return new List<CategoryModel>();
            }
        }
    }
}