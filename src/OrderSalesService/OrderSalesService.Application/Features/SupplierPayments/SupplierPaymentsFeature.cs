using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.SupplierPayments;

public record SupplierPaymentDto(
    Guid Id,
    string PaymentCode,
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    decimal Amount,
    string PaymentMethod,
    DateTime PaymentDate,
    string? Note,
    Guid CreatedBy,
    string CreatedByName
);

public record GetSupplierPaymentsQuery(string? Search, Guid? SupplierId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<SupplierPaymentDto>>;

public class GetSupplierPaymentsQueryHandler : IRequestHandler<GetSupplierPaymentsQuery, PagedResponse<SupplierPaymentDto>>
{
    private readonly IOrderSalesDbContext _ctx;

    public GetSupplierPaymentsQueryHandler(IOrderSalesDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResponse<SupplierPaymentDto>> Handle(GetSupplierPaymentsQuery req, CancellationToken ct)
    {
        var q = _ctx.SupplierPayments.Include(x => x.Supplier).AsNoTracking().AsQueryable();

        if (req.SupplierId.HasValue)
        {
            q = q.Where(x => x.SupplierId == req.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            q = q.Where(x => x.PaymentCode.ToLower().Contains(s) || x.Supplier.Name.ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.PaymentDate)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new SupplierPaymentDto(
                x.Id,
                x.PaymentCode,
                x.SupplierId,
                x.Supplier.Name,
                x.Supplier.Code,
                x.Amount,
                x.PaymentMethod,
                x.PaymentDate,
                x.Note,
                x.CreatedBy,
                x.CreatedByName
            ))
            .ToListAsync(ct);

        return new PagedResponse<SupplierPaymentDto>
        {
            Items = items,
            PageNumber = req.PageNumber,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}

public record CreateSupplierPaymentCommand(Guid SupplierId, decimal Amount, string PaymentMethod, string? Note, Guid CreatedBy, string CreatedByName)
    : IRequest<SupplierPaymentDto>;

public class CreateSupplierPaymentCommandHandler : IRequestHandler<CreateSupplierPaymentCommand, SupplierPaymentDto>
{
    private readonly IOrderSalesDbContext _ctx;

    public CreateSupplierPaymentCommandHandler(IOrderSalesDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<SupplierPaymentDto> Handle(CreateSupplierPaymentCommand req, CancellationToken ct)
    {
        var supplier = await _ctx.Suppliers.FirstOrDefaultAsync(x => x.Id == req.SupplierId, ct);
        if (supplier == null)
        {
            throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");
        }

        if (req.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");
        }

        // Apply reduction to Supplier Debt
        supplier.DebtAmount -= req.Amount;

        var payment = new SupplierPayment
        {
            PaymentCode = $"PCNCC-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SupplierId = req.SupplierId,
            Amount = req.Amount,
            PaymentMethod = req.PaymentMethod,
            PaymentDate = DateTime.UtcNow,
            Note = req.Note,
            CreatedBy = req.CreatedBy,
            CreatedByName = req.CreatedByName
        };

        _ctx.SupplierPayments.Add(payment);
        await _ctx.SaveChangesAsync(ct);

        return new SupplierPaymentDto(
            payment.Id,
            payment.PaymentCode,
            payment.SupplierId,
            supplier.Name,
            supplier.Code,
            payment.Amount,
            payment.PaymentMethod,
            payment.PaymentDate,
            payment.Note,
            payment.CreatedBy,
            payment.CreatedByName
        );
    }
}
