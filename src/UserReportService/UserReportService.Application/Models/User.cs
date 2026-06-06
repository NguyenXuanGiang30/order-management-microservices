namespace UserReportService.Application.Models;

/// <summary>
/// Người dùng hệ thống - Xác thực, phân quyền (Admin, Sales, Warehouse).
/// </summary>
public class User : AuditableEntity
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!; // Admin, Sales, Warehouse
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
