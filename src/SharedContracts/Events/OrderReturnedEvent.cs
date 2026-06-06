namespace SharedContracts.Events;

public class OrderReturnedEvent
{
    public Guid ReturnOrderId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public DateTime ReturnedAt { get; set; }
    public List<OrderReturnedItem> Items { get; set; } = new();
}

public class OrderReturnedItem
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int ReturnQuantity { get; set; }
    public decimal RefundAmount { get; set; }
}
