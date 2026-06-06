namespace UserReportService.Application.Models;

/// <summary>
/// Refresh Token - Hoàn chỉnh luồng JWT Auth.
/// Hỗ trợ token rotation và revoke token.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByIp { get; set; } = null!;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActiveToken => !IsExpired && !IsRevoked;
}
