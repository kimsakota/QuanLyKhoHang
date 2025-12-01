using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Server.DTOs;
using QuanLyKho.Web.Services;

namespace QuanLyKho.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsApiController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsApiController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            var list = products.Select(p => p.ToDto()).ToList();

            return Ok(new ApiResponse<List<ProductDto>>
            {
                Success = true,
                Data = list
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy sản phẩm" });

            return Ok(new ApiResponse<ProductDto>
            {
                Success = true,
                Data = product.ToDto()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Dữ liệu không hợp lệ" });

            var product = new Models.Product
            {
                ProductCode = dto.ProductCode,
                ProductName = dto.ProductName,
                SalePrice = dto.SalePrice,
                InitialQty = dto.InitialQty,
                CategoryId = dto.CategoryId
            };

            await _productService.CreateAsync(product);
            return Ok(new ApiResponse<ProductDto> { Success = true, Data = product.ToDto() });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            var existing = await _productService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy sản phẩm" });

            existing.ProductCode = dto.ProductCode;
            existing.ProductName = dto.ProductName;
            existing.SalePrice = dto.SalePrice;
            existing.InitialQty = dto.InitialQty;
            existing.CategoryId = dto.CategoryId;

            await _productService.UpdateAsync(existing);
            return Ok(new ApiResponse<ProductDto> { Success = true, Data = existing.ToDto() });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _productService.DeleteAsync(id);
            if (!ok)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy sản phẩm" });

            return Ok(new ApiResponse<object> { Success = true, Message = "Đã xoá sản phẩm" });
        }
    }
}
