using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Payments;

public record PaymentDto(Guid Id, Guid OrderId, Guid CustomerId, decimal Amount, string? PaymentMethod, string? Note, string ReceivedByName, DateTime PaymentDate);

// ======================== Create Payment ========================
public record CreatePaymentCommand(Guid OrderId, Guid CustomerId, decimal Amount, string? PaymentMethod, string? Note, Guid ReceivedBy, string ReceivedByName) : IRequest<PaymentDto>;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    private readonly IOrderSalesDbContext _ctx;
    public CreatePaymentCommandHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }

    public async Task<PaymentDto> Handle(CreatePaymentCommand req, CancellationToken ct)
    {
        var order = await _ctx.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Đơn hàng không tồn tại.");
        var oldStatus = order.Status;

        var payment = new PaymentTransaction
        {
            OrderId = req.OrderId, CustomerId = req.CustomerId, Amount = req.Amount,
            PaymentMethod = req.PaymentMethod, Note = req.Note, ReceivedBy = req.ReceivedBy,
            ReceivedByName = req.ReceivedByName, PaymentDate = DateTime.UtcNow
        };
        _ctx.PaymentTransactions.Add(payment);

        // Cập nhật PaidAmount + DebtAmount của đơn hàng
        order.PaidAmount += req.Amount;
        order.DebtAmount = order.FinalAmount - order.PaidAmount;
        if (order.DebtAmount <= 0) { order.Status = "Paid"; order.DebtAmount = 0; }
        else if (order.PaidAmount > 0) { order.Status = "PartialPaid"; }

        // Log status nếu cần
        _ctx.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id, OldStatus = oldStatus, NewStatus = order.Status,
            Note = $"Thanh toán {req.Amount:N0} VND", ChangedBy = req.ReceivedBy, ChangedByName = req.ReceivedByName
        });

        // Cập nhật công nợ khách hàng
        var customer = await _ctx.Customers.FirstOrDefaultAsync(c => c.Id == req.CustomerId, ct);
        if (customer != null) customer.DebtAmount = Math.Max(0, customer.DebtAmount - req.Amount);

        await _ctx.SaveChangesAsync(ct);
        return new PaymentDto(payment.Id, payment.OrderId, payment.CustomerId, payment.Amount, payment.PaymentMethod, payment.Note, payment.ReceivedByName, payment.PaymentDate);
    }
}

// ======================== Get Payments ========================
public record GetPaymentsQuery(Guid? CustomerId, Guid? OrderId, DateTime? From, int PageNumber = 1, int PageSize = 20) : IRequest<PagedResponse<PaymentDto>>;

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, PagedResponse<PaymentDto>>
{
    private readonly IOrderSalesDbContext _ctx;
    public GetPaymentsQueryHandler(IOrderSalesDbContext ctx) { _ctx = ctx; }

    public async Task<PagedResponse<PaymentDto>> Handle(GetPaymentsQuery req, CancellationToken ct)
    {
        var q = _ctx.PaymentTransactions.AsNoTracking().AsQueryable();
        if (req.CustomerId.HasValue) q = q.Where(p => p.CustomerId == req.CustomerId.Value);
        if (req.OrderId.HasValue) q = q.Where(p => p.OrderId == req.OrderId.Value);
        if (req.From.HasValue) q = q.Where(p => p.PaymentDate >= req.From.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.PaymentDate).Skip((req.PageNumber - 1) * req.PageSize).Take(req.PageSize)
            .Select(p => new PaymentDto(p.Id, p.OrderId, p.CustomerId, p.Amount, p.PaymentMethod, p.Note, p.ReceivedByName, p.PaymentDate)).ToListAsync(ct);

        return new PagedResponse<PaymentDto> { Items = items, PageNumber = req.PageNumber, PageSize = req.PageSize, TotalCount = total };
    }
}
