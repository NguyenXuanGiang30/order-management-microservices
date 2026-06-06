using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Features.Products;

// ======================== Update Product ========================
public record UpdateProductCommand(Guid Id, string? Name, string? Description, string? Barcode,
    decimal? ImportPrice, decimal? SellPrice, string? ImageUrl, Guid? CategoryId, Guid? UnitId) : IRequest<ProductDto?>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    private readonly IProductInventoryDbContext _ctx;
    private readonly IMapper _mapper;
    public UpdateProductCommandHandler(IProductInventoryDbContext ctx, IMapper m) { _ctx = ctx; _mapper = m; }

    public async Task<ProductDto?> Handle(UpdateProductCommand req, CancellationToken ct)
    {
        var product = await _ctx.Products.Include(p => p.Category).Include(p => p.Unit).Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (product == null) return null;

        if (req.Name != null) product.Name = req.Name;
        if (req.Description != null) product.Description = req.Description;
        if (req.Barcode != null) product.Barcode = req.Barcode;
        if (req.ImportPrice.HasValue) product.ImportPrice = req.ImportPrice.Value;
        if (req.SellPrice.HasValue) product.SellPrice = req.SellPrice.Value;
        if (req.ImageUrl != null) product.ImageUrl = req.ImageUrl;
        if (req.CategoryId.HasValue) product.CategoryId = req.CategoryId.Value;
        if (req.UnitId.HasValue) product.UnitId = req.UnitId.Value;

        await _ctx.SaveChangesAsync(ct);
        return _mapper.Map<ProductDto>(product);
    }
}

// ======================== Toggle Product Active ========================
public record ToggleProductActiveCommand(Guid Id) : IRequest<bool>;

public class ToggleProductActiveCommandHandler : IRequestHandler<ToggleProductActiveCommand, bool>
{
    private readonly IProductInventoryDbContext _ctx;
    public ToggleProductActiveCommandHandler(IProductInventoryDbContext ctx) { _ctx = ctx; }

    public async Task<bool> Handle(ToggleProductActiveCommand req, CancellationToken ct)
    {
        var product = await _ctx.Products.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (product == null) return false;
        product.IsActive = !product.IsActive;
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}
