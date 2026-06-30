using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Interfaces;

namespace OrderSalesService.Application.Features.CashBook;

public record GetCashTransactionsQuery(
    string? Search,
    string? Type,
    string? Category,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<CashTransactionDto>>;

public record GetCashBookBalanceQuery() : IRequest<CashBookBalanceDto>;

public class CashBookBalanceDto
{
    public decimal TotalReceipts { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal CurrentBalance { get; set; }
}

public class CashBookQueriesHandler :
    IRequestHandler<GetCashTransactionsQuery, PagedResponse<CashTransactionDto>>,
    IRequestHandler<GetCashBookBalanceQuery, CashBookBalanceDto>
{
    private readonly IOrderSalesDbContext _context;

    public CashBookQueriesHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CashTransactionDto>> Handle(GetCashTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.CashTransactions.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c => c.TransactionCode.ToLower().Contains(search) ||
                                     c.SourceOrRecipient.ToLower().Contains(search) ||
                                     (c.Note != null && c.Note.ToLower().Contains(search)));
        }

        if (!string.IsNullOrEmpty(request.Type))
        {
            query = query.Where(c => c.Type == request.Type);
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(c => c.Category == request.Category);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CashTransactionDto
            {
                Id = c.Id,
                TransactionCode = c.TransactionCode,
                Type = c.Type,
                Amount = c.Amount,
                SourceOrRecipient = c.SourceOrRecipient,
                Category = c.Category,
                ReferenceId = c.ReferenceId,
                Note = c.Note,
                CreatedByName = c.CreatedByName,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<CashTransactionDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<CashBookBalanceDto> Handle(GetCashBookBalanceQuery request, CancellationToken cancellationToken)
    {
        var receipts = await _context.CashTransactions
            .Where(c => c.Type == "Receipt")
            .SumAsync(c => c.Amount, cancellationToken);

        var payments = await _context.CashTransactions
            .Where(c => c.Type == "Payment")
            .SumAsync(c => c.Amount, cancellationToken);

        return new CashBookBalanceDto
        {
            TotalReceipts = receipts,
            TotalPayments = payments,
            CurrentBalance = receipts - payments
        };
    }
}
