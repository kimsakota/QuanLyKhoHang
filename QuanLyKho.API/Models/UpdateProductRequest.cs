using System.ComponentModel.DataAnnotations;

namespace QuanLyKho.API.Models;

public class UpdateProductRequest
{
    [Required]
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Unit { get; set; }
    public int? Quantity { get; set; }
}
