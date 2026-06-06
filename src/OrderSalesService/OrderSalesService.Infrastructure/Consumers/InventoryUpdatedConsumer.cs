using MassTransit;
using OrderSalesService.Application.Common;
using SharedContracts.Events;

namespace OrderSalesService.Infrastructure.Consumers;

/// <summary>
/// Lắng nghe sự kiện InventoryUpdatedEvent (stock.updated) từ RabbitMQ.
/// Cập nhật cache đệm tồn kho cục bộ của OrderSalesService để phục vụ kiểm tra nhanh khi tạo đơn hàng.
/// </summary>
public class InventoryUpdatedConsumer : IConsumer<InventoryUpdatedEvent>
{
    private readonly IStockCache _stockCache;

    public InventoryUpdatedConsumer(IStockCache stockCache)
    {
        _stockCache = stockCache;
    }

    public Task Consume(ConsumeContext<InventoryUpdatedEvent> context)
    {
        var message = context.Message;
        
        // Cập nhật tồn kho mới vào Cache cục bộ
        _stockCache.UpdateStock(message.ProductId, message.NewQuantityOnHand);
        
        return Task.CompletedTask;
    }
}
