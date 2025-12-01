using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        [MaxLength(255)]
        public string? Name { get; set; }              // Tên công ty/nhà cung cấp

        [MaxLength(255)]
        public string? ContactPerson { get; set; }     // Người liên hệ

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }           // Địa chỉ

        [MaxLength(50)]
        public string? TaxCode { get; set; }           // Mã số thuế

        [MaxLength(255)]
        public string? BankName { get; set; }          // Tên ngân hàng

        [MaxLength(255)]
        public string? AccountName { get; set; }       // Chủ tài khoản

        [MaxLength(50)]
        public string? AccountNumber { get; set; }     // Số tài khoản

        [MaxLength(1000)]
        public string? Notes { get; set; }             // Ghi chú thêm

        public ICollection<Import> Imports { get; set; } = new List<Import>();
    }
}
