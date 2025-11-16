using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public class UserModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; } 

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; } 

        [Required]
        [MaxLength(100)]
        public string? Username { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Employee";

        public void ValidateAll() => base.ValidateAllProperties();
    }
}
