using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public partial class ImportDetailModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ImportId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private decimal unitPrice;

        [ForeignKey(nameof(ProductId))]
        public ProductModel? Product { get; set; }

        [ForeignKey(nameof(ImportId))]
        public ImportModel? Import { get; set; }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;

        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalPrice));
        partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));
    }
}
