using System;
using System.Collections.Generic;

namespace UiDesktopApp1.DTOs
{
    // DTO dùng để gửi yêu cầu tạo phiếu nhập lên API
    public class CreateImportRequest
    {
        public int SupplierId { get; set; }
        public DateTime ImportDate { get; set; }
        public string? ImportedBy { get; set; } // Người thực hiện nhập kho
        public List<ImportItemDto> Details { get; set; } = new();
    }

    // DTO chi tiết sản phẩm nhập
    public class ImportItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}