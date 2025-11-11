using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public partial class CustomerModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private string? phoneNumber;

        [ObservableProperty]
        private string? address;

        [ObservableProperty]
        private string? notes;

        public ICollection<ExportModel> Exports { get; set; } = new List<ExportModel>();

        public void ValidateAll() => base.ValidateAllProperties();
    }
}
