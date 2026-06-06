namespace SharedContracts.Events;

/// <summary>
/// Event phát ra khi tồn kho được cập nhật (sau khi trừ kho cho đơn hàng).
/// </summary>
public record InventoryUpdatedEvent
{
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = null!;
    public int QuantityChange { get; init; }
    public int NewQuantityOnHand { get; init; }
    public string Reason { get; init; } = null!;
    public DateTime UpdatedAt { get; init; }
}
