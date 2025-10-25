using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.Models
{
    public partial class CategoryModel : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [ObservableProperty]
        private string? name;

        public ICollection<ProductModel> Products { get; set; } = new List<ProductModel>();
    }
}
