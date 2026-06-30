using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Shifts;

public record CashShiftClosing(decimal ExpectedCash, decimal Variance);

public static class CashShiftCalculator
{
    public static CashShiftClosing CalculateClosing(decimal openingCash, decimal cashPayments, decimal actualCash)
    {
        var expectedCash = openingCash + cashPayments;
        return new CashShiftClosing(expectedCash, actualCash - expectedCash);
    }
}

public record CashShiftDto(
    Guid Id,
    string ShiftCode,
    Guid CashierId,
    string CashierName,
    DateTime OpenedAt,
    decimal OpeningCash,
    DateTime? ClosedAt,
    decimal ExpectedCash,
    decimal? ActualCash,
    decimal Variance,
    string Status,
    string? Note);

public record OpenCashShiftCommand(Guid CashierId, string CashierName, decimal OpeningCash, string? Note)
    : IRequest<CashShiftDto>;

public record CloseCashShiftCommand(Guid ShiftId, Guid CashierId, decimal ActualCash, string? Note)
    : IRequest<CashShiftDto>;

public record GetCurrentCashShiftQuery(Guid CashierId) : IRequest<CashShiftDto?>;

public record GetCashShiftsQuery(Guid? CashierId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<CashShiftDto>>;

public class OpenCashShiftCommandHandler : IRequestHandler<OpenCashShiftCommand, CashShiftDto>
{
    private readonly IOrderSalesDbContext _context;

    public OpenCashShiftCommandHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<CashShiftDto> Handle(OpenCashShiftCommand request, CancellationToken cancellationToken)
    {
        var hasOpenShift = await _context.CashShifts
            .AnyAsync(s => s.CashierId == request.CashierId && s.Status == "Open", cancellationToken);

        if (hasOpenShift)
        {
            throw new InvalidOperationException("Thu ngân đang có ca bán hàng chưa đóng.");
        }

        var shift = new CashShift
        {
            ShiftCode = $"CA-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CashierId = request.CashierId,
            CashierName = request.CashierName,
            OpenedAt = DateTime.UtcNow,
            OpeningCash = request.OpeningCash,
            ExpectedCash = request.OpeningCash,
            Variance = 0,
            Status = "Open",
            Note = request.Note
        };

        _context.CashShifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);

        return CashShiftMapper.ToDto(shift);
    }
}

public class CloseCashShiftCommandHandler : IRequestHandler<CloseCashShiftCommand, CashShiftDto>
{
    private readonly IOrderSalesDbContext _context;

    public CloseCashShiftCommandHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<CashShiftDto> Handle(CloseCashShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await _context.CashShifts
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && s.CashierId == request.CashierId && s.Status == "Open", cancellationToken);

        if (shift == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ca bán hàng đang mở.");
        }

        var closedAt = DateTime.UtcNow;
        var cashPayments = await _context.PaymentTransactions
            .Where(p =>
                p.ReceivedBy == request.CashierId &&
                p.PaymentMethod == "Tiền mặt" &&
                p.PaymentDate >= shift.OpenedAt &&
                p.PaymentDate <= closedAt)
            .SumAsync(p => p.Amount, cancellationToken);

        var cashReceipts = await _context.CashTransactions
            .Where(c =>
                c.CreatedBy == request.CashierId &&
                c.Type == "Receipt" &&
                c.CreatedAt >= shift.OpenedAt &&
                c.CreatedAt <= closedAt)
            .SumAsync(c => c.Amount, cancellationToken);

        var cashPaymentsOut = await _context.CashTransactions
            .Where(c =>
                c.CreatedBy == request.CashierId &&
                c.Type == "Payment" &&
                c.CreatedAt >= shift.OpenedAt &&
                c.CreatedAt <= closedAt)
            .SumAsync(c => c.Amount, cancellationToken);

        var totalCashInflow = cashPayments + cashReceipts - cashPaymentsOut;
        var closing = CashShiftCalculator.CalculateClosing(shift.OpeningCash, totalCashInflow, request.ActualCash);

        shift.ClosedAt = closedAt;
        shift.ExpectedCash = closing.ExpectedCash;
        shift.ActualCash = request.ActualCash;
        shift.Variance = closing.Variance;
        shift.Status = "Closed";
        shift.Note = request.Note ?? shift.Note;

        await _context.SaveChangesAsync(cancellationToken);

        return CashShiftMapper.ToDto(shift);
    }
}

public class GetCurrentCashShiftQueryHandler : IRequestHandler<GetCurrentCashShiftQuery, CashShiftDto?>
{
    private readonly IOrderSalesDbContext _context;

    public GetCurrentCashShiftQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<CashShiftDto?> Handle(GetCurrentCashShiftQuery request, CancellationToken cancellationToken)
    {
        var shift = await _context.CashShifts
            .AsNoTracking()
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(s => s.CashierId == request.CashierId && s.Status == "Open", cancellationToken);

        return shift == null ? null : CashShiftMapper.ToDto(shift);
    }
}

public class GetCashShiftsQueryHandler : IRequestHandler<GetCashShiftsQuery, PagedResponse<CashShiftDto>>
{
    private readonly IOrderSalesDbContext _context;

    public GetCashShiftsQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CashShiftDto>> Handle(GetCashShiftsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CashShifts.AsNoTracking().AsQueryable();
        if (request.CashierId.HasValue)
        {
            query = query.Where(s => s.CashierId == request.CashierId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var shifts = await query
            .OrderByDescending(s => s.OpenedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CashShiftDto>
        {
            Items = shifts.Select(CashShiftMapper.ToDto).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

internal static class CashShiftMapper
{
    public static CashShiftDto ToDto(CashShift shift) =>
        new(
            shift.Id,
            shift.ShiftCode,
            shift.CashierId,
            shift.CashierName,
            shift.OpenedAt,
            shift.OpeningCash,
            shift.ClosedAt,
            shift.ExpectedCash,
            shift.ActualCash,
            shift.Variance,
            shift.Status,
            shift.Note);
}
