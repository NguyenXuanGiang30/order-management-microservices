using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Infrastructure.Data;

public class OrderSalesDbContext : DbContext, IOrderSalesDbContext
{
    public OrderSalesDbContext(DbContextOptions<OrderSalesDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerGroup> CustomerGroups { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<ReturnOrder> ReturnOrders { get; set; }
    public DbSet<ReturnOrderDetail> ReturnOrderDetails { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<CashShift> CashShifts { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionItem> PromotionItems { get; set; }
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
        // CUSTOMER GROUP
        // =============================================
        modelBuilder.Entity<CustomerGroup>(entity =>
        {
            entity.ToTable("CustomerGroups");
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.DefaultDiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(c => c.Note).HasMaxLength(500);

            // Seed Data - 3 nhóm khách hàng mặc định
            entity.HasData(
                new CustomerGroup
                {
                    Id = Guid.Parse("e5f02c6b-67a8-44a6-b51f-4b07fb7b4de1"),
                    Name = "VIP",
                    DefaultDiscountPercent = 10.0m,
                    Note = "Khách hàng VIP - Chiết khấu 10%"
                },
                new CustomerGroup
                {
                    Id = Guid.Parse("c24f74d1-55b2-4d2a-8742-5f657a8a25c1"),
                    Name = "Khách Lẻ",
                    DefaultDiscountPercent = 0.0m,
                    Note = "Khách hàng lẻ - Không chiết khấu"
                },
                new CustomerGroup
                {
                    Id = Guid.Parse("b8e18df0-fc7c-4869-9528-765f14841db1"),
                    Name = "Khách Sỉ",
                    DefaultDiscountPercent = 15.0m,
                    Note = "Khách hàng sỉ - Chiết khấu 15%"
                }
            );
        });

        // =============================================
        // CUSTOMER
        // =============================================
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.Property(c => c.Code).IsRequired().HasMaxLength(50);
            entity.Property(c => c.FullName).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Phone).HasMaxLength(20);
            entity.Property(c => c.Email).HasMaxLength(200);
            entity.Property(c => c.Address).HasMaxLength(500);
            entity.Property(c => c.TaxCode).HasMaxLength(50);
            entity.Property(c => c.TotalPurchased).HasColumnType("decimal(18,2)");
            entity.Property(c => c.DebtAmount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.Note).HasMaxLength(500);

            entity.HasIndex(c => c.Code).IsUnique().HasDatabaseName("IX_Customers_Code");

            entity.HasOne(c => c.CustomerGroup)
                .WithMany(g => g.Customers)
                .HasForeignKey(c => c.CustomerGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =============================================
        // SUPPLIER
        // =============================================
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.Property(s => s.Code).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.ContactPerson).HasMaxLength(200);
            entity.Property(s => s.ContactPhone).HasMaxLength(20);
            entity.Property(s => s.ContactEmail).HasMaxLength(200);
            entity.Property(s => s.Address).HasMaxLength(500);
            entity.Property(s => s.TaxCode).HasMaxLength(50);
            entity.Property(s => s.DebtAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Note).HasMaxLength(500);

            entity.HasIndex(s => s.Code).IsUnique().HasDatabaseName("IX_Suppliers_Code");
        });

        // =============================================
        // ORDER
        // =============================================
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.Property(o => o.OrderCode).IsRequired().HasMaxLength(50);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(o => o.CreatedByName).IsRequired().HasMaxLength(200);
            entity.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(o => o.DiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.PromotionCode).HasMaxLength(50);
            entity.Property(o => o.PromotionName).HasMaxLength(200);
            entity.Property(o => o.PromotionDiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.FinalAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.PaidAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.DebtAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.PaymentMethod).HasMaxLength(50);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Note).HasMaxLength(500);

