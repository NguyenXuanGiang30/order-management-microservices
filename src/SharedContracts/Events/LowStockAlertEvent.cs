namespace SharedContracts.Events;

/// <summary>
/// Sự kiện phát ra khi số lượng tồn kho của sản phẩm giảm xuống dưới hạn mức tối thiểu (MinThreshold).
/// </summary>
public record LowStockAlertEvent
{
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public int QuantityOnHand { get; init; }
    public int MinThreshold { get; init; }
    public string AlertMessage { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
