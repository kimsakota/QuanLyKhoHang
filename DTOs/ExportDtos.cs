using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.DTOs
{
    internal class ExportDtos
    {
    }

    public class CreateExportRequest
    {
        public int CustomerId { get; set; }
        public string? NewCustomerName { get; set; }
        public string? NewCustomerPhone { get; set; }
        public string? NewCustomerAddress { get; set; }
        public List<ExportItemDto> Details { get; set; } = new();
    }

    public class ExportItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
