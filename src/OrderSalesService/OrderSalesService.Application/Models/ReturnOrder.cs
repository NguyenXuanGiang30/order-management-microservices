namespace OrderSalesService.Application.Models;

/// <summary>
/// Phiếu trả hàng - Xử lý quy trình trả hàng (hoàn tiền, hoàn kho).
/// Liên kết trực tiếp tới OrderId gốc.
/// </summary>
public class ReturnOrder : AuditableEntity
{
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    public string ReturnCode { get; set; } = null!;
    public decimal TotalRefundAmount { get; set; }
    public string? ReturnReason { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Rejected
    public Guid CreatedBy { get; set; } // Ref ID -> UserReportDB.User

    // Navigation
    public virtual ICollection<ReturnOrderDetail> ReturnOrderDetails { get; set; } = new List<ReturnOrderDetail>();
}
