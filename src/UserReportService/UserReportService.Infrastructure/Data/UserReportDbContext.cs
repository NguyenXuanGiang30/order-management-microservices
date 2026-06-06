using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Infrastructure.Data;

public class UserReportDbContext : DbContext, IUserReportDbContext
{
    public UserReportDbContext(DbContextOptions<UserReportDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<DailySalesSummary> DailySalesSummaries { get; set; }
    public DbSet<MonthlySalesSummary> MonthlySalesSummaries { get; set; }
    public DbSet<DailyProfitSummary> DailyProfitSummaries { get; set; }
    public DbSet<MonthlyProfitSummary> MonthlyProfitSummaries { get; set; }
    public DbSet<ProductProfitSummary> ProductProfitSummaries { get; set; }
    public DbSet<TopProductSummary> TopProductSummaries { get; set; }
    public DbSet<TopCustomerSummary> TopCustomerSummaries { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<BackupRecord> BackupRecords { get; set; }
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
        // USER
        // =============================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).HasMaxLength(200);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(50);

            entity.HasIndex(u => u.Username).IsUnique().HasDatabaseName("IX_Users_Username");
        });

        // =============================================
        // REFRESH TOKEN
        // =============================================
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.Property(r => r.Token).IsRequired().HasMaxLength(500);
            entity.Property(r => r.CreatedByIp).IsRequired().HasMaxLength(50);
            entity.Property(r => r.RevokedByIp).HasMaxLength(50);
            entity.Property(r => r.ReplacedByToken).HasMaxLength(500);

