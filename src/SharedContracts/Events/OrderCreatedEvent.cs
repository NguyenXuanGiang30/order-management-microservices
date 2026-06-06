namespace SharedContracts.Events;

/// <summary>
/// Event phát ra khi một đơn hàng mới được tạo thành công.
/// ProductInventoryService lắng nghe để trừ kho.
/// UserReportService lắng nghe để cập nhật báo cáo doanh thu.
/// </summary>
public record OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public string OrderCode { get; init; } = null!;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = null!;
    public decimal FinalAmount { get; init; }
    public DateTime OrderDate { get; init; }
    public List<OrderCreatedItem> Items { get; init; } = new();
}

public record OrderCreatedItem
{
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal CostPrice { get; init; }
    public decimal CostTotal { get; init; }
    public decimal SubTotal { get; init; }
}
