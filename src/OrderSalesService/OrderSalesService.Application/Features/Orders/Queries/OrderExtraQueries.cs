using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Interfaces;

namespace OrderSalesService.Application.Features.Orders.Queries;

// ======================== Status History ========================
public record StatusHistoryDto(Guid Id, Guid OrderId, string? OldStatus, string NewStatus, string? Note, Guid ChangedBy, string ChangedByName, DateTime CreatedAt);
public record GetOrderStatusHistoryQuery(Guid OrderId) : IRequest<List<StatusHistoryDto>>;

public class GetOrderStatusHistoryQueryHandler : IRequestHandler<GetOrderStatusHistoryQuery, List<StatusHistoryDto>>
{
    private readonly IOrderSalesDbContext _ctx;
    public GetOrderStatusHistoryQueryHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<List<StatusHistoryDto>> Handle(GetOrderStatusHistoryQuery req, CancellationToken ct)
    {
        return await _ctx.OrderStatusHistories.AsNoTracking().Where(h => h.OrderId == req.OrderId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new StatusHistoryDto(h.Id, h.OrderId, h.OldStatus, h.NewStatus, h.Note, h.ChangedBy, h.ChangedByName, h.CreatedAt))
            .ToListAsync(ct);
    }
}

// ======================== Invoice ========================
public record InvoiceDto(string OrderCode, string CustomerName, DateTime OrderDate, string? PaymentMethod,
    decimal SubTotal, decimal DiscountPercent, decimal DiscountAmount, string? PromotionCode, string? PromotionName,
    decimal PromotionDiscountAmount, decimal FinalAmount, decimal PaidAmount, decimal DebtAmount,
    List<InvoiceItemDto> Items);
public record InvoiceItemDto(string ProductCode, string ProductName, string UnitName, decimal UnitPrice, int Quantity, decimal SubTotal);
public record GetOrderInvoiceQuery(Guid OrderId) : IRequest<InvoiceDto?>;

public class GetOrderInvoiceQueryHandler : IRequestHandler<GetOrderInvoiceQuery, InvoiceDto?>
{
    private readonly IOrderSalesDbContext _ctx;
    public GetOrderInvoiceQueryHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<InvoiceDto?> Handle(GetOrderInvoiceQuery req, CancellationToken ct)
    {
        var order = await _ctx.Orders.Include(o => o.OrderDetails).AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order == null) return null;

        return new InvoiceDto(order.OrderCode, order.CustomerName, order.OrderDate, order.PaymentMethod,
            order.SubTotal, order.DiscountPercent, order.DiscountAmount, order.PromotionCode, order.PromotionName,
            order.PromotionDiscountAmount, order.FinalAmount, order.PaidAmount, order.DebtAmount,
            order.OrderDetails.Select(d => new InvoiceItemDto(d.ProductCode, d.ProductName, d.UnitName, d.UnitPrice, d.Quantity, d.SubTotal)).ToList());
    }
}
