using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;

namespace ProductInventoryService.Application.Features.Products;

public record GetProductPriceHistoryQuery(Guid ProductId) : IRequest<List<ProductPriceHistoryDto>>;

public class ProductPriceHistoryHandlers : 
    IRequestHandler<GetProductPriceHistoryQuery, List<ProductPriceHistoryDto>>
{
    private readonly IProductInventoryDbContext _context;

    public ProductPriceHistoryHandlers(IProductInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductPriceHistoryDto>> Handle(GetProductPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _context.ProductPriceHistories
            .Where(x => x.ProductId == request.ProductId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProductPriceHistoryDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                OldImportPrice = x.OldImportPrice,
                NewImportPrice = x.NewImportPrice,
                OldSellPrice = x.OldSellPrice,
                NewSellPrice = x.NewSellPrice,
                ChangedBy = x.ChangedBy,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
