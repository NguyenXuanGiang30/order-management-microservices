namespace UserReportService.Application.Models;

/// <summary>
/// Thống kê khách hàng chi tiêu nhiều nhất theo tháng - CQRS Read Model.
/// </summary>
public class TopCustomerSummary : BaseEntity
{
    public Guid CustomerId { get; set; } // Ref ID -> OrderSalesDB.Customer
    public string CustomerName { get; set; } = null!; // Snapshot
    public string? CustomerPhone { get; set; } // Snapshot
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
