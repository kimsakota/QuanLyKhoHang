using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace UiDesktopApp1.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        private const string ApiKey = "KhoHangSecretKey_2025"; // Trùng với server
        private const int TimeoutSeconds = 15;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://kimsakota.xyz.com/"), // sửa domain
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };

            // Auto gửi API KEY
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        // ============================
        //  RESPONSE MODEL
        // ============================
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        // ============================
        //  GET LIST
        // ============================
        public async Task<List<T>> GetAllAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/{endpoint}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ GET LIST ERROR [{endpoint}] Status: {response.StatusCode}");
                    return new List<T>();
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<T>>>(_jsonOptions);
                return apiResponse?.Data ?? new List<T>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ GET LIST EXCEPTION [{endpoint}]: {ex.Message}");
                return new List<T>();
            }
        }

        // ============================
        //  GET BY ID
        // ============================
        public async Task<T?> GetByIdAsync<T>(string endpoint, int id) where T : class
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/{endpoint}/{id}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                return apiResponse?.Data;
            }
            catch
            {
                return null;
            }
        }


        // ============================
        //  POST (CREATE)
        // ============================
        public async Task<bool> AddAsync<T>(string endpoint, T item)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/{endpoint}", item, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ADD EXCEPTION [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        // ============================
        //  PUT (UPDATE)
        // ============================
        public async Task<bool> UpdateAsync<T>(string endpoint, int id, T item)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/{endpoint}/{id}", item, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ UPDATE EXCEPTION [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        // ============================
        //  DELETE 1 ITEM
        // ============================
        public async Task<bool> DeleteAsync(string endpoint, int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/{endpoint}/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ DELETE EXCEPTION [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        // ============================
        //  DELETE MANY (List<int>)
        // ============================
        public async Task<bool> DeleteManyAsync(string endpoint, List<int> ids)
        {
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(_httpClient.BaseAddress + $"api/{endpoint}/DeleteMany"),
                    Content = JsonContent.Create(ids, options: _jsonOptions)
                };

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ DELETE MANY EXCEPTION [{endpoint}]: {ex.Message}");
                return false;
            }
        }

        // ============================
        //  LOGIN (DÙNG WRAPPER GIỐNG API)
        // ============================
        public async Task<T?> PostWrapperAsync<T>(string endpoint, object wrapper)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/{endpoint}", wrapper, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ LOGIN ERROR [{endpoint}] Status: {response.StatusCode}");
                    return default;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                return apiResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ LOGIN EXCEPTION [{endpoint}]: {ex.Message}");
                return default;
            }
        }
    }
}
