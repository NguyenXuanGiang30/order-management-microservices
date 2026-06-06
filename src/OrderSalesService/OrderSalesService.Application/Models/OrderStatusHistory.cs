namespace OrderSalesService.Application.Models;

/// <summary>
/// Lịch sử trạng thái đơn hàng - Lưu vết mọi thay đổi trạng thái
/// (ai duyệt, ai hủy, thời gian nào). Bắt buộc cho audit trail.
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public string? Note { get; set; }
    public Guid ChangedBy { get; set; } // Ref ID -> UserReportDB.User
    public string ChangedByName { get; set; } = null!; // Snapshot
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
