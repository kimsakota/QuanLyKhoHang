using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [MaxLength(100)]
        public string? Username { get; set; }

        // Mật khẩu đã được băm (BCrypt, v.v.)
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Employee";
    }
}
