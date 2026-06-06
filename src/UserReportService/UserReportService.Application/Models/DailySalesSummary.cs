namespace UserReportService.Application.Models;

/// <summary>
/// Tổng hợp doanh thu theo ngày - CQRS Read Model cho Dashboard.
/// </summary>
public class DailySalesSummary : BaseEntity
{
    public DateTime ReportDate { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public int TotalItemsSold { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
