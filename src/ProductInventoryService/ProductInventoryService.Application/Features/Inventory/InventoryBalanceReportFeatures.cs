using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;

namespace ProductInventoryService.Application.Features.Inventory;

public record GetInventoryBalanceReportQuery(DateTime? StartDate, DateTime? EndDate, string? Search) 
    : IRequest<List<InventoryBalanceReportDto>>;

public class GetInventoryBalanceReportQueryHandler 
    : IRequestHandler<GetInventoryBalanceReportQuery, List<InventoryBalanceReportDto>>
{
    private readonly IProductInventoryDbContext _context;

    public GetInventoryBalanceReportQueryHandler(IProductInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<InventoryBalanceReportDto>> Handle(GetInventoryBalanceReportQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        // Get all products
        var productsQuery = _context.Products.Include(p => p.Unit).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(search) || p.Code.ToLower().Contains(search));
        }

        var products = await productsQuery.ToListAsync(cancellationToken);

        // Fetch all transactions for the products in chronological order
        var productIds = products.Select(p => p.Id).ToList();
        var allTransactions = await _context.InventoryTransactions
            .Where(t => productIds.Contains(t.ProductId))
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var reportList = new List<InventoryBalanceReportDto>();

        foreach (var product in products)
        {
            var txs = allTransactions.Where(t => t.ProductId == product.Id).ToList();

            // 1. Tồn đầu kỳ: Latest QuantityAfter of a transaction before startDate.
            // If none, check if there's any transaction during or after. If yes, the starting point was 0.
            // If there are no transactions at all for this product, then opening stock is its current stock.
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == product.Id, cancellationToken);
            var currentQty = inventory?.QuantityOnHand ?? 0;

            var txBefore = txs.LastOrDefault(t => t.CreatedAt < startDate);
            int openingStock = 0;
            if (txBefore != null)
            {
                openingStock = txBefore.QuantityAfter;
            }
            else
            {
                // If there are no transactions before the start date, it means all stock changes happened inside or after the period.
                // So we can compute opening stock by starting from the earliest transaction's QuantityAfter minus its QuantityChange.
                var firstTx = txs.FirstOrDefault();
                if (firstTx != null)
                {
                    openingStock = Math.Max(0, firstTx.QuantityAfter - firstTx.QuantityChange);
                }
                else
                {
                    // No transactions at all, opening stock is the current stock
                    openingStock = currentQty;
                }
            }

            // 2. Nhập trong kỳ: Sum of positive QuantityChange during [startDate, endDate]
            var received = txs
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.QuantityChange > 0)
                .Sum(t => t.QuantityChange);

            // 3. Xuất trong kỳ: Sum of negative QuantityChange during [startDate, endDate] (absolute value)
            var shipped = txs
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.QuantityChange < 0)
                .Sum(t => Math.Abs(t.QuantityChange));

            // 4. Tồn cuối kỳ: openingStock + received - shipped
            var closingStock = openingStock + received - shipped;

            reportList.Add(new InventoryBalanceReportDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitName = product.Unit.Name,
                OpeningStock = openingStock,
                ReceivedQuantity = received,
                ShippedQuantity = shipped,
                ClosingStock = closingStock
            });
        }

        return reportList;
    }
}
