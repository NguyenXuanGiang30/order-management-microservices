using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;
using SharedContracts.Events;

namespace ProductInventoryService.Application.Features.Inventory;

public record StocktakeSessionDto(Guid Id, string StocktakeCode, string CountedByName, string Status, DateTime StartedAt,
    DateTime? ConfirmedAt, string? Note, int TotalItems, int CountedItems, int TotalVarianceQuantity, List<StocktakeLineDto> Lines);

public record StocktakeLineDto(Guid Id, Guid ProductId, string ProductCode, string ProductName, string UnitName,
    int SystemQuantity, int? CountedQuantity, int VarianceQuantity, string? Note);

public record CreateStocktakeSessionCommand(Guid CountedBy, string CountedByName, string? Note) : IRequest<StocktakeSessionDto>;

public class CreateStocktakeSessionCommandHandler : IRequestHandler<CreateStocktakeSessionCommand, StocktakeSessionDto>
{
    private readonly IProductInventoryDbContext _ctx;

    public CreateStocktakeSessionCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<StocktakeSessionDto> Handle(CreateStocktakeSessionCommand req, CancellationToken ct)
    {
        var inventories = await _ctx.Inventories
            .Include(i => i.Product)
            .ThenInclude(p => p.Unit)
            .OrderBy(i => i.Product.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        if (inventories.Count == 0)
        {
            throw new InvalidOperationException("Cannot create stocktake without inventory rows.");
        }

        var session = new StocktakeSession
        {
            StocktakeCode = $"KK-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            CountedBy = req.CountedBy,
            CountedByName = req.CountedByName,
            StartedAt = DateTime.UtcNow,
            Status = "Draft",
            Note = req.Note,
            TotalItems = inventories.Count,
            CountedItems = 0,
            TotalVarianceQuantity = 0,
            Lines = inventories.Select(i => new StocktakeLine
            {
                ProductId = i.ProductId,
                ProductCode = i.Product.Code,
                ProductName = i.Product.Name,
                UnitName = i.Product.Unit.Name,
                SystemQuantity = i.QuantityOnHand,
                CountedQuantity = null,
                VarianceQuantity = 0
            }).ToList()
        };

        _ctx.StocktakeSessions.Add(session);
        await _ctx.SaveChangesAsync(ct);

        return StocktakeMapper.ToDto(session);
    }
}

public record GetStocktakesQuery(string? Status, int PageNumber = 1, int PageSize = 20) : IRequest<PagedResponse<StocktakeSessionDto>>;

public class GetStocktakesQueryHandler : IRequestHandler<GetStocktakesQuery, PagedResponse<StocktakeSessionDto>>
{
    private readonly IProductInventoryDbContext _ctx;

    public GetStocktakesQueryHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResponse<StocktakeSessionDto>> Handle(GetStocktakesQuery req, CancellationToken ct)
    {
        var q = _ctx.StocktakeSessions.Include(s => s.Lines).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Status)) q = q.Where(s => s.Status == req.Status);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(s => s.StartedAt)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<StocktakeSessionDto>
        {
            Items = items.Select(StocktakeMapper.ToDto).ToList(),
            PageNumber = req.PageNumber,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}

public record GetStocktakeByIdQuery(Guid Id) : IRequest<StocktakeSessionDto?>;

public class GetStocktakeByIdQueryHandler : IRequestHandler<GetStocktakeByIdQuery, StocktakeSessionDto?>
{
    private readonly IProductInventoryDbContext _ctx;

    public GetStocktakeByIdQueryHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<StocktakeSessionDto?> Handle(GetStocktakeByIdQuery req, CancellationToken ct)
    {
        var session = await _ctx.StocktakeSessions
            .Include(s => s.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct);

        return session == null ? null : StocktakeMapper.ToDto(session);
    }
}

public record UpdateStocktakeLineItemDto(Guid LineId, int CountedQuantity, string? Note);
public record UpdateStocktakeLinesCommand(Guid SessionId, List<UpdateStocktakeLineItemDto> Lines) : IRequest<StocktakeSessionDto?>;

public class UpdateStocktakeLinesCommandHandler : IRequestHandler<UpdateStocktakeLinesCommand, StocktakeSessionDto?>
{
    private readonly IProductInventoryDbContext _ctx;

    public UpdateStocktakeLinesCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<StocktakeSessionDto?> Handle(UpdateStocktakeLinesCommand req, CancellationToken ct)
    {
        var session = await _ctx.StocktakeSessions.Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId && s.Status == "Draft", ct);

        if (session == null) return null;

        ApplyCounts(session, req.Lines);
        await _ctx.SaveChangesAsync(ct);

        return StocktakeMapper.ToDto(session);
    }

    internal static void ApplyCounts(StocktakeSession session, IEnumerable<UpdateStocktakeLineItemDto> updates)
    {
        var linesById = session.Lines.ToDictionary(line => line.Id);

        foreach (var update in updates)
        {
            if (!linesById.TryGetValue(update.LineId, out var line))
            {
                throw new InvalidOperationException($"Stocktake line {update.LineId} does not exist in this session.");
            }

            var adjustment = StocktakeAdjustmentPolicy.Calculate(line.SystemQuantity, update.CountedQuantity);
            line.CountedQuantity = update.CountedQuantity;
            line.VarianceQuantity = adjustment.VarianceQuantity;
            line.Note = update.Note;
        }

        StocktakeMapper.RecalculateTotals(session);
    }
}

public record ImportStocktakeCountsCommand(Guid SessionId, string CsvContent) : IRequest<StocktakeSessionDto?>;

public class ImportStocktakeCountsCommandHandler : IRequestHandler<ImportStocktakeCountsCommand, StocktakeSessionDto?>
{
    private readonly IProductInventoryDbContext _ctx;

    public ImportStocktakeCountsCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<StocktakeSessionDto?> Handle(ImportStocktakeCountsCommand req, CancellationToken ct)
    {
        var session = await _ctx.StocktakeSessions.Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId && s.Status == "Draft", ct);

        if (session == null) return null;

        var imported = InventoryCsvService.ParseStocktakeCounts(req.CsvContent);
        var linesByProductId = session.Lines.ToDictionary(line => line.ProductId);
        var linesByProductCode = session.Lines.ToDictionary(line => line.ProductCode, StringComparer.OrdinalIgnoreCase);

        foreach (var row in imported)
        {
            StocktakeLine? line = null;
            if (row.ProductId.HasValue) linesByProductId.TryGetValue(row.ProductId.Value, out line);
            if (line == null && !string.IsNullOrWhiteSpace(row.ProductCode)) linesByProductCode.TryGetValue(row.ProductCode, out line);
            if (line == null) continue;

            var adjustment = StocktakeAdjustmentPolicy.Calculate(line.SystemQuantity, row.CountedQuantity);
            line.CountedQuantity = row.CountedQuantity;
            line.VarianceQuantity = adjustment.VarianceQuantity;
            line.Note = row.Note;
        }

        StocktakeMapper.RecalculateTotals(session);
        await _ctx.SaveChangesAsync(ct);

        return StocktakeMapper.ToDto(session);
    }
}

public record ConfirmStocktakeCommand(Guid SessionId, Guid ConfirmedBy) : IRequest<StocktakeSessionDto?>;

public class ConfirmStocktakeCommandHandler : IRequestHandler<ConfirmStocktakeCommand, StocktakeSessionDto?>
{
    private readonly IProductInventoryDbContext _ctx;
    private readonly IPublishEndpoint _publishEndpoint;

