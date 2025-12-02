using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("ImportDetails")]
    public class ImportDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ImportId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Đơn giá không hợp lệ.")]
        public decimal UnitPrice { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(ImportId))]
        public Import? Import { get; set; }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
