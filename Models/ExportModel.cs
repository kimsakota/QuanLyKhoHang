using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public partial class ExportModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ObservableProperty]
        private DateTime exportDate;

        [ObservableProperty]
        private string? exportedBy;

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public CustomerModel? Customer { get; set; }

        public ICollection<ExportDetailModel> ExportDetails { get; set; } = new List<ExportDetailModel>();
    }
}
