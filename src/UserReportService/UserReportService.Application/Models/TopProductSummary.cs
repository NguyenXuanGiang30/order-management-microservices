namespace UserReportService.Application.Models;

/// <summary>
/// Thống kê sản phẩm bán chạy theo tháng - CQRS Read Model.
/// </summary>
public class TopProductSummary : BaseEntity
{
    public Guid ProductId { get; set; } // Ref ID -> ProductInventoryDB.Product
    public string ProductCode { get; set; } = null!; // Snapshot
    public string ProductName { get; set; } = null!; // Snapshot
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
