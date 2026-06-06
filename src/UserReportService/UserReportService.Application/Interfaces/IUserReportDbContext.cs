using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Models;

namespace UserReportService.Application.Interfaces;

public interface IUserReportDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
    DbSet<ActivityLog> ActivityLogs { get; set; }
    DbSet<DailySalesSummary> DailySalesSummaries { get; set; }
    DbSet<MonthlySalesSummary> MonthlySalesSummaries { get; set; }
    DbSet<DailyProfitSummary> DailyProfitSummaries { get; set; }
    DbSet<MonthlyProfitSummary> MonthlyProfitSummaries { get; set; }
    DbSet<ProductProfitSummary> ProductProfitSummaries { get; set; }
    DbSet<TopProductSummary> TopProductSummaries { get; set; }
    DbSet<TopCustomerSummary> TopCustomerSummaries { get; set; }
    DbSet<Permission> Permissions { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<BackupRecord> BackupRecords { get; set; }
    DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
