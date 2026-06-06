namespace OrderSalesService.Application.Models;

/// <summary>
/// Bảng đảm bảo Idempotency - Chống trùng lặp message từ RabbitMQ.
/// </summary>
public class ProcessedEvent
{
    public string EventId { get; set; } = null!;
    public string ConsumerName { get; set; } = null!;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
