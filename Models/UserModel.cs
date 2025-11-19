using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public partial class UserModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        [MaxLength(100)]
        [ObservableProperty]
        private string? _fullName;

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [ObservableProperty]
        private string? _phoneNumber;

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [ObservableProperty] 
        private string? _email;

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [MaxLength(100)]
        [ObservableProperty] 
        private string? _username;

        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        [ObservableProperty] 
        private string _role = "Employee";

        public void ValidateAll() => base.ValidateAllProperties();
    }
}
