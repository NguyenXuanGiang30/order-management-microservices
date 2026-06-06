namespace UserReportService.Application.Models;

public class ProductProfitSummary : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
