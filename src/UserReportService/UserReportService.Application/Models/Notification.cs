namespace UserReportService.Application.Models;

public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Severity { get; set; } = "Info";
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
