using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;
using SharedContracts.Events;

namespace ProductInventoryService.Infrastructure.Consumers;

/// <summary>
/// Lắng nghe sự kiện OrderCreatedEvent từ RabbitMQ.
/// Tự động trừ kho sản phẩm và ghi nhận lịch sử biến động kho.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IProductInventoryDbContext _context;

    public OrderCreatedConsumer(IProductInventoryDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId?.ToString() ?? message.OrderId.ToString();

        // 1. Kiểm tra Idempotency để tránh xử lý trùng lặp
        var isProcessed = await _context.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(OrderCreatedConsumer));

        if (isProcessed)
        {
            return; // Đã xử lý rồi, bỏ qua
        }

        // 2. Thực hiện trừ kho từng sản phẩm trong đơn hàng
        foreach (var item in message.Items)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

            if (inventory != null)
            {
                var oldQty = inventory.QuantityOnHand;
                inventory.QuantityOnHand -= item.Quantity;

                // Tạo Transaction Log biến động kho
                var transaction = new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    TransactionType = "Export",
                    QuantityChange = -item.Quantity,
                    QuantityAfter = inventory.QuantityOnHand,
                    ReferenceType = "Order",
                    ReferenceId = message.OrderId,
                    Note = $"Xuất kho bán lẻ theo đơn {message.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.InventoryTransactions.Add(transaction);

                // Publish sự kiện cập nhật tồn kho để các phân hệ khác (nếu cần) đồng bộ
                var stockUpdatedEvent = new InventoryUpdatedEvent
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    QuantityChange = -item.Quantity,
                    NewQuantityOnHand = inventory.QuantityOnHand,
                    Reason = $"Export (Order {message.OrderCode})",
                    UpdatedAt = DateTime.UtcNow
                };

                await context.Publish(stockUpdatedEvent);

                // Kiểm tra xem sản phẩm có xuống dưới hạn mức tồn kho tối thiểu không
                if (inventory.QuantityOnHand < inventory.MinThreshold)
                {
                    var lowStockEvent = new LowStockAlertEvent
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        QuantityOnHand = inventory.QuantityOnHand,
                        MinThreshold = inventory.MinThreshold,
                        AlertMessage = $"CẢNH BÁO: Sản phẩm {item.ProductName} ({item.ProductCode}) có tồn kho là {inventory.QuantityOnHand}, dưới mức tối thiểu an toàn là {inventory.MinThreshold}!",
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Publish(lowStockEvent);
                }
            }
        }

        // 3. Đánh dấu sự kiện đã được xử lý thành công
        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = nameof(OrderCreatedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
