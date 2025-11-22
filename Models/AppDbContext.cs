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
        public DbSet<SupplierModel> Suppliers => Set<SupplierModel>();
        public DbSet<CustomerModel> Customers => Set<CustomerModel>();
        public DbSet<ImportModel> Imports => Set<ImportModel>();
        public DbSet<ImportDetailModel> ImportDetails => Set<ImportDetailModel>();
        public DbSet<ExportModel> Exports => Set<ExportModel>();
        public DbSet<ExportDetailModel> ExportDetails => Set<ExportDetailModel>();
        public DbSet<InventoryCheckModel> InventoryChecks => Set<InventoryCheckModel>();
        public DbSet<InventoryCheckDetailModel> InventoryCheckDetails => Set<InventoryCheckDetailModel>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Product
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => p.ProductCode)
                .IsUnique(false); // Cho phép ProductCode không unique (như trong migration)

            modelBuilder.Entity<ProductModel>()
                .Property(p => p.SalePrice)
                .HasColumnType("decimal(18,2)");

            // Cấu hình Category <-> Product (1-Nhiều)
            modelBuilder.Entity<CategoryModel>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Nếu xóa Category, Product.CategoryId -> null

            // Cấu hình User
            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.Username)
                .IsUnique(true);

            // Cấu hình Supplier
            modelBuilder.Entity<SupplierModel>()
                .HasIndex(s => s.TaxCode)
                .IsUnique();

            // Cấu hình Supplier <-> Import (1-Nhiều)
            modelBuilder.Entity<SupplierModel>()
                .HasMany(s => s.Imports)
                .WithOne(i => i.Supplier)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.SetNull); // Nếu xóa Supplier, Import.SupplierId -> null

            // Cấu hình Customer <-> Export (1-Nhiều)
            modelBuilder.Entity<CustomerModel>()
                .HasMany(c => c.Exports)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull); // Nếu xóa Customer, Export.CustomerId -> null

            // Cấu hình Import <-> ImportDetail (1-Nhiều)
            modelBuilder.Entity<ImportModel>()
                .HasMany(i => i.ImportDetails)
                .WithOne(d => d.Import)
                .HasForeignKey(d => d.ImportId)
                .OnDelete(DeleteBehavior.Cascade); // Nếu xóa Import, xóa luôn ImportDetail

            // Cấu hình Export <-> ExportDetail (1-Nhiều)
            modelBuilder.Entity<ExportModel>()
                .HasMany(e => e.ExportDetails)
                .WithOne(d => d.Export)
                .HasForeignKey(d => d.ExportId)
                .OnDelete(DeleteBehavior.Cascade); // Nếu xóa Export, xóa luôn ExportDetail

            // Cấu hình Product <-> ImportDetail (1-Nhiều)
            modelBuilder.Entity<ProductModel>()
                .HasMany(p => p.ImportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Product nếu có ImportDetail

            // Cấu hình Product <-> ExportDetail (1-Nhiều)
            modelBuilder.Entity<ProductModel>()
                .HasMany(p => p.ExportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Product nếu có ExportDetail

            // Cấu hình giá tiền cho ImportDetail và ExportDetail
            modelBuilder.Entity<ImportDetailModel>()
                .Property(d => d.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ExportDetailModel>()
                .Property(d => d.UnitPrice)
                .HasColumnType("decimal(18,2)");
        }
    }
}