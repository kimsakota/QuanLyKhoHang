using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public class InventoryCheckModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CheckDate { get; set; } // Ngày kiểm kê

        public string? CheckedBy { get; set; } // Người kiểm

        public string? Notes { get; set; } // Ghi chú

        public ICollection<InventoryCheckDetailModel> Details { get; set; } = new List<InventoryCheckDetailModel>();
    }
}
