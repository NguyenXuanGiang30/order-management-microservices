namespace ProductInventoryService.Application.Models;

/// <summary>
/// Lớp cơ sở cho các thực thể cần audit trail (theo dõi lịch sử tạo/sửa).
/// Hỗ trợ soft-delete thông qua trường IsActive.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
