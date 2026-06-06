namespace ProductInventoryService.Application.Models;

/// <summary>
/// Bảng đảm bảo Idempotency - Lưu vết các event đã được xử lý thành công
/// để chống trùng lặp message khi RabbitMQ gửi lại.
/// </summary>
public class ProcessedEvent
{
    public string EventId { get; set; } = null!;
    public string ConsumerName { get; set; } = null!;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
