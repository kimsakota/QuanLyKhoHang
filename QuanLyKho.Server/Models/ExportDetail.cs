using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("ExportDetails")]
    public class ExportDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ExportId { get; set; }

        // Số lượng xuất – LƯU VÀO DB (không NotMapped nữa)
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }

        // Đơn giá – LƯU VÀO DB
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Đơn giá không hợp lệ.")]
        public decimal UnitPrice { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(ExportId))]
        public Export? Export { get; set; }

        // Không lưu DB – chỉ dùng để hiển thị, tính toán
        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
