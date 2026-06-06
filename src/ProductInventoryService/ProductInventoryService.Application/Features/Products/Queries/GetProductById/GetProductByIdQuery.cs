using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;

namespace ProductInventoryService.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductInventoryDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.Inventory)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        return product == null ? null : _mapper.Map<ProductDto>(product);
    }
}
