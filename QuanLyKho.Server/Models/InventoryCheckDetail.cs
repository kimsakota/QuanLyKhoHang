using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("InventoryCheckDetails")]
    public class InventoryCheckDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InventoryCheckId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Tồn kho trên hệ thống tại thời điểm kiểm
        public int SystemQty { get; set; }

        // Số lượng thực tế kiểm được
        public int ActualQty { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(InventoryCheckId))]
        public InventoryCheck? InventoryCheck { get; set; }

        [NotMapped]
        public int Diff => ActualQty - SystemQty;
    }
}
