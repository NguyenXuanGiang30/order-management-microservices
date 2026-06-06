using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Features.Suppliers;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;
using SharedContracts.Events;

namespace OrderSalesService.Infrastructure.Consumers;

public class GoodsReceiptConfirmedConsumer : IConsumer<GoodsReceiptConfirmedEvent>
{
    private const string ConsumerName = nameof(GoodsReceiptConfirmedConsumer);
    private readonly IOrderSalesDbContext _ctx;

    public GoodsReceiptConfirmedConsumer(IOrderSalesDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task Consume(ConsumeContext<GoodsReceiptConfirmedEvent> context)
    {
        var message = context.Message;
        var eventId = string.IsNullOrWhiteSpace(message.EventId)
            ? $"goods-receipt-confirmed:{message.ReceiptId}"
            : message.EventId;

        var alreadyProcessed = await _ctx.ProcessedEvents.AnyAsync(
            e => e.EventId == eventId && e.ConsumerName == ConsumerName,
            context.CancellationToken);

        if (alreadyProcessed) return;

        var supplier = await _ctx.Suppliers.FirstOrDefaultAsync(s => s.Id == message.SupplierId, context.CancellationToken);
        if (supplier == null)
        {
            supplier = new Supplier
            {
                Id = message.SupplierId,
                Code = $"NCC-{message.SupplierId.ToString("N")[..8]}",
                Name = message.SupplierName,
                CreatedBy = Guid.Empty
            };
            _ctx.Suppliers.Add(supplier);
        }

        SupplierDebtPolicy.ApplyConfirmedGoodsReceipt(supplier, message.TotalAmount);
        _ctx.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = ConsumerName,
            ProcessedAt = DateTime.UtcNow
        });

        await _ctx.SaveChangesAsync(context.CancellationToken);
    }
}
