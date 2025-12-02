using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("ApiLogs")]
    public class ApiLog
    {
        [Key]
        public long Id { get; set; }

        [MaxLength(200)]
        public string? ApiKey { get; set; }

        [MaxLength(200)]
        public string? Username { get; set; }

        [MaxLength(200)]
        public string? Endpoint { get; set; }

        [MaxLength(10)]
        public string? Method { get; set; }

        public int StatusCode { get; set; }

        public DateTime CalledAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? ClientIp { get; set; }
    }
}
