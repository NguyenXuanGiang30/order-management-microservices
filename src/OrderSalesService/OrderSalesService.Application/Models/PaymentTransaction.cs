namespace OrderSalesService.Application.Models;

/// <summary>
/// Giao dịch thanh toán - Ghi nhận mỗi lần khách thanh toán (hỗ trợ thanh toán từng phần).
/// </summary>
public class PaymentTransaction : BaseEntity
{
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public Guid ReceivedBy { get; set; } // Ref ID -> UserReportDB.User
    public string ReceivedByName { get; set; } = null!; // Snapshot
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
