using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<ProductModel> Products => Set<ProductModel>();
        public DbSet<CategoryModel> Categories => Set<CategoryModel>();
        public DbSet<UserModel> Users => Set<UserModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => p.ProductCode)
                .IsUnique(false);

            modelBuilder.Entity<ProductModel>()
                .HasOne(p => p.Category)
                .WithMany(p => p.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); 

            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.Username)
                .IsUnique(true); // Đảm bảo Tên đăng nhập là duy nhất

            base.OnModelCreating(modelBuilder);
        }
    }
}