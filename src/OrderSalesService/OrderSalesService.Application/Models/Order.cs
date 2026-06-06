namespace OrderSalesService.Application.Models;

/// <summary>
/// Đơn hàng bán - Lưu kèm snapshot tên khách, tên người tạo.
/// Hỗ trợ quy trình: Pending → Confirmed → Paid/PartialPaid → Returned/Cancelled.
/// </summary>
public class Order : AuditableEntity
{
    public string OrderCode { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;
    public string CustomerName { get; set; } = null!; // Snapshot
    public Guid CreatedBy { get; set; } // Ref ID -> UserReportDB.User
    public string CreatedByName { get; set; } = null!; // Snapshot
    public DateTime OrderDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? PromotionId { get; set; }
    public string? PromotionCode { get; set; }
    public string? PromotionName { get; set; }
    public decimal PromotionDiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Paid, PartialPaid, Returned, Cancelled
    public string? Note { get; set; }

    // Navigation
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();
    public virtual ICollection<ReturnOrder> ReturnOrders { get; set; } = new List<ReturnOrder>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
