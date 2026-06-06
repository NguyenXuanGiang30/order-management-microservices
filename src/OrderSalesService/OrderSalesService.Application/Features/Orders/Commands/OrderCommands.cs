using MediatR;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;
using SharedContracts.Events;

namespace OrderSalesService.Application.Features.Orders.Commands;

// ======================== Update Order (khi Pending) ========================
public record UpdateOrderCommand(Guid Id, decimal? DiscountPercent, string? PaymentMethod, string? Note) : IRequest<bool>;
public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, bool>
{
    private readonly IOrderSalesDbContext _ctx;
    public UpdateOrderCommandHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<bool> Handle(UpdateOrderCommand req, CancellationToken ct)
    {
        var order = await _ctx.Orders.FirstOrDefaultAsync(o => o.Id == req.Id && o.Status == "Pending", ct);
        if (order == null) return false;
        if (req.DiscountPercent.HasValue)
        {
            order.DiscountPercent = req.DiscountPercent.Value;
            order.DiscountAmount = order.SubTotal * req.DiscountPercent.Value / 100;
            order.FinalAmount = order.SubTotal - order.DiscountAmount;
            order.DebtAmount = order.FinalAmount - order.PaidAmount;
        }
        if (req.PaymentMethod != null) order.PaymentMethod = req.PaymentMethod;
        if (req.Note != null) order.Note = req.Note;
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}

// ======================== Cancel Order ========================
public record CancelOrderCommand(Guid Id, Guid CancelledBy, string CancelledByName, string? Reason) : IRequest<bool>;
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderSalesDbContext _ctx;
    public CancelOrderCommandHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }
    public async Task<bool> Handle(CancelOrderCommand req, CancellationToken ct)
    {
        var order = await _ctx.Orders.FirstOrDefaultAsync(o => o.Id == req.Id && o.Status != "Cancelled", ct);
        if (order == null) return false;
        var oldStatus = order.Status;
        order.Status = "Cancelled";

        // Cập nhật công nợ khách hàng
        var customer = await _ctx.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId, ct);
        if (customer != null) customer.DebtAmount = Math.Max(0, customer.DebtAmount - order.DebtAmount);
        order.DebtAmount = 0;

        _ctx.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id, OldStatus = oldStatus, NewStatus = "Cancelled",
            Note = req.Reason ?? "Hủy đơn hàng.", ChangedBy = req.CancelledBy, ChangedByName = req.CancelledByName
        });
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}

// ======================== Return Order ========================
public record CreateReturnOrderCommand(Guid OrderId, string ReturnReason, Guid CreatedBy, List<ReturnItemDto> Items) : IRequest<ReturnOrderResultDto>;
public record ReturnItemDto(Guid OrderDetailId, int ReturnQuantity);
public record ReturnOrderResultDto(Guid ReturnOrderId, string ReturnCode, decimal TotalRefundAmount);

public class CreateReturnOrderCommandHandler : IRequestHandler<CreateReturnOrderCommand, ReturnOrderResultDto>
{
    private readonly IOrderSalesDbContext _ctx;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateReturnOrderCommandHandler(IOrderSalesDbContext ctx, IPublishEndpoint publishEndpoint)
    {
        _ctx = ctx;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ReturnOrderResultDto> Handle(CreateReturnOrderCommand req, CancellationToken ct)
    {
        var order = await _ctx.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.ReturnOrders)
                .ThenInclude(r => r.ReturnOrderDetails)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Không thể trả hàng cho đơn đã hủy.");
        }

        var oldStatus = order.Status;
        var returnPlan = ReturnOrderPolicy.ValidateAndBuild(order, req.Items);
        var returnCode = $"TH-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        var returnDetails = returnPlan.Items.Select(item => new ReturnOrderDetail
        {
            OrderDetailId = item.OrderDetailId,
            ProductId = item.ProductId,
            ReturnQuantity = item.ReturnQuantity,
            RefundAmount = item.RefundAmount
        }).ToList();

        var returnOrder = new ReturnOrder
        {
            OrderId = req.OrderId, ReturnCode = returnCode, TotalRefundAmount = returnPlan.TotalRefundAmount,
            ReturnReason = req.ReturnReason, Status = "Completed", CreatedBy = req.CreatedBy,
            ReturnOrderDetails = returnDetails
        };
        _ctx.ReturnOrders.Add(returnOrder);

        order.Status = ReturnOrderPolicy.IsFullyReturned(order, returnPlan) ? "Returned" : "PartialReturned";

        _ctx.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id, OldStatus = oldStatus, NewStatus = order.Status,
            Note = $"Trả hàng: {req.ReturnReason}", ChangedBy = req.CreatedBy, ChangedByName = "System"
        });

        var refundLeft = returnPlan.TotalRefundAmount;
        var debtReduction = Math.Min(order.DebtAmount, refundLeft);
        order.DebtAmount -= debtReduction;
        refundLeft -= debtReduction;
        if (refundLeft > 0)
        {
            order.PaidAmount = Math.Max(0, order.PaidAmount - refundLeft);
        }

        var customer = await _ctx.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId, ct);
        if (customer != null)
        {
            customer.DebtAmount = Math.Max(0, customer.DebtAmount - debtReduction);
            customer.TotalPurchased = Math.Max(0, customer.TotalPurchased - returnPlan.TotalRefundAmount);
        }

        await _ctx.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new OrderReturnedEvent
        {
            ReturnOrderId = returnOrder.Id,
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            ReturnedAt = DateTime.UtcNow,
            Items = returnPlan.Items.Select(item => new OrderReturnedItem
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                ReturnQuantity = item.ReturnQuantity,
                RefundAmount = item.RefundAmount
            }).ToList()
        }, ct);
        await _publishEndpoint.Publish(new AuditLoggedEvent
        {
            UserId = req.CreatedBy,
            ServiceName = "OrderSalesService",
            Action = "OrderReturned",
            EntityType = "ReturnOrder",
            EntityId = returnOrder.Id.ToString(),
            Severity = "Info",
            Description = $"Return {returnOrder.ReturnCode} completed for order {order.OrderCode}.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new ReturnOrderResultDto(returnOrder.Id, returnCode, returnPlan.TotalRefundAmount);
    }
}
