namespace ProductInventoryService.Application.Models;

/// <summary>
/// Lớp cơ sở cho tất cả các thực thể trong hệ thống.
/// Sử dụng GUID làm khóa chính để tránh conflict khi merge dữ liệu.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
