namespace UserReportService.Application.Models;

public class MonthlyProfitSummary : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public int TotalOrders { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
