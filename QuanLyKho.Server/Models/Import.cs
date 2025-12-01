using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKho.Server.Models
{
    [Table("Imports")]
    public class Import
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime ImportDate { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public string? ImportedBy { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
    }
}
