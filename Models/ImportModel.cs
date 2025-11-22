using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public partial class ImportModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ObservableProperty]
        private DateTime importDate;

        [ObservableProperty]
        private string? importedBy; 

        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public SupplierModel? Supplier { get; set; }

        public ICollection<ImportDetailModel> ImportDetails { get; set; } = new List<ImportDetailModel>();

    }
}
