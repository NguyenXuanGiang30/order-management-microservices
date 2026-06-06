using System.Collections.Concurrent;

namespace OrderSalesService.Application.Common;

/// <summary>
/// Cache lưu trữ tồn kho đệm tại OrderSalesService (Đồng bộ qua sự kiện stock.updated từ MassTransit).
/// Giúp kiểm tra nhanh hàng tồn kho trước khi đặt đơn mà không cần truy vấn chéo DB.
/// </summary>
public interface IStockCache
{
    void UpdateStock(Guid productId, int quantity);
    bool IsInStock(Guid productId, int requestedQuantity);
    int GetStock(Guid productId);
}

public class StockCache : IStockCache
{
    private readonly ConcurrentDictionary<Guid, int> _stock = new();

    public void UpdateStock(Guid productId, int quantity)
    {
        _stock[productId] = quantity;
    }

    public bool IsInStock(Guid productId, int requestedQuantity)
    {
        // Nếu chưa có trong cache đệm, mặc định coi như còn hàng (hoặc có thể gọi API kiểm tra)
        // Đây là cách tiếp cận Eventual Consistency tối ưu cho Microservices
        if (!_stock.TryGetValue(productId, out var qty)) return true; 
        return qty >= requestedQuantity;
    }

    public int GetStock(Guid productId)
    {
        return _stock.TryGetValue(productId, out var qty) ? qty : -1;
    }
}
