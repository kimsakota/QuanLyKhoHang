using Microsoft.EntityFrameworkCore;
using QuanLyKho.API.Models;

namespace QuanLyKho.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
}
