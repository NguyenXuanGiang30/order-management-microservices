namespace UserReportService.Application.Models;

/// <summary>
/// Tổng hợp doanh thu theo tháng - CQRS Read Model cho Dashboard.
/// </summary>
public class MonthlySalesSummary : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public int TotalItemsSold { get; set; }
    public int TotalNewCustomers { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
