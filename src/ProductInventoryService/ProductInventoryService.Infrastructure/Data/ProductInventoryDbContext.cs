using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Infrastructure.Data;

/// <summary>
/// DbContext cho ProductInventoryDB - Cấu hình Fluent API đầy đủ cho tất cả thực thể.
/// Tự động điền CreatedAt/UpdatedAt khi SaveChanges.
/// </summary>
public class ProductInventoryDbContext : DbContext, IProductInventoryDbContext
{
    public ProductInventoryDbContext(DbContextOptions<ProductInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<GoodsReceiptDetail> GoodsReceiptDetails { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<StocktakeSession> StocktakeSessions { get; set; }
    public DbSet<StocktakeLine> StocktakeLines { get; set; }
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is AuditableEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (AuditableEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                entity.UpdatedAt = DateTime.UtcNow;
                entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =============================================
        // CATEGORY - Cấu trúc cây cha-con (Self-referencing)
        // =============================================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Description).HasMaxLength(500);

            entity.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // UNIT - Đơn vị tính
        // =============================================
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("Units");
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Abbreviation).HasMaxLength(20);
        });

        // =============================================
        // PRODUCT - Sản phẩm
        // =============================================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(300);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Barcode).HasMaxLength(100);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.ImportPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.SellPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Weight).HasColumnType("decimal(18,3)");

            entity.HasIndex(p => p.Code).IsUnique().HasDatabaseName("IX_Products_Code");
            entity.HasIndex(p => p.Barcode).HasDatabaseName("IX_Products_Barcode");

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Unit)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // INVENTORY - Tồn kho (1:1 với Product)
        // =============================================
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventories");

            entity.HasOne(i => i.Product)
                .WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => i.ProductId).IsUnique().HasDatabaseName("IX_Inventories_ProductId");
        });

        // =============================================
        // GOODS RECEIPT - Phiếu nhập kho
        // =============================================
        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("GoodsReceipts");
            entity.Property(g => g.ReceiptCode).IsRequired().HasMaxLength(50);
            entity.Property(g => g.SupplierName).IsRequired().HasMaxLength(200);
            entity.Property(g => g.CreatedByName).IsRequired().HasMaxLength(200);
            entity.Property(g => g.Note).HasMaxLength(500);
            entity.Property(g => g.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(g => g.Status).IsRequired().HasMaxLength(20);

            entity.HasIndex(g => g.ReceiptCode).IsUnique().HasDatabaseName("IX_GoodsReceipts_ReceiptCode");
        });

        // =============================================
        // GOODS RECEIPT DETAIL - Chi tiết phiếu nhập
        // =============================================
        modelBuilder.Entity<GoodsReceiptDetail>(entity =>
        {
            entity.ToTable("GoodsReceiptDetails");
            entity.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(d => d.SubTotal).HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.GoodsReceipt)
                .WithMany(g => g.GoodsReceiptDetails)
                .HasForeignKey(d => d.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                .WithMany(p => p.GoodsReceiptDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // INVENTORY TRANSACTION - Lịch sử biến động kho
        // =============================================
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");
            entity.Property(t => t.TransactionType).IsRequired().HasMaxLength(20);
            entity.Property(t => t.ReferenceType).IsRequired().HasMaxLength(30);
            entity.Property(t => t.Note).HasMaxLength(500);

            entity.HasOne(t => t.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new { t.ProductId, t.CreatedAt })
                .HasDatabaseName("IX_InventoryTransactions_ProductId_CreatedAt");
        });

        // =============================================
        // STOCKTAKE SESSION - Inventory counting sessions
        // =============================================
        modelBuilder.Entity<StocktakeSession>(entity =>
        {
            entity.ToTable("StocktakeSessions");
            entity.Property(s => s.StocktakeCode).IsRequired().HasMaxLength(50);
            entity.Property(s => s.CountedByName).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(20);
            entity.Property(s => s.Note).HasMaxLength(500);

            entity.HasIndex(s => s.StocktakeCode).IsUnique().HasDatabaseName("IX_StocktakeSessions_StocktakeCode");
            entity.HasIndex(s => new { s.Status, s.StartedAt }).HasDatabaseName("IX_StocktakeSessions_Status_StartedAt");
        });

        // =============================================
        // STOCKTAKE LINE - Product snapshot for one stocktake
        // =============================================
        modelBuilder.Entity<StocktakeLine>(entity =>
        {
            entity.ToTable("StocktakeLines");
            entity.Property(l => l.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(l => l.ProductName).IsRequired().HasMaxLength(300);
            entity.Property(l => l.UnitName).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Note).HasMaxLength(500);

            entity.HasOne(l => l.StocktakeSession)
                .WithMany(s => s.Lines)
                .HasForeignKey(l => l.StocktakeSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => new { l.StocktakeSessionId, l.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_StocktakeLines_Session_Product");
        });

        // =============================================
        // PROCESSED EVENT - Idempotency
        // =============================================
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");
            entity.HasKey(p => p.EventId);
            entity.Property(p => p.EventId).HasMaxLength(100);
            entity.Property(p => p.ConsumerName).IsRequired().HasMaxLength(200);
        });
    }
}
