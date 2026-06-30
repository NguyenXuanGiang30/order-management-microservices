using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Features.Inventory;

public record UnitConversionDto(Guid Id, Guid ProductId, string ProductName, Guid FromUnitId, string FromUnitName, Guid ToUnitId, string ToUnitName, decimal Factor);

public record GetUnitConversionsQuery(Guid? ProductId) : IRequest<List<UnitConversionDto>>;

public class GetUnitConversionsQueryHandler : IRequestHandler<GetUnitConversionsQuery, List<UnitConversionDto>>
{
    private readonly IProductInventoryDbContext _ctx;

    public GetUnitConversionsQueryHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<List<UnitConversionDto>> Handle(GetUnitConversionsQuery req, CancellationToken ct)
    {
        var q = _ctx.UnitConversions
            .Include(u => u.Product)
            .Include(u => u.FromUnit)
            .Include(u => u.ToUnit)
            .AsNoTracking()
            .AsQueryable();

        if (req.ProductId.HasValue)
        {
            q = q.Where(u => u.ProductId == req.ProductId.Value);
        }

        return await q.Select(u => new UnitConversionDto(
            u.Id,
            u.ProductId,
            u.Product.Name,
            u.FromUnitId,
            u.FromUnit.Name,
            u.ToUnitId,
            u.ToUnit.Name,
            u.Factor
        )).ToListAsync(ct);
    }
}

public record CreateUnitConversionCommand(Guid ProductId, Guid FromUnitId, Guid ToUnitId, decimal Factor) : IRequest<UnitConversionDto>;

public class CreateUnitConversionCommandHandler : IRequestHandler<CreateUnitConversionCommand, UnitConversionDto>
{
    private readonly IProductInventoryDbContext _ctx;

    public CreateUnitConversionCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<UnitConversionDto> Handle(CreateUnitConversionCommand req, CancellationToken ct)
    {
        if (req.Factor <= 0)
        {
            throw new InvalidOperationException("Tỷ lệ quy đổi phải lớn hơn 0.");
        }
        if (req.FromUnitId == req.ToUnitId)
        {
            throw new InvalidOperationException("Đơn vị nguồn và đơn vị đích không được trùng nhau.");
        }

        var productExists = await _ctx.Products.AnyAsync(p => p.Id == req.ProductId, ct);
        if (!productExists)
        {
            throw new InvalidOperationException("Sản phẩm không tồn tại.");
        }

        var fromUnitExists = await _ctx.Units.AnyAsync(u => u.Id == req.FromUnitId, ct);
        var toUnitExists = await _ctx.Units.AnyAsync(u => u.Id == req.ToUnitId, ct);
        if (!fromUnitExists || !toUnitExists)
        {
            throw new InvalidOperationException("Một trong hai đơn vị tính không tồn tại.");
        }

        var exists = await _ctx.UnitConversions.AnyAsync(u => u.ProductId == req.ProductId && u.FromUnitId == req.FromUnitId && u.ToUnitId == req.ToUnitId, ct);
        if (exists)
        {
            throw new InvalidOperationException("Quy tắc quy đổi này đã tồn tại cho sản phẩm.");
        }

        var conversion = new UnitConversion
        {
            ProductId = req.ProductId,
            FromUnitId = req.FromUnitId,
            ToUnitId = req.ToUnitId,
            Factor = req.Factor,
            IsActive = true
        };

        _ctx.UnitConversions.Add(conversion);
        await _ctx.SaveChangesAsync(ct);

        // Fetch again to get related entities
        var saved = await _ctx.UnitConversions
            .Include(u => u.Product)
            .Include(u => u.FromUnit)
            .Include(u => u.ToUnit)
            .FirstAsync(u => u.Id == conversion.Id, ct);

        return new UnitConversionDto(
            saved.Id,
            saved.ProductId,
            saved.Product.Name,
            saved.FromUnitId,
            saved.FromUnit.Name,
            saved.ToUnitId,
            saved.ToUnit.Name,
            saved.Factor
        );
    }
}

public record DeleteUnitConversionCommand(Guid Id) : IRequest<bool>;

public class DeleteUnitConversionCommandHandler : IRequestHandler<DeleteUnitConversionCommand, bool>
{
    private readonly IProductInventoryDbContext _ctx;

    public DeleteUnitConversionCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> Handle(DeleteUnitConversionCommand req, CancellationToken ct)
    {
        var conversion = await _ctx.UnitConversions.FirstOrDefaultAsync(u => u.Id == req.Id, ct);
        if (conversion == null) return false;

        _ctx.UnitConversions.Remove(conversion);
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}
