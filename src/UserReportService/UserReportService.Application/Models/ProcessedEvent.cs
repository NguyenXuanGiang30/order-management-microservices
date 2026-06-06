namespace UserReportService.Application.Models;

public class ProcessedEvent
{
    public string EventId { get; set; } = null!;
    public string ConsumerName { get; set; } = null!;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
