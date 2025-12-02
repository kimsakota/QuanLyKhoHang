using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("ApiKeys")]
    public class ApiKey
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Key { get; set; } = null!;   // ví dụ: KhoHangSecretKey_2025

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public ICollection<ApiPermission> Permissions { get; set; } = new List<ApiPermission>();
    }
}
