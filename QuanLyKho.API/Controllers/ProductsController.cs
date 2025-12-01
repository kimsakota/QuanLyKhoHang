using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.API.Data;
using QuanLyKho.API.Models;

namespace QuanLyKho.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ProductsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("list")]
    public async Task<ActionResult<IEnumerable<Product>>> List([FromBody] ProductFilterRequest filter)
    {
        filter ??= new ProductFilterRequest();

        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(keyword) || p.Code.Contains(keyword));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();
        return Ok(products);
    }

    [HttpPost("create")]
    public async Task<ActionResult<Product>> Create([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = new Product
        {
            Code = request.Code,
            Name = request.Name,
            Unit = request.Unit,
            Quantity = request.Quantity
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPost("by-id")]
    public async Task<ActionResult<Product>> GetById([FromBody] UpdateProductRequest request)
    {
        if (request is null)
        {
            return BadRequest("Request payload is required.");
        }

        var product = await _dbContext.Products.FindAsync(request.Id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("update")]
    public async Task<ActionResult<Product>> Update([FromBody] UpdateProductRequest request)
    {
        if (request is null)
        {
            return BadRequest("Request payload is required.");
        }

        var product = await _dbContext.Products.FindAsync(request.Id);
        if (product is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            product.Code = request.Code;
        }
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            product.Name = request.Name;
        }
        if (!string.IsNullOrWhiteSpace(request.Unit))
        {
            product.Unit = request.Unit;
        }
        if (request.Quantity.HasValue)
        {
            product.Quantity = request.Quantity.Value;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] UpdateProductRequest request)
    {
        if (request is null)
        {
            return BadRequest("Request payload is required.");
        }

        var product = await _dbContext.Products.FindAsync(request.Id);
        if (product is null)
        {
            return NotFound();
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}
