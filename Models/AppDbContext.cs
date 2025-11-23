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

            // 1. BỎ QUA CÁC THUỘC TÍNH TÍNH TOÁN (QUAN TRỌNG ĐỂ SỬA LỖI "No backing field")
            modelBuilder.Entity<ExportDetailModel>().Ignore(e => e.TotalPrice);
            modelBuilder.Entity<ImportDetailModel>().Ignore(i => i.TotalPrice);
            modelBuilder.Entity<InventoryCheckDetailModel>().Ignore(c => c.Diff);

            // 2. Cấu hình các Index và Relationship
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => p.ProductCode)
                .IsUnique(false);

            modelBuilder.Entity<CategoryModel>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.Username)
                .IsUnique(true);

            modelBuilder.Entity<SupplierModel>()
                .HasIndex(s => s.TaxCode)
                .IsUnique();

            modelBuilder.Entity<SupplierModel>()
                .HasMany(s => s.Imports)
                .WithOne(i => i.Supplier)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CustomerModel>()
                .HasMany(c => c.Exports)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ImportModel>()
                .HasMany(i => i.ImportDetails)
                .WithOne(d => d.Import)
                .HasForeignKey(d => d.ImportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExportModel>()
                .HasMany(e => e.ExportDetails)
                .WithOne(d => d.Export)
                .HasForeignKey(d => d.ExportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductModel>()
                .HasMany(p => p.ImportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductModel>()
                .HasMany(p => p.ExportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryCheckModel>()
                .HasMany(c => c.Details)
                .WithOne(d => d.InventoryCheck)
                .HasForeignKey(d => d.InventoryCheckId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. XỬ LÝ KIỂU DECIMAL CHO SQLITE (Nếu dùng SQLite thì convert sang double)
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(decimal));

                    foreach (var property in properties)
                    {
                        modelBuilder.Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion<double>();
                    }
                }
            }
            // Nếu dùng SQL Server thì giữ nguyên cấu hình cũ (nếu muốn dùng song song)
            else
            {
                modelBuilder.Entity<ProductModel>().Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
                modelBuilder.Entity<ImportDetailModel>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
                modelBuilder.Entity<ExportDetailModel>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            }
        }
    }
}