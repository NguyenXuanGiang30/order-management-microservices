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
    public DbSet<UnitConversion> UnitConversions { get; set; }
    public DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }

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

        // Track price changes
        var priceChanges = new List<ProductPriceHistory>();
        var modifiedProducts = ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedProducts)
        {
            var oldImportPrice = entry.OriginalValues[nameof(Product.ImportPrice)] is decimal val1 ? val1 : 0m;
            var newImportPrice = entry.Entity.ImportPrice;

            var oldSellPrice = entry.OriginalValues[nameof(Product.SellPrice)] is decimal val2 ? val2 : 0m;
            var newSellPrice = entry.Entity.SellPrice;

            if (oldImportPrice != newImportPrice || oldSellPrice != newSellPrice)
            {
                priceChanges.Add(new ProductPriceHistory
                {
                    ProductId = entry.Entity.Id,
                    OldImportPrice = oldImportPrice,
                    NewImportPrice = newImportPrice,
                    OldSellPrice = oldSellPrice,
                    NewSellPrice = newSellPrice,
                    ChangedBy = "Admin",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var addedProducts = ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in addedProducts)
        {
            priceChanges.Add(new ProductPriceHistory
            {
                ProductId = entry.Entity.Id,
                OldImportPrice = 0,
                NewImportPrice = entry.Entity.ImportPrice,
                OldSellPrice = 0,
                NewSellPrice = entry.Entity.SellPrice,
                ChangedBy = "Admin",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (priceChanges.Any())
        {
            await ProductPriceHistories.AddRangeAsync(priceChanges, cancellationToken);
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

        // =============================================
        // UNIT CONVERSION - Quy đổi đơn vị tính
        // =============================================
        modelBuilder.Entity<UnitConversion>(entity =>
        {
            entity.ToTable("UnitConversions");
            entity.Property(u => u.Factor).HasColumnType("decimal(18,4)");

            entity.HasOne(u => u.Product)
                .WithMany()
                .HasForeignKey(u => u.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.FromUnit)
                .WithMany()
                .HasForeignKey(u => u.FromUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.ToUnit)
                .WithMany()
                .HasForeignKey(u => u.ToUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(u => new { u.ProductId, u.FromUnitId, u.ToUnitId })
                .IsUnique()
                .HasDatabaseName("IX_UnitConversions_Product_From_To");
        });

        // =============================================
        // PRODUCT PRICE HISTORY
        // =============================================
        modelBuilder.Entity<ProductPriceHistory>(entity =>
        {
            entity.ToTable("ProductPriceHistories");
            entity.Property(p => p.OldImportPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.NewImportPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.OldSellPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.NewSellPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.ChangedBy).HasMaxLength(100);

            entity.HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // SEED PRODUCTS, UNITS, CATEGORIES & INVENTORY
        // =============================================
        SeedInitialProducts(modelBuilder);
    }

    private void SeedInitialProducts(ModelBuilder modelBuilder)
    {
        // 1. Seed Units
        var unitGoiId = Guid.Parse("40c5f2b8-bd2e-4b20-8b1e-7f61b021c324");
        var unitLonId = Guid.Parse("5a9602a8-12c8-472d-bf84-7a1a2b3c4d5e");
        var unitChaiId = Guid.Parse("6b8703a9-23d9-483e-cf95-8b2b3c4d5e6f");
        var unitHopId = Guid.Parse("7c7804b0-34ea-494f-df06-9c3b4c5d6e7f");
        var unitTuiId = Guid.Parse("8d6905c1-45fb-4a50-ef17-0d4b5c6d7e8f");
        var unitHuId = Guid.Parse("9e5a06d2-56fc-4b61-ff28-1e5b6c7d8e9f");

        modelBuilder.Entity<Unit>().HasData(
            new Unit { Id = unitGoiId, Name = "Gói", Abbreviation = "gói", IsActive = true },
            new Unit { Id = unitLonId, Name = "Lon", Abbreviation = "lon", IsActive = true },
            new Unit { Id = unitChaiId, Name = "Chai", Abbreviation = "chai", IsActive = true },
            new Unit { Id = unitHopId, Name = "Hộp", Abbreviation = "hộp", IsActive = true },
            new Unit { Id = unitTuiId, Name = "Túi", Abbreviation = "túi", IsActive = true },
            new Unit { Id = unitHuId, Name = "Hũ", Abbreviation = "hũ", IsActive = true }
        );

        // 2. Seed Categories
        var catThucPhamId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
        var catNuocGiaiKhatId = Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e");
        var catSuaId = Guid.Parse("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f");
        var catBanhKeoId = Guid.Parse("d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a");
        var catHoaMyPhamId = Guid.Parse("e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9a0b");

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = catThucPhamId, Name = "Thực phẩm khô & Gia vị", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = catNuocGiaiKhatId, Name = "Nước giải khát, Bia & Nước suối", SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = catSuaId, Name = "Sữa & Sản phẩm từ sữa", SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = catBanhKeoId, Name = "Bánh kẹo & Đồ ăn vặt", SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = catHoaMyPhamId, Name = "Hóa mỹ phẩm & Chăm sóc cá nhân", SortOrder = 5, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Helper list to seed products and inventories
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var products = new[]
        {
            // Nhóm 1: Thực phẩm khô & Gia vị
            new { Id = Guid.Parse("f0010001-1111-2222-3333-444455556666"), Code = "TP-MHH", Name = "Mì ăn liền Hảo Hảo tôm chua cay 75g", Barcode = "8934563138162", ImportPrice = 3200m, SellPrice = 4000m, Weight = 0.075m, CatId = catThucPhamId, UnitId = unitGoiId },
            new { Id = Guid.Parse("f0010002-1111-2222-3333-444455556666"), Code = "TP-DAS", Name = "Dầu ăn Simply nguyên chất 1L", Barcode = "8936011381014", ImportPrice = 42000m, SellPrice = 52000m, Weight = 0.900m, CatId = catThucPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0010003-1111-2222-3333-444455556666"), Code = "TP-TCS", Name = "Tương ớt Chinsu chai 250g", Barcode = "8936017362352", ImportPrice = 10500m, SellPrice = 14000m, Weight = 0.250m, CatId = catThucPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0010004-1111-2222-3333-444455556666"), Code = "TP-NMNN", Name = "Nước mắm Nam Ngư 3 trong 1 chai 750ml", Barcode = "8936017368026", ImportPrice = 32000m, SellPrice = 39000m, Weight = 0.750m, CatId = catThucPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0010005-1111-2222-3333-444455556666"), Code = "TP-HNKN", Name = "Hạt nêm Knorr Thịt thăn Xương ống 400g", Barcode = "8934839111815", ImportPrice = 28000m, SellPrice = 34000m, Weight = 0.400m, CatId = catThucPhamId, UnitId = unitGoiId },
            new { Id = Guid.Parse("f0010006-1111-2222-3333-444455556666"), Code = "TP-GAO", Name = "Gạo thơm Jasmine thượng hạng túi 5kg", Barcode = "8938508216010", ImportPrice = 105000m, SellPrice = 125000m, Weight = 5.000m, CatId = catThucPhamId, UnitId = unitTuiId },
            new { Id = Guid.Parse("f0010007-1111-2222-3333-444455556666"), Code = "TP-MKK", Name = "Mì gói Kokomi Đại 90 Tôm Chua Cay", Barcode = "8936017369016", ImportPrice = 2800m, SellPrice = 3500m, Weight = 0.090m, CatId = catThucPhamId, UnitId = unitGoiId },
            new { Id = Guid.Parse("f0010008-1111-2222-3333-444455556666"), Code = "TP-MUOI", Name = "Muối tinh sấy i-ốt Visalco 500g", Barcode = "8935041200055", ImportPrice = 4000m, SellPrice = 6000m, Weight = 0.500m, CatId = catThucPhamId, UnitId = unitGoiId },

            // Nhóm 2: Nước giải khát, Bia & Nước suối
            new { Id = Guid.Parse("f0020001-1111-2222-3333-444455556666"), Code = "NG-COCA", Name = "Nước ngọt Coca Cola lon 320ml", Barcode = "8935049500461", ImportPrice = 8000m, SellPrice = 10000m, Weight = 0.320m, CatId = catNuocGiaiKhatId, UnitId = unitLonId },
            new { Id = Guid.Parse("f0020002-1111-2222-3333-444455556666"), Code = "NG-KEN", Name = "Bia Heineken Silver lon 330ml", Barcode = "8934822201332", ImportPrice = 16000m, SellPrice = 20000m, Weight = 0.330m, CatId = catNuocGiaiKhatId, UnitId = unitLonId },
            new { Id = Guid.Parse("f0020003-1111-2222-3333-444455556666"), Code = "NG-LAVIE", Name = "Nước khoáng thiên nhiên La Vie 500ml", Barcode = "8935026810019", ImportPrice = 4000m, SellPrice = 6000m, Weight = 0.500m, CatId = catNuocGiaiKhatId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0020004-1111-2222-3333-444455556666"), Code = "NG-PEPSI", Name = "Nước ngọt Pepsi lon 320ml", Barcode = "8938502391010", ImportPrice = 7800m, SellPrice = 9500m, Weight = 0.320m, CatId = catNuocGiaiKhatId, UnitId = unitLonId },
            new { Id = Guid.Parse("f0020005-1111-2222-3333-444455556666"), Code = "NG-TIGER", Name = "Bia Tiger lon 330ml", Barcode = "8934822301339", ImportPrice = 13500m, SellPrice = 17000m, Weight = 0.330m, CatId = catNuocGiaiKhatId, UnitId = unitLonId },
            new { Id = Guid.Parse("f0020006-1111-2222-3333-444455556666"), Code = "NG-REDBULL", Name = "Nước tăng lực Redbull lon 250ml", Barcode = "8851012110115", ImportPrice = 10000m, SellPrice = 12500m, Weight = 0.250m, CatId = catNuocGiaiKhatId, UnitId = unitLonId },
            new { Id = Guid.Parse("f0020007-1111-2222-3333-444455556666"), Code = "NG-KHONGDO", Name = "Trà xanh Không Độ 500ml", Barcode = "8936006030019", ImportPrice = 7000m, SellPrice = 9000m, Weight = 0.500m, CatId = catNuocGiaiKhatId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0020008-1111-2222-3333-444455556666"), Code = "NG-7UP", Name = "Nước ngọt 7Up lon 320ml", Barcode = "8938502391034", ImportPrice = 7800m, SellPrice = 9500m, Weight = 0.320m, CatId = catNuocGiaiKhatId, UnitId = unitLonId },

            // Nhóm 3: Sữa & Sản phẩm từ sữa
            new { Id = Guid.Parse("f0030001-1111-2222-3333-444455556666"), Code = "SU-VNM", Name = "Sữa tươi tiệt trùng Vinamilk ít đường 180ml", Barcode = "8934673122457", ImportPrice = 6500m, SellPrice = 8500m, Weight = 0.180m, CatId = catSuaId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0030002-1111-2222-3333-444455556666"), Code = "SU-TH", Name = "Sữa tươi tiệt trùng TH True Milk ít đường 180ml", Barcode = "8936049080036", ImportPrice = 6800m, SellPrice = 9000m, Weight = 0.180m, CatId = catSuaId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0030003-1111-2222-3333-444455556666"), Code = "SU-MILO", Name = "Sữa lúa mạch Nestlé Milo hộp 180ml", Barcode = "8934804021200", ImportPrice = 7000m, SellPrice = 9200m, Weight = 0.180m, CatId = catSuaId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0030004-1111-2222-3333-444455556666"), Code = "SU-CHUA", Name = "Sữa chua ăn Vinamilk có đường hộp 100g", Barcode = "8934673130179", ImportPrice = 5000m, SellPrice = 6500m, Weight = 0.100m, CatId = catSuaId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0030005-1111-2222-3333-444455556666"), Code = "SU-ONTHO", Name = "Sữa đặc Ông Thọ đỏ lon 380g", Barcode = "8934673120156", ImportPrice = 22000m, SellPrice = 26000m, Weight = 0.380m, CatId = catSuaId, UnitId = unitLonId },

            // Nhóm 4: Bánh kẹo & Đồ ăn vặt
            new { Id = Guid.Parse("f0040001-1111-2222-3333-444455556666"), Code = "BK-COSY", Name = "Bánh quy Cosy Kinh Đô Marie gói 144g", Barcode = "8934681144007", ImportPrice = 12000m, SellPrice = 16000m, Weight = 0.144m, CatId = catBanhKeoId, UnitId = unitGoiId },
            new { Id = Guid.Parse("f0040002-1111-2222-3333-444455556666"), Code = "BK-LAYS", Name = "Khoai tây chiên Lay's vị tự nhiên gói 95g", Barcode = "8936079010123", ImportPrice = 16000m, SellPrice = 21000m, Weight = 0.095m, CatId = catBanhKeoId, UnitId = unitGoiId },
            new { Id = Guid.Parse("f0040003-1111-2222-3333-444455556666"), Code = "BK-CHOCO", Name = "Bánh ChocoPie Orion hộp 12 cái 396g", Barcode = "8936036010041", ImportPrice = 44000m, SellPrice = 55000m, Weight = 0.396m, CatId = catBanhKeoId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0040004-1111-2222-3333-444455556666"), Code = "BK-HARIBO", Name = "Kẹo dẻo Haribo Goldbears gói 80g", Barcode = "4001686301524", ImportPrice = 22000m, SellPrice = 28000m, Weight = 0.080m, CatId = catBanhKeoId, UnitId = unitGoiId },
            new { Id = Guid.Parse("f0040005-1111-2222-3333-444455556666"), Code = "BK-PRINGLES", Name = "Snack khoai tây Pringles vị tự nhiên 110g", Barcode = "8886467100017", ImportPrice = 32000m, SellPrice = 40000m, Weight = 0.110m, CatId = catBanhKeoId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0040006-1111-2222-3333-444455556666"), Code = "BK-GUM", Name = "Kẹo cao su Doublemint sọc 5 tép", Barcode = "022000004944", ImportPrice = 4000m, SellPrice = 6000m, Weight = 0.015m, CatId = catBanhKeoId, UnitId = unitGoiId },

            // Nhóm 5: Hóa mỹ phẩm & Chăm sóc cá nhân
            new { Id = Guid.Parse("f0050001-1111-2222-3333-444455556666"), Code = "HM-SL", Name = "Nước rửa chén Sunlight chanh chai 750g", Barcode = "8934839130779", ImportPrice = 24000m, SellPrice = 30000m, Weight = 0.750m, CatId = catHoaMyPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0050002-1111-2222-3333-444455556666"), Code = "HM-OMO", Name = "Bột giặt OMO Comfort tinh dầu thơm túi 3.6kg", Barcode = "8934839141089", ImportPrice = 165000m, SellPrice = 198000m, Weight = 3.600m, CatId = catHoaMyPhamId, UnitId = unitTuiId },
            new { Id = Guid.Parse("f0050003-1111-2222-3333-444455556666"), Code = "HM-CG", Name = "Kem đánh răng Colgate ngừa sâu răng 225g", Barcode = "6901396340243", ImportPrice = 28000m, SellPrice = 35000m, Weight = 0.225m, CatId = catHoaMyPhamId, UnitId = unitHopId },
            new { Id = Guid.Parse("f0050004-1111-2222-3333-444455556666"), Code = "HM-CLEAR", Name = "Dầu gội Clear Bạc Hà thơm mát chai 630ml", Barcode = "8934839154386", ImportPrice = 125000m, SellPrice = 149000m, Weight = 0.630m, CatId = catHoaMyPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0050005-1111-2222-3333-444455556666"), Code = "HM-LIFEBUOY", Name = "Sữa tắm Lifebuoy bảo vệ vượt trội chai 850g", Barcode = "8934839129056", ImportPrice = 140000m, SellPrice = 169000m, Weight = 0.850m, CatId = catHoaMyPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0050006-1111-2222-3333-444455556666"), Code = "HM-LAUSAN", Name = "Nước lau sàn Sunlight Hương Hoa thiên nhiên 1kg", Barcode = "8934839123016", ImportPrice = 23000m, SellPrice = 29000m, Weight = 1.000m, CatId = catHoaMyPhamId, UnitId = unitChaiId },
            new { Id = Guid.Parse("f0050007-1111-2222-3333-444455556666"), Code = "HM-DOWNY", Name = "Nước xả vải Downy Huyền Bí túi 1.6L", Barcode = "4902430882101", ImportPrice = 110000m, SellPrice = 135000m, Weight = 1.600m, CatId = catHoaMyPhamId, UnitId = unitTuiId }
        };

        foreach (var p in products)
        {
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Name,
                    Barcode = p.Barcode,
                    ImportPrice = p.ImportPrice,
                    SellPrice = p.SellPrice,
                    ImageUrl = null,
                    Weight = p.Weight,
                    CategoryId = p.CatId,
                    UnitId = p.UnitId,
                    IsActive = true,
                    CreatedAt = createdAt
                }
            );

            modelBuilder.Entity<Inventory>().HasData(
                new Inventory
                {
                    Id = Guid.Parse(p.Id.ToString().Replace("f00", "e00")),
                    ProductId = p.Id,
                    QuantityOnHand = 100,
                    QuantityReserved = 0,
                    MinThreshold = 10,
                    MaxThreshold = 1000,
                    LastUpdated = createdAt
                }
            );
        }
    }
}

