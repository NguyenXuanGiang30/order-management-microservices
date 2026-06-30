using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.CashBook;

public record CreateCashTransactionCommand(
    string Type, // Receipt or Payment
    decimal Amount,
    string SourceOrRecipient,
    string Category,
    string? Note,
    Guid CreatedBy,
    string CreatedByName) : IRequest<CashTransactionDto>;

public class CashBookCommandsHandler : IRequestHandler<CreateCashTransactionCommand, CashTransactionDto>
{
    private readonly IOrderSalesDbContext _context;

    public CashBookCommandsHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<CashTransactionDto> Handle(CreateCashTransactionCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var prefix = request.Type == "Receipt" ? "T" : "C";
        var dateString = DateTime.UtcNow.ToString("yyMMdd");
        
        var countToday = await _context.CashTransactions
            .CountAsync(c => c.CreatedAt >= today, cancellationToken);
        
        var transactionCode = $"SQ-{prefix}-{dateString}-{(countToday + 1):D4}";

        var cashTx = new CashTransaction
        {
            TransactionCode = transactionCode,
            Type = request.Type,
            Amount = request.Amount,
            SourceOrRecipient = request.SourceOrRecipient,
            Category = request.Category,
            Note = request.Note,
            CreatedBy = request.CreatedBy,
            CreatedByName = request.CreatedByName,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashTransactions.Add(cashTx);
        await _context.SaveChangesAsync(cancellationToken);

        return new CashTransactionDto
        {
            Id = cashTx.Id,
            TransactionCode = cashTx.TransactionCode,
            Type = cashTx.Type,
            Amount = cashTx.Amount,
            SourceOrRecipient = cashTx.SourceOrRecipient,
            Category = cashTx.Category,
            ReferenceId = cashTx.ReferenceId,
            Note = cashTx.Note,
            CreatedByName = cashTx.CreatedByName,
            CreatedAt = cashTx.CreatedAt
        };
    }
}
