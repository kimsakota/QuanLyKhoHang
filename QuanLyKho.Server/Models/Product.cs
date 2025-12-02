using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models 
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        // Đường dẫn ảnh (trên server / url)
        [MaxLength(500)]
        public string? ImagePath { get; set; } = "/images/default/logo-image.png";

        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        [MaxLength(50)]
        public string? ProductCode { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [MaxLength(255)]
        public string? ProductName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng ban đầu không hợp lệ.")]
        public int InitialQty { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá bán không hợp lệ.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }   // entity Category bên web

        // Navigation properties
        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
        public ICollection<ExportDetail> ExportDetails { get; set; } = new List<ExportDetail>();

        // Các property chỉ phục vụ UI (nếu cần) thì NotMapped
        [NotMapped]
        public bool IsSelected { get; set; }
    }
}
