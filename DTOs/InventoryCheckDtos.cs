using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.DTOs
{
    internal class InventoryCheckDtos
    {
    }

    // --- DTOs cho Client ---
    public class CreateInventoryCheckRequest
    {
        public DateTime CheckDate { get; set; }
        public string? Notes { get; set; }
        public List<InventoryCheckDetailDto> Details { get; set; } = new();
    }

    public class InventoryCheckDetailDto
    {
        public int ProductId { get; set; }
        public int SystemQty { get; set; }
        public int ActualQty { get; set; }
    }
}
