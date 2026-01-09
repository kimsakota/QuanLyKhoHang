using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            //string apiUrl = configuration["AppSettings:ApiBaseUrl"] ?? "https://LAPTOP-5S9SACTI:5263/api/";
            //string apiUrl = "http://LAPTOP-5S9SACTI:5127/api/"; // Tạm thời hardcode để tránh lỗi cấu hình
            //string apiUrl = "http://localhost:5127/api/";
            string apiUrl = "https://kimsakota.xyz/api/";

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
                var loginRequest = new { username = username, password = password };
                // Gọi đến endpoint Users/Login
                //var response = await _httpClient.PostAsJsonAsync("Users/Login", loginRequest);
                var response = await _httpClient.PostAsJsonAsync("Auth/login", loginRequest, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Login Error: {response.StatusCode}");
                    return null;
                }

                //Read AuthRespone from API
                var user = await response.Content.ReadFromJsonAsync<UserModel>(_jsonOptions);
                if(user == null || string.IsNullOrWhiteSpace(user.Token))
                {
                    System.Diagnostics.Debug.WriteLine("Login Error: AuthResponse null hoặc không có Token");
                    return null;
                }

                // Gắn Bearer Token vào header để các request sau tự kèm Authorization
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

                return user;
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

        ///<summary>
        /// Lấy danh sách bản ghi có giao dịch trong khoảng thời gian.
        /// Ví dụ: await GetWithTransactionAsync<CustomerModel>("Customers", fromDate, toDate);
        ///</summary>
        public async Task<List<T>> GetWithTransactionAsync<T>(string endpoint, DateTime from, DateTime to)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<T>>(
                    $"{endpoint}/WithTransaction?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", _jsonOptions);
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET WITH TRANSACTION Error [{endpoint}]: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Lấy báo cáo tổng hợp.
        /// Ví dụ await GetReportAsync<CustomerReportResponse>("Reports/Customers", fromDate, toDate);
        /// </summary>
        public async Task<T?> GetReportAsync<T>(string endpoint, DateTime from, DateTime to)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>(
                    $"{endpoint}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET REPORT Error [{endpoint}]: {ex.Message}");
                return default;
            }
        }

        ///<summary>
        /// Lấy giao dịch gần nhất của người giao dịch
        /// Ví dụ await GetLastTransaction<ImportDetail>("Imports", id)
        ///</summary>
        public async Task<T?> GetLastTransactionAsync<T>(string endpoint, int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>(
                    $"{endpoint}/LastTransaction?id={id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET LASTTRANSACTION Error [{endpoint}]: {ex.Message}");
                return default;
            }
        }

        ///<summary>
        /// Lấy giá giao dịch gần nhất của sản phẩm
        /// Ví dụ await GetLastTransaction<ImportDetail>("Imports", id)
        ///</summary>
        public async Task<decimal> GetLastImportPriceAsync(int productId)
        {
            try
            {
                // Gọi API: api/Products/{id}/LastImportPrice
                var result = await _httpClient.GetFromJsonAsync<decimal>($"Products/{productId}/LastImportPrice");
                return result;
            }
            catch
            {
                return 0; // Lỗi hoặc không có dữ liệu thì trả về 0
            }
        }

        /// <summary>
        /// Lấy số lượng bản ghi trong bảng.
        /// Ví dụ: await GetCountAsync("Products");
        /// </summary>
        public async Task<int> GetCountAsync(string endpoint)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<int>($"{endpoint}/Count", _jsonOptions);
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET COUNT Exception [{endpoint}]: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gọi GET generic theo URL tùy chỉnh, trả về 1 object T
        /// </summary>
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>(endpoint, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET Error [{endpoint}]: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Thêm mới một bản ghi.
        /// Ví dụ: await AddAsync("Products", newProduct);
        /// </summary>
        public async Task<TResponse?> AddAsync<TRequest, TResponse>(string endpoint, TRequest item)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, item, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    // Trả về đối tượng kiểu TResponse (ví dụ: ExportModel)
                    return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
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
        /// Route: api/{endpoint}/Update/{id}
        /// </summary>
        public async Task<bool> UpdateAsync<T>(string endpoint, int id, T item)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{endpoint}/Update/{id}", item, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"POST Exception [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa bản ghi.
        /// Route: api/{endpoint}/Delete/{id}
        /// </summary>
        public async Task<bool> DeleteAsync(string endpoint, int id)
        {
            var response = await _httpClient.PostAsync($"{endpoint}/Delete/{id}", null);

            // 2. Nếu thành công (200 OK, 204 No Content) -> Trả về true
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // 3. Nếu thất bại (VD: 400 Bad Request do ràng buộc dữ liệu)
            // ĐỌC NỘI DUNG LỖI TỪ SERVER GỬI VỀ
            var errorContent = await response.Content.ReadAsStringAsync();

            // Cố gắng lấy message đẹp nếu server trả json (Tùy chọn)
            string finalMessage = errorContent;
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(errorContent))
                {
                    if (doc.RootElement.TryGetProperty("message", out var msg))
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        finalMessage = msg.GetString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                }
            }
            catch { }

            // 4. NÉM LỖI RA NGOÀI (Thay vì return false)
            // Việc này giúp hàm DeleteSelected bắt được dòng chữ "Sản phẩm đã tồn tại..."
            throw new Exception(finalMessage);
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