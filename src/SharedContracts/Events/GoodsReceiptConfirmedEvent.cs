namespace SharedContracts.Events;

public record GoodsReceiptConfirmedEvent
{
    public string EventId { get; init; } = null!;
    public Guid ReceiptId { get; init; }
    public string ReceiptCode { get; init; } = null!;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public DateTime ConfirmedAt { get; init; }
}
