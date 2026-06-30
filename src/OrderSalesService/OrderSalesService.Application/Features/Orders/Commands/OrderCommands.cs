using MediatR;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;
using OrderSalesService.Application.Common;
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

// ======================== Confirm Draft / Quotation Order ========================
public record ConfirmOrderCommand(Guid Id, Guid ConfirmedBy, string ConfirmedByName, string? PaymentMethod, decimal PaidAmount, List<CreateOrder.CreatePaymentTransactionDto>? Payments = null) : IRequest<bool>;

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, bool>
{
    private readonly IOrderSalesDbContext _ctx;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IStockCache _stockCache;

    public ConfirmOrderCommandHandler(IOrderSalesDbContext ctx, IPublishEndpoint publishEndpoint, IStockCache stockCache)
    {
        _ctx = ctx;
        _publishEndpoint = publishEndpoint;
        _stockCache = stockCache;
    }

    public async Task<bool> Handle(ConfirmOrderCommand req, CancellationToken ct)
    {
        var order = await _ctx.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (order == null) return false;

        // Chỉ cho phép chuyển đổi từ Draft hoặc Quotation
        if (order.Status != "Draft" && order.Status != "Quotation")
        {
            throw new InvalidOperationException("Chỉ có thể xác nhận đơn hàng nháp hoặc báo giá.");
        }

        // Kiểm tra tồn kho trước khi xác nhận
        foreach (var item in order.OrderDetails)
        {
            if (!_stockCache.IsInStock(item.ProductId, item.Quantity))
            {
                var currentStock = _stockCache.GetStock(item.ProductId);
                var currentStockStr = currentStock >= 0 ? currentStock.ToString() : "0 (Hết hàng)";
                throw new FluentValidation.ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(nameof(order.OrderDetails), 
                        $"Sản phẩm '{item.ProductName}' không đủ tồn kho! Số lượng yêu cầu: {item.Quantity}, Tồn kho hiện hành: {currentStockStr}")
                });
            }
        }

        var oldStatus = order.Status;
        decimal totalPaid = 0;
        var paymentTransactions = new List<PaymentTransaction>();

        if (req.Payments != null && req.Payments.Any())
        {
            foreach (var p in req.Payments)
            {
                if (p.Amount > 0)
                {
                    totalPaid += p.Amount;
                    paymentTransactions.Add(new PaymentTransaction
                    {
                        OrderId = order.Id,
                        CustomerId = order.CustomerId,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        Note = p.Note,
                        ReceivedBy = req.ConfirmedBy,
                        ReceivedByName = req.ConfirmedByName,
                        PaymentDate = DateTime.UtcNow
                    });
                }
            }
            order.PaymentMethod = string.Join(", ", req.Payments.Select(p => p.PaymentMethod).Distinct());
        }
        else
        {
            totalPaid = req.PaidAmount;
            if (!string.IsNullOrEmpty(req.PaymentMethod))
            {
                order.PaymentMethod = req.PaymentMethod;
                if (totalPaid > 0 && !string.Equals(req.PaymentMethod, "Ghi nợ", StringComparison.OrdinalIgnoreCase))
                {
                    paymentTransactions.Add(new PaymentTransaction
                    {
                        OrderId = order.Id,
                        CustomerId = order.CustomerId,
                        Amount = totalPaid,
                        PaymentMethod = req.PaymentMethod,
                        Note = "Thanh toán khi xác nhận đơn hàng",
                        ReceivedBy = req.ConfirmedBy,
                        ReceivedByName = req.ConfirmedByName,
                        PaymentDate = DateTime.UtcNow
                    });
                }
            }
        }

        order.PaidAmount = totalPaid;
        order.DebtAmount = Math.Max(0, order.FinalAmount - totalPaid);

        // Xác định trạng thái mới dựa trên thanh toán
        if (order.DebtAmount <= 0)
        {
            order.Status = "Paid";
            order.DebtAmount = 0;
        }
        else if (order.PaidAmount > 0)
        {
            order.Status = "PartialPaid";
        }
        else
        {
            order.Status = "Pending";
        }

        foreach (var pt in paymentTransactions)
        {
            _ctx.PaymentTransactions.Add(pt);
        }

        // Cập nhật công nợ và tổng mua hàng của khách hàng
        var customer = await _ctx.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId, ct);
        if (customer != null)
        {
            customer.TotalPurchased += order.FinalAmount;
            customer.DebtAmount += order.DebtAmount;
        }

        _ctx.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = order.Status,
            Note = "Xác nhận đơn hàng từ bản nháp/báo giá.",
            ChangedBy = req.ConfirmedBy,
            ChangedByName = req.ConfirmedByName,
            CreatedAt = DateTime.UtcNow
        });

        await _ctx.SaveChangesAsync(ct);

        // Publish OrderCreatedEvent để trừ kho và làm báo cáo
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            FinalAmount = order.FinalAmount,
            OrderDate = order.OrderDate,
            Items = order.OrderDetails.Select(item => new OrderCreatedItem
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CostPrice = item.CostPrice,
                CostTotal = item.CostTotal,
                SubTotal = item.SubTotal
            }).ToList()
        };
        await _publishEndpoint.Publish(orderCreatedEvent, ct);

        await _publishEndpoint.Publish(new AuditLoggedEvent
        {
            UserId = req.ConfirmedBy,
            UserName = req.ConfirmedByName,
            ServiceName = "OrderSalesService",
            Action = "OrderConfirmed",
            EntityType = "Order",
            EntityId = order.Id.ToString(),
            Severity = "Info",
            Description = $"Order draft/quote {order.OrderCode} confirmed and status changed to {order.Status}.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        return true;
    }
}
