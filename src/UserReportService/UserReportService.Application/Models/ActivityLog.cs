namespace UserReportService.Application.Models;

/// <summary>
/// Nhật ký hoạt động - Ghi lại mọi thao tác quan trọng phục vụ audit trail và bảo mật.
/// </summary>
public class ActivityLog : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public string Action { get; set; } = null!; // Login, Logout, CreateOrder, ImportGoods...
    public string EntityType { get; set; } = null!; // Order, Product, GoodsReceipt...
    public string? EntityId { get; set; }
    public string ServiceName { get; set; } = "UserReportService";
    public string Severity { get; set; } = "Info";
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
