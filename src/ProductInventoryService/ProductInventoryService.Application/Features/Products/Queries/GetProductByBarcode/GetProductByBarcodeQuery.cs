using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace ProductInventoryService.Application.Features.Products.Queries.GetProductByBarcode;

public record GetProductByBarcodeQuery(string Barcode) : IRequest<ProductDto?>;

public class GetProductByBarcodeQueryHandler : IRequestHandler<GetProductByBarcodeQuery, ProductDto?>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;

    public GetProductByBarcodeQueryHandler(IProductInventoryDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode)) return null;

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.Inventory)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == request.Barcode && p.IsActive, cancellationToken);

        return product == null ? null : _mapper.Map<ProductDto>(product);
    }
}
