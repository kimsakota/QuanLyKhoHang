using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("ApiPermissions")]
    public class ApiPermission
    {
        [Key]
        public int Id { get; set; }

        public int ApiKeyId { get; set; }

        [ForeignKey(nameof(ApiKeyId))]
        public ApiKey? ApiKey { get; set; }

        [Required, MaxLength(200)]
        public string Endpoint { get; set; } = "";
        // ví dụ: "Products", "Imports", "Exports", "Reports"

        [MaxLength(20)]
        public string Method { get; set; } = "GET";
        // GET / POST / PUT / DELETE hoặc "*" cho tất cả
    }
}