            entity.HasIndex(o => o.OrderCode).IsUnique().HasDatabaseName("IX_Orders_OrderCode");
            entity.HasIndex(o => new { o.CustomerId, o.CreatedAt }).HasDatabaseName("IX_Orders_CustomerId_CreatedAt");

            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // ORDER DETAIL
        // =============================================
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails");
            entity.Property(d => d.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(d => d.ProductName).IsRequired().HasMaxLength(300);
            entity.Property(d => d.UnitName).IsRequired().HasMaxLength(100);
            entity.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(d => d.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(d => d.CostTotal).HasColumnType("decimal(18,2)");
            entity.Property(d => d.DiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(d => d.SubTotal).HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // ORDER STATUS HISTORY
        // =============================================
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistories");
            entity.Property(h => h.OldStatus).HasMaxLength(20);
            entity.Property(h => h.NewStatus).IsRequired().HasMaxLength(20);
            entity.Property(h => h.Note).HasMaxLength(500);
            entity.Property(h => h.ChangedByName).IsRequired().HasMaxLength(200);

            entity.HasIndex(h => new { h.OrderId, h.CreatedAt }).HasDatabaseName("IX_OrderStatusHistories_OrderId_CreatedAt");

            entity.HasOne(h => h.Order)
                .WithMany(o => o.OrderStatusHistories)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // RETURN ORDER
        // =============================================
        modelBuilder.Entity<ReturnOrder>(entity =>
        {
            entity.ToTable("ReturnOrders");
            entity.Property(r => r.ReturnCode).IsRequired().HasMaxLength(50);
            entity.Property(r => r.TotalRefundAmount).HasColumnType("decimal(18,2)");
            entity.Property(r => r.ReturnReason).HasMaxLength(500);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(20);

            entity.HasIndex(r => r.OrderId).HasDatabaseName("IX_ReturnOrders_OrderId");

            entity.HasOne(r => r.Order)
                .WithMany(o => o.ReturnOrders)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // RETURN ORDER DETAIL
        // =============================================
        modelBuilder.Entity<ReturnOrderDetail>(entity =>
        {
            entity.ToTable("ReturnOrderDetails");
            entity.Property(d => d.RefundAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.ReturnOrder)
                .WithMany(r => r.ReturnOrderDetails)
                .HasForeignKey(d => d.ReturnOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // PAYMENT TRANSACTION
        // =============================================
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.Property(p => p.PaymentMethod).HasMaxLength(50);
            entity.Property(p => p.Note).HasMaxLength(500);
            entity.Property(p => p.ReceivedByName).IsRequired().HasMaxLength(200);

            entity.HasOne(p => p.Order)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Customer)
                .WithMany(c => c.PaymentTransactions)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // CASH SHIFT
        // =============================================
        modelBuilder.Entity<CashShift>(entity =>
        {
            entity.ToTable("CashShifts");
            entity.Property(s => s.ShiftCode).IsRequired().HasMaxLength(50);
            entity.Property(s => s.CashierName).IsRequired().HasMaxLength(200);
            entity.Property(s => s.OpeningCash).HasColumnType("decimal(18,2)");
            entity.Property(s => s.ExpectedCash).HasColumnType("decimal(18,2)");
            entity.Property(s => s.ActualCash).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Variance).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Status).IsRequired().HasMaxLength(20);
            entity.Property(s => s.Note).HasMaxLength(500);

            entity.HasIndex(s => s.ShiftCode).IsUnique().HasDatabaseName("IX_CashShifts_ShiftCode");
            entity.HasIndex(s => new { s.CashierId, s.Status }).HasDatabaseName("IX_CashShifts_Cashier_Status");
        });

        // =============================================
        // PROMOTION
        // =============================================
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions");
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.PromotionType).IsRequired().HasMaxLength(30);
            entity.Property(p => p.DiscountType).IsRequired().HasMaxLength(30);
            entity.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MinimumOrderAmount).HasColumnType("decimal(18,2)");

            entity.HasIndex(p => p.Code).IsUnique().HasDatabaseName("IX_Promotions_Code");
            entity.HasIndex(p => new { p.IsActive, p.StartAt, p.EndAt }).HasDatabaseName("IX_Promotions_Active_DateRange");
        });

        modelBuilder.Entity<PromotionItem>(entity =>
        {
            entity.ToTable("PromotionItems");
            entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(p => p.ProductName).IsRequired().HasMaxLength(300);

            entity.HasOne(p => p.Promotion)
                .WithMany(p => p.PromotionItems)
                .HasForeignKey(p => p.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
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
        // SEED CUSTOMERS, SUPPLIERS & PROMOTIONS
        // =============================================
        SeedInitialSalesData(modelBuilder);
    }

    private void SeedInitialSalesData(ModelBuilder modelBuilder)
    {
        var catLelId = Guid.Parse("c24f74d1-55b2-4d2a-8742-5f657a8a25c1");
        var catVipId = Guid.Parse("e5f02c6b-67a8-44a6-b51f-4b07fb7b4de1");
        var catSiId = Guid.Parse("b8e18df0-fc7c-4869-9528-765f14841db1");

        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var adminUserId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");

        // 1. Seed Customers
        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = Guid.Parse("d0000001-1111-2222-3333-444455556666"),
                Code = "KH001",
                FullName = "Nguyễn Văn Anh",
                Phone = "0912345678",
                Email = "vananh@gmail.com",
                Address = "123 Đường Láng, Đống Đa, Hà Nội",
                TaxCode = null,
                TotalPurchased = 0m,
                DebtAmount = 0m,
                Note = "Khách hàng lẻ thân quen",
                CustomerGroupId = catLelId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Customer
            {
                Id = Guid.Parse("d0000002-1111-2222-3333-444455556666"),
                Code = "KH002",
                FullName = "Trần Thị Bình",
                Phone = "0987654321",
                Email = "thibinh@gmail.com",
                Address = "456 Nguyễn Trãi, Thanh Xuân, Hà Nội",
                TaxCode = null,
                TotalPurchased = 1500000m,
                DebtAmount = 0m,
                Note = "Khách hàng VIP mua nhiều",
                CustomerGroupId = catVipId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Customer
            {
                Id = Guid.Parse("d0000003-1111-2222-3333-444455556666"),
                Code = "KH003",
                FullName = "Lê Hoàng Cường",
                Phone = "0905123456",
                Email = "hoangcuong@gmail.com",
                Address = "789 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
                TaxCode = "8012345678",
                TotalPurchased = 12000000m,
                DebtAmount = 2000000m,
                Note = "Đại lý bán sỉ tạp hóa nhỏ",
                CustomerGroupId = catSiId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Customer
            {
                Id = Guid.Parse("d0000004-1111-2222-3333-444455556666"),
                Code = "KH004",
                FullName = "Phạm Minh Đức",
                Phone = "0936789012",
                Email = "minhduc@gmail.com",
                Address = "101 Cầu Giấy, Quan Hoa, Hà Nội",
                TaxCode = null,
                TotalPurchased = 300000m,
                DebtAmount = 0m,
                Note = "Khách vãng lai mua lẻ",
                CustomerGroupId = catLelId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Customer
            {
                Id = Guid.Parse("d0000005-1111-2222-3333-444455556666"),
                Code = "KH005",
                FullName = "Đỗ Thu Hà",
                Phone = "0977111222",
                Email = "thuha@gmail.com",
                Address = "202 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội",
                TaxCode = null,
                TotalPurchased = 2500000m,
                DebtAmount = 0m,
                Note = "Khách hàng VIP mua hàng tháng",
                CustomerGroupId = catVipId,
                CreatedAt = createdAt,
                IsActive = true
            }
        );

        // 2. Seed Suppliers
        modelBuilder.Entity<Supplier>().HasData(
            new Supplier
            {
                Id = Guid.Parse("e0000001-1111-2222-3333-444455556666"),
                Code = "NCC-ACECOOK",
                Name = "Công ty Cổ phần Acecook Việt Nam",
                ContactPerson = "Ông Kajiwara Junichi",
                ContactPhone = "02838154060",
                ContactEmail = "info@acecookvietnam.com",
                Address = "Lô II-3, Đường số 11, KCN Tân Bình, Tân Phú, TP. HCM",
                TaxCode = "0303525911",
                DebtAmount = 0m,
                Note = "Nhà cung cấp các loại mì ăn liền Hảo Hảo",
                CreatedBy = adminUserId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Supplier
            {
                Id = Guid.Parse("e0000002-1111-2222-3333-444455556666"),
                Code = "NCC-UNILEVER",
                Name = "Công ty TNHH Unilever Việt Nam",
                ContactPerson = "Bà Nguyễn Thị Bích Vân",
                ContactPhone = "02854131100",
                ContactEmail = "tuvankhachhang@unilever.com",
                Address = "Lô A2-3, KCN Tây Bắc Củ Chi, Củ Chi, TP. HCM",
                TaxCode = "0301438692",
                DebtAmount = 15000000m,
                Note = "Nhà cung cấp Sunlight, OMO, Clear, Lifebuoy...",
                CreatedBy = adminUserId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Supplier
            {
                Id = Guid.Parse("e0000003-1111-2222-3333-444455556666"),
                Code = "NCC-VINAMILK",
                Name = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
                ContactPerson = "Bà Mai Kiều Liên",
                ContactPhone = "02854155555",
                ContactEmail = "vinamilk@vinamilk.com.vn",
                Address = "10 Tân Trào, Tân Phú, Quận 7, TP. HCM",
                TaxCode = "0300588569",
                DebtAmount = 0m,
                Note = "Nhà cung cấp các sản phẩm sữa tươi, sữa chua, sữa đặc",
                CreatedBy = adminUserId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Supplier
            {
                Id = Guid.Parse("e0000004-1111-2222-3333-444455556666"),
                Code = "NCC-PEPSICO",
                Name = "Công ty TNHH Suntory PepsiCo Việt Nam",
                ContactPerson = "Ông Jahanzeb Khan",
                ContactPhone = "02838219468",
                ContactEmail = "contact@suntorypepsico.vn",
                Address = "Cao ốc Sheraton, 88 Đồng Khởi, Quận 1, TP. HCM",
                TaxCode = "0312051832",
                DebtAmount = 8000000m,
                Note = "Nhà cung cấp Pepsi, 7Up, Aquafina, Sting...",
                CreatedBy = adminUserId,
                CreatedAt = createdAt,
                IsActive = true
            },
            new Supplier
            {
                Id = Guid.Parse("e0000005-1111-2222-3333-444455556666"),
                Code = "NCC-HEINEKEN",
                Name = "Công ty TNHH Nhà Máy Bia Heineken Việt Nam",
                ContactPerson = "Ông Alexander Koch",
                ContactPhone = "02838222755",
                ContactEmail = "heinekenvietnam@heineken.com",
                Address = "Tầng 18, Tòa nhà Vietcombank, 5 Công Trường Mê Linh, Quận 1, TP. HCM",
                TaxCode = "0300830053",
                DebtAmount = 0m,
                Note = "Nhà cung cấp bia Heineken, Tiger, Larue...",
                CreatedBy = adminUserId,
                CreatedAt = createdAt,
                IsActive = true
            }
        );

        // 3. Seed Promotions
        modelBuilder.Entity<Promotion>().HasData(
            new Promotion
            {
                Id = Guid.Parse("a0000001-1111-2222-3333-444455556666"),
                Code = "MUNGKHAITRUONG",
                Name = "Mừng Khai Trương Siêu Thị",
                Description = "Giảm giá 5% cho toàn bộ đơn hàng trị giá từ 100,000 VND trở lên",
                PromotionType = "OrderDiscount",
                DiscountType = "Percent",
                DiscountValue = 5m,
                MinimumOrderAmount = 100000m,
                StartAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedBy = adminUserId,
                IsActive = true,
                CreatedAt = createdAt
            },
            new Promotion
            {
                Id = Guid.Parse("a0000002-1111-2222-3333-444455556666"),
                Code = "TRIANKHACHHANG",
                Name = "Tri Ân Khách Hàng Thân Thiết",
                Description = "Giảm giá 10% cho toàn bộ đơn hàng trị giá từ 200,000 VND trở lên",
                PromotionType = "OrderDiscount",
                DiscountType = "Percent",
                DiscountValue = 10m,
                MinimumOrderAmount = 200000m,
                StartAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedBy = adminUserId,
                IsActive = true,
                CreatedAt = createdAt
            }
        );
    }
}
