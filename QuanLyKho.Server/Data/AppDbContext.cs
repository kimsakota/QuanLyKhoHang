using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace QuanLyKho.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Import> Imports => Set<Import>();
        public DbSet<ImportDetail> ImportDetails => Set<ImportDetail>();
        public DbSet<Export> Exports => Set<Export>();
        public DbSet<ExportDetail> ExportDetails => Set<ExportDetail>();
        public DbSet<InventoryCheck> InventoryChecks => Set<InventoryCheck>();
        public DbSet<InventoryCheckDetail> InventoryCheckDetails => Set<InventoryCheckDetail>();

        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<ApiPermission> ApiPermissions => Set<ApiPermission>();
        public DbSet<ApiLog> ApiLogs => Set<ApiLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Bỏ qua các thuộc tính tính toán (đã [NotMapped] nhưng Ignore thêm vẫn OK)
            modelBuilder.Entity<ExportDetail>().Ignore(e => e.TotalPrice);
            modelBuilder.Entity<ImportDetail>().Ignore(i => i.TotalPrice);
            modelBuilder.Entity<InventoryCheckDetail>().Ignore(c => c.Diff);

            // 2. Cấu hình Index và Relationship

            // Product
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique(false);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique(true);

            // Supplier
            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.TaxCode)
                .IsUnique(false); // nếu bạn muốn Mã số thuế không trùng, đổi thành true

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Imports)
                .WithOne(i => i.Supplier)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            // Customer
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Exports)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Import – ImportDetail
            modelBuilder.Entity<Import>()
                .HasMany(i => i.ImportDetails)
                .WithOne(d => d.Import)
                .HasForeignKey(d => d.ImportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Export – ExportDetail
            modelBuilder.Entity<Export>()
                .HasMany(e => e.ExportDetails)
                .WithOne(d => d.Export)
                .HasForeignKey(d => d.ExportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product – ImportDetail / ExportDetail
            modelBuilder.Entity<Product>()
                .HasMany(p => p.ImportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.ExportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryCheck – InventoryCheckDetail
            modelBuilder.Entity<InventoryCheck>()
                .HasMany(c => c.Details)
                .WithOne(d => d.InventoryCheck)
                .HasForeignKey(d => d.InventoryCheckId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Xử lý kiểu decimal cho SQLite (nếu bạn dùng SQLite dev)
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
            // Nếu dùng SQL Server trên hosting, các [Column(TypeName = "decimal(18,2)")] trong model đã đủ
        }
    }
}
