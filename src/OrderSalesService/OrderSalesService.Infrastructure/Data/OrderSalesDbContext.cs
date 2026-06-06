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
    }
}
