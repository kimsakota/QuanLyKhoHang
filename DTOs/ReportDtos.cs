using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.DTOs
{
    internal class ReportDtos
    {
    }

    // --- Các Class DTO Client ---
    public class InventoryReportResponse
    {
        public int TotalProductsCount { get; set; }
        public int TotalStockQuantity { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public List<ChartItemDto> CategoryValueChart { get; set; } = new();
        public List<ChartItemDto> TopValueProductChart { get; set; } = new();
    }

    public class LowStockProductDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int InitialQty { get; set; }
    }

    public class ChartItemDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
    // -----------------------------
}
