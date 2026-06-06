using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;
using SharedContracts.Events;

namespace ProductInventoryService.Infrastructure.Consumers;

public class OrderReturnedConsumer : IConsumer<OrderReturnedEvent>
{
    private readonly IProductInventoryDbContext _context;

    public OrderReturnedConsumer(IProductInventoryDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<OrderReturnedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId?.ToString() ?? message.ReturnOrderId.ToString();

        var isProcessed = await _context.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(OrderReturnedConsumer));

        if (isProcessed)
        {
            return;
        }

        foreach (var item in message.Items)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

            if (inventory == null)
            {
                continue;
            }

            inventory.QuantityOnHand += item.ReturnQuantity;
            inventory.LastUpdated = DateTime.UtcNow;

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = item.ProductId,
                TransactionType = "Return",
                QuantityChange = item.ReturnQuantity,
                QuantityAfter = inventory.QuantityOnHand,
                ReferenceType = "ReturnOrder",
                ReferenceId = message.ReturnOrderId,
                Note = $"Nhập lại hàng trả từ đơn {message.OrderCode}",
                CreatedAt = DateTime.UtcNow
            });

            await context.Publish(new InventoryUpdatedEvent
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                QuantityChange = item.ReturnQuantity,
                NewQuantityOnHand = inventory.QuantityOnHand,
                Reason = $"Return (Order {message.OrderCode})",
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = nameof(OrderReturnedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