            entity.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // ACTIVITY LOG
        // =============================================
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(50);
            entity.Property(a => a.ServiceName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Severity).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Description).HasMaxLength(500);
            entity.Property(a => a.IpAddress).HasMaxLength(50);

            entity.HasOne(a => a.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // DAILY SALES SUMMARY
        // =============================================
        modelBuilder.Entity<DailySalesSummary>(entity =>
        {
            entity.ToTable("DailySalesSummaries");
            entity.Property(d => d.TotalRevenue).HasColumnType("decimal(18,2)");
            entity.Property(d => d.TotalDiscount).HasColumnType("decimal(18,2)");

            entity.HasIndex(d => d.ReportDate).IsUnique().HasDatabaseName("IX_DailySalesSummaries_ReportDate");
        });

        // =============================================
        // MONTHLY SALES SUMMARY
        // =============================================
        modelBuilder.Entity<MonthlySalesSummary>(entity =>
        {
            entity.ToTable("MonthlySalesSummaries");
            entity.Property(m => m.TotalRevenue).HasColumnType("decimal(18,2)");
            entity.Property(m => m.TotalDiscount).HasColumnType("decimal(18,2)");

            entity.HasIndex(m => new { m.Year, m.Month }).IsUnique().HasDatabaseName("IX_MonthlySalesSummaries_Year_Month");
        });

        // =============================================
        // PROFIT SUMMARIES
        // =============================================
        modelBuilder.Entity<DailyProfitSummary>(entity =>
        {
            entity.ToTable("DailyProfitSummaries");
            entity.Property(p => p.TotalRevenue).HasColumnType("decimal(18,2)");
            entity.Property(p => p.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(p => p.GrossProfit).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MarginPercent).HasColumnType("decimal(7,2)");
            entity.HasIndex(p => p.ReportDate).IsUnique().HasDatabaseName("IX_DailyProfitSummaries_ReportDate");
        });

        modelBuilder.Entity<MonthlyProfitSummary>(entity =>
        {
            entity.ToTable("MonthlyProfitSummaries");
            entity.Property(p => p.TotalRevenue).HasColumnType("decimal(18,2)");
            entity.Property(p => p.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(p => p.GrossProfit).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MarginPercent).HasColumnType("decimal(7,2)");
            entity.HasIndex(p => new { p.Year, p.Month }).IsUnique().HasDatabaseName("IX_MonthlyProfitSummaries_Year_Month");
        });

        modelBuilder.Entity<ProductProfitSummary>(entity =>
        {
            entity.ToTable("ProductProfitSummaries");
            entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(p => p.ProductName).IsRequired().HasMaxLength(300);
            entity.Property(p => p.TotalRevenue).HasColumnType("decimal(18,2)");
            entity.Property(p => p.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(p => p.GrossProfit).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MarginPercent).HasColumnType("decimal(7,2)");
            entity.HasIndex(p => new { p.Year, p.Month, p.ProductId }).IsUnique().HasDatabaseName("IX_ProductProfitSummaries_Period_Product");
        });

        // =============================================
        // TOP PRODUCT SUMMARY
        // =============================================
        modelBuilder.Entity<TopProductSummary>(entity =>
        {
            entity.ToTable("TopProductSummaries");
            entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(p => p.ProductName).IsRequired().HasMaxLength(300);
            entity.Property(p => p.TotalRevenueGenerated).HasColumnType("decimal(18,2)");
        });

        // =============================================
        // TOP CUSTOMER SUMMARY
        // =============================================
        modelBuilder.Entity<TopCustomerSummary>(entity =>
        {
            entity.ToTable("TopCustomerSummaries");
            entity.Property(c => c.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(c => c.CustomerPhone).HasMaxLength(20);
            entity.Property(c => c.TotalSpent).HasColumnType("decimal(18,2)");
        });

        // =============================================
        // PERMISSIONS
        // =============================================
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.Property(p => p.Code).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Group).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.HasIndex(p => p.Code).IsUnique().HasDatabaseName("IX_Permissions_Code");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.Property(p => p.Role).IsRequired().HasMaxLength(50);
            entity.Property(p => p.PermissionCode).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => new { p.Role, p.PermissionCode }).IsUnique().HasDatabaseName("IX_RolePermissions_Role_Permission");
        });

        // =============================================
        // NOTIFICATIONS
        // =============================================
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.Severity).IsRequired().HasMaxLength(20);
            entity.Property(n => n.Link).HasMaxLength(500);
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt }).HasDatabaseName("IX_Notifications_User_Read_CreatedAt");
        });

        // =============================================
        // BACKUP RECORDS
        // =============================================
        modelBuilder.Entity<BackupRecord>(entity =>
        {
            entity.ToTable("BackupRecords");
            entity.Property(b => b.BackupId).IsRequired().HasMaxLength(100);
            entity.Property(b => b.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(b => b.Status).IsRequired().HasMaxLength(30);
            entity.Property(b => b.CreatedByName).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Note).HasMaxLength(1000);
            entity.HasIndex(b => b.BackupId).IsUnique().HasDatabaseName("IX_BackupRecords_BackupId");
        });

        // =============================================
        // PROCESSED EVENT
        // =============================================
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");
            entity.HasKey(p => p.EventId);
            entity.Property(p => p.EventId).HasMaxLength(100);
            entity.Property(p => p.ConsumerName).IsRequired().HasMaxLength(200);
        });

        // =============================================
        // SEED DATA FOR USERS
        // =============================================
        SeedInitialUsers(modelBuilder);
    }

    private void SeedInitialUsers(ModelBuilder modelBuilder)
    {
        // Mật khẩu tĩnh được hash tương ứng với "SuperStrong@Password123" bằng bộ mã hóa PasswordHasher mới
        string hashedPassword = "oIYBABAAAAABAgMEBQYHCAkKCwwNDg8QSqv7jmhvenP7tL1meD6gdhri+A1tWUyVSyYM9rejQY0=";

        var adminId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
        var salesId = Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e");
        var warehouseId = Guid.Parse("c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                Username = "admin",
                PasswordHash = hashedPassword,
                FullName = "System Administrator",
                Email = "admin@company.com",
                Phone = "0123456789",
                Role = "Admin",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = salesId,
                Username = "sales01",
                PasswordHash = hashedPassword,
                FullName = "Sales Specialist",
                Email = "sales01@company.com",
                Phone = "0987654321",
                Role = "Sales",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = warehouseId,
                Username = "warehouse01",
                PasswordHash = hashedPassword,
                FullName = "Warehouse Keeper",
                Email = "warehouse01@company.com",
                Phone = "0555555555",
                Role = "Warehouse",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
