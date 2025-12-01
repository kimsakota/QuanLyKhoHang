using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("Exports")]
    public class Export
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime ExportDate { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public string? ExportedBy { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public ICollection<ExportDetail> ExportDetails { get; set; } = new List<ExportDetail>();
    }
}