    public ConfirmStocktakeCommandHandler(IProductInventoryDbContext ctx, IPublishEndpoint publishEndpoint)
    {
        _ctx = ctx;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<StocktakeSessionDto?> Handle(ConfirmStocktakeCommand req, CancellationToken ct)
    {
        var session = await _ctx.StocktakeSessions.Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == req.SessionId && s.Status == "Draft", ct);

        if (session == null) return null;
        if (session.Lines.Any(line => !line.CountedQuantity.HasValue))
        {
            throw new InvalidOperationException("All stocktake lines must be counted before confirmation.");
        }

        foreach (var line in session.Lines)
        {
            var inventory = await _ctx.Inventories.FirstOrDefaultAsync(i => i.ProductId == line.ProductId, ct);
            if (inventory == null)
            {
                inventory = new Models.Inventory
                {
                    ProductId = line.ProductId,
                    QuantityOnHand = 0,
                    MinThreshold = 5,
                    MaxThreshold = 500
                };
                _ctx.Inventories.Add(inventory);
            }

            var countedQuantity = line.CountedQuantity!.Value;
            var adjustment = StocktakeAdjustmentPolicy.Calculate(inventory.QuantityOnHand, countedQuantity);
            line.SystemQuantity = inventory.QuantityOnHand;
            line.VarianceQuantity = adjustment.VarianceQuantity;
            inventory.QuantityOnHand = countedQuantity;
            inventory.LastUpdated = DateTime.UtcNow;

            if (adjustment.VarianceQuantity != 0)
            {
                _ctx.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = line.ProductId,
                    TransactionType = "StocktakeAdjustment",
                    QuantityChange = adjustment.VarianceQuantity,
                    QuantityAfter = adjustment.QuantityAfter,
                    ReferenceType = "Stocktake",
                    ReferenceId = session.Id,
                    Note = $"Stocktake {session.StocktakeCode}: {adjustment.Direction}",
                    CreatedBy = req.ConfirmedBy
                });
            }
        }

        session.Status = "Confirmed";
        session.ConfirmedAt = DateTime.UtcNow;
        StocktakeMapper.RecalculateTotals(session);
        await _ctx.SaveChangesAsync(ct);

        foreach (var line in session.Lines.Where(line => line.VarianceQuantity != 0))
        {
            await _publishEndpoint.Publish(new InventoryUpdatedEvent
            {
                ProductId = line.ProductId,
                ProductCode = line.ProductCode,
                QuantityChange = line.VarianceQuantity,
                NewQuantityOnHand = line.CountedQuantity!.Value,
                Reason = $"Stocktake {session.StocktakeCode}",
                UpdatedAt = DateTime.UtcNow
            }, ct);
        }
        await _publishEndpoint.Publish(new AuditLoggedEvent
        {
            UserId = req.ConfirmedBy,
            ServiceName = "ProductInventoryService",
            Action = "StocktakeConfirmed",
            EntityType = "Stocktake",
            EntityId = session.Id.ToString(),
            Severity = session.TotalVarianceQuantity == 0 ? "Info" : "Warning",
            Description = $"Stocktake {session.StocktakeCode} confirmed with variance {session.TotalVarianceQuantity}.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        return StocktakeMapper.ToDto(session);
    }
}

public record CancelStocktakeCommand(Guid SessionId) : IRequest<bool>;

public class CancelStocktakeCommandHandler : IRequestHandler<CancelStocktakeCommand, bool>
{
    private readonly IProductInventoryDbContext _ctx;

    public CancelStocktakeCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> Handle(CancelStocktakeCommand req, CancellationToken ct)
    {
        var session = await _ctx.StocktakeSessions.FirstOrDefaultAsync(s => s.Id == req.SessionId && s.Status == "Draft", ct);
        if (session == null) return false;

        session.Status = "Cancelled";
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}

internal static class StocktakeMapper
{
    public static StocktakeSessionDto ToDto(StocktakeSession session)
    {
        return new StocktakeSessionDto(session.Id, session.StocktakeCode, session.CountedByName, session.Status,
            session.StartedAt, session.ConfirmedAt, session.Note, session.TotalItems, session.CountedItems,
            session.TotalVarianceQuantity,
            session.Lines.OrderBy(line => line.ProductName).Select(line => new StocktakeLineDto(line.Id, line.ProductId,
                line.ProductCode, line.ProductName, line.UnitName, line.SystemQuantity, line.CountedQuantity,
                line.VarianceQuantity, line.Note)).ToList());
    }

    public static void RecalculateTotals(StocktakeSession session)
    {
        session.TotalItems = session.Lines.Count;
        session.CountedItems = session.Lines.Count(line => line.CountedQuantity.HasValue);
        session.TotalVarianceQuantity = session.Lines.Sum(line => line.VarianceQuantity);
    }
}
