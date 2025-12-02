using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("InventoryChecks")]
    public class InventoryCheck
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CheckDate { get; set; }  // Ngày kiểm kê

        [MaxLength(255)]
        public string? CheckedBy { get; set; }   // Người kiểm

        [MaxLength(1000)]
        public string? Notes { get; set; }       // Ghi chú

        public ICollection<InventoryCheckDetail> Details { get; set; } = new List<InventoryCheckDetail>();
    }
}
