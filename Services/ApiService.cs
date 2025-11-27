using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            // 1. Lấy URL API từ cấu hình
            string apiUrl = configuration["AppSettings:ApiBaseUrl"] ?? "https://LAPTOP-5S9SACTI:5263/api/";
            //string apiUrl = "http://LAPTOP-5S9SACTI:5263/api/"; // Tạm thời hardcode để tránh lỗi cấu hình
            //string apiUrl = "http://localhost:5263/api/";

            // Đảm bảo URL luôn có dấu / ở cuối
            if (!apiUrl.EndsWith("/")) apiUrl += "/";

            _httpClient.BaseAddress = new Uri(apiUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Timeout sau 30s

            // 2. Cấu hình xử lý JSON (Quan trọng)
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Không phân biệt hoa thường (id == Id)
                ReferenceHandler = ReferenceHandler.IgnoreCycles, // Bỏ qua lỗi vòng lặp (A chứa B, B chứa A)
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ==========================================
        // 1. AUTHENTICATION (Đăng nhập)
        // ==========================================
        public async Task<UserModel?> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new { Username = username, Password = password };
                // Gọi đến endpoint Users/Login
                var response = await _httpClient.PostAsJsonAsync("Users/Login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserModel>(_jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login Error: {ex.Message}");
                return null;
            }
        }

        // ==========================================
        // 2. GENERIC CRUD (Dùng chung cho mọi bảng)
        // ==========================================

        /// <summary>
        /// Lấy danh sách tất cả bản ghi.
        /// Ví dụ: await GetAllAsync<ProductModel>("Products");
        /// </summary>
        public async Task<List<T>> GetAllAsync<T>(string endpoint)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<T>>(endpoint, _jsonOptions);
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET ALL Error [{endpoint}]: {ex.Message}");
                // Trả về list rỗng thay vì null để tránh lỗi NullReference ở View
                return new List<T>();
            }
        }

        /// <summary>
        /// Lấy chi tiết 1 bản ghi theo ID.
        /// Ví dụ: await GetByIdAsync<ProductModel>("Products", 1);
        /// </summary>
        public async Task<T?> GetByIdAsync<T>(string endpoint, int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>($"{endpoint}/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET ID Error [{endpoint}]: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Thêm mới một bản ghi.
        /// Ví dụ: await AddAsync("Products", newProduct);
        /// </summary>
        public async Task<T?> AddAsync<T>(string endpoint, T item)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, item, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    // Trả về đối tượng vừa tạo (bao gồm ID mới sinh ra)
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }

                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"POST Error [{endpoint}]: {error}");
                return default;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"POST Exception [{endpoint}]: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Cập nhật bản ghi.
        /// Ví dụ: await UpdateAsync("Products", product.Id, product);
        /// </summary>
        public async Task<bool> UpdateAsync<T>(string endpoint, int id, T item)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{id}", item, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PUT Exception [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa bản ghi.
        /// Ví dụ: await DeleteAsync("Products", 5);
        /// </summary>
        public async Task<bool> DeleteAsync(string endpoint, int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DELETE Exception [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra mã tồn tại.
        /// Ví dụ: await CheckExistsAsync("Products", "productCode", "SP001");
        /// </summary>
        public async Task<bool> CheckExistsAsync(string endpoint, string fieldName, string value)
        {
            try
            {
                //var response = await _httpClient.GetAsync($"{endpoint}/CheckExists?code={value}");
                var response = await _httpClient.GetAsync($"{endpoint}/CheckExists?value={Uri.EscapeDataString(value)}");
                if (response.IsSuccessStatusCode)
                {
                    var exists = await response.Content.ReadFromJsonAsync<bool>();
                    return exists;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CHECK EXISTS Exception [{endpoint}]: {ex.Message}");
                return false;
            }
        }
    }
}