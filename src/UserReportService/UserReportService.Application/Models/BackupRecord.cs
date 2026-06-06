namespace UserReportService.Application.Models;

public class BackupRecord : BaseEntity
{
    public string BackupId { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string Status { get; set; } = "Completed";
    public string CreatedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RestoredAt { get; set; }
    public string? Note { get; set; }
}
