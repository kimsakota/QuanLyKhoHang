using QuanLyKho.Server.Models;

namespace QuanLyKho.Server.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal SalePrice { get; set; }
        public int InitialQty { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }

    public static class ProductMapping
    {
        public static ProductDto ToDto(this Product p) => new ProductDto
        {
            Id = p.Id,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            SalePrice = p.SalePrice,
            InitialQty = p.InitialQty,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name
        };
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
