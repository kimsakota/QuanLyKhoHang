using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation tới các phiếu xuất
        public ICollection<Export> Exports { get; set; } = new List<Export>();
    }
}
