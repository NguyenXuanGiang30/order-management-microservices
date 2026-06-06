using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.Interfaces;
using ProductInventoryService.Application.Models;
using SharedContracts.Events;

namespace ProductInventoryService.Application.Features.Inventory;

public record GoodsReceiptDto(Guid Id, string ReceiptCode, Guid SupplierId, string SupplierName, string CreatedByName,
    DateTime ReceiptDate, string? Note, decimal TotalAmount, string Status, DateTime CreatedAt, List<GoodsReceiptDetailDto> Details);

public record GoodsReceiptDetailDto(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal SubTotal);

public record StockDto(Guid ProductId, string ProductCode, string ProductName, string UnitName, int QuantityOnHand, int QuantityReserved,
    int AvailableQuantity, int MinThreshold, int MaxThreshold, bool IsBelowMin, string AlertLevel, int ReorderQuantity,
    int RecommendedOrderQuantity, string StockCoverageLabel);

public record InventoryTransactionDto(Guid Id, Guid ProductId, string ProductName, string TransactionType, int QuantityChange,
    int QuantityAfter, string ReferenceType, Guid? ReferenceId, string? Note, DateTime CreatedAt);

public record CreateGoodsReceiptItemDto(Guid ProductId, int Quantity, decimal UnitPrice);

public record GetGoodsReceiptsQuery(string? Status, DateTime? From, DateTime? To, int PageNumber = 1, int PageSize = 20) : IRequest<PagedResponse<GoodsReceiptDto>>;

public class GetGoodsReceiptsQueryHandler : IRequestHandler<GetGoodsReceiptsQuery, PagedResponse<GoodsReceiptDto>>
{
    private readonly IProductInventoryDbContext _ctx;

    public GetGoodsReceiptsQueryHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResponse<GoodsReceiptDto>> Handle(GetGoodsReceiptsQuery req, CancellationToken ct)
    {
        var q = _ctx.GoodsReceipts
            .Include(g => g.GoodsReceiptDetails)
            .ThenInclude(d => d.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Status)) q = q.Where(g => g.Status == req.Status);
        if (req.From.HasValue) q = q.Where(g => g.ReceiptDate >= req.From.Value);
        if (req.To.HasValue) q = q.Where(g => g.ReceiptDate <= req.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(g => g.CreatedAt)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = items.Select(ToGoodsReceiptDto).ToList();

        return new PagedResponse<GoodsReceiptDto> { Items = dtos, PageNumber = req.PageNumber, PageSize = req.PageSize, TotalCount = total };
    }

    private static GoodsReceiptDto ToGoodsReceiptDto(GoodsReceipt receipt)
    {
        return new GoodsReceiptDto(receipt.Id, receipt.ReceiptCode, receipt.SupplierId, receipt.SupplierName, receipt.CreatedByName,
            receipt.ReceiptDate, receipt.Note, receipt.TotalAmount, receipt.Status, receipt.CreatedAt,
            receipt.GoodsReceiptDetails.Select(d => new GoodsReceiptDetailDto(d.Id, d.ProductId, d.Product?.Name ?? "",
                d.Quantity, d.UnitPrice, d.SubTotal)).ToList());
    }
}

public record CreateGoodsReceiptCommand(Guid SupplierId, string SupplierName, Guid CreatedBy, string CreatedByName, string? Note,
    List<CreateGoodsReceiptItemDto> Items) : IRequest<GoodsReceiptDto>;

public class CreateGoodsReceiptCommandHandler : IRequestHandler<CreateGoodsReceiptCommand, GoodsReceiptDto>
{
    private readonly IProductInventoryDbContext _ctx;

    public CreateGoodsReceiptCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<GoodsReceiptDto> Handle(CreateGoodsReceiptCommand req, CancellationToken ct)
    {
        if (req.Items.Count == 0)
        {
            throw new InvalidOperationException("Goods receipt must contain at least one item.");
        }

        var productIds = req.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _ctx.Products.Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id, ct);

        foreach (var item in req.Items)
        {
            if (item.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
            if (item.UnitPrice < 0) throw new InvalidOperationException("Unit price cannot be negative.");
            if (!products.ContainsKey(item.ProductId)) throw new InvalidOperationException($"Product {item.ProductId} does not exist.");
        }

        var receiptCode = $"PN-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var details = req.Items.Select(item => new GoodsReceiptDetail
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            SubTotal = item.Quantity * item.UnitPrice
        }).ToList();

        var receipt = new GoodsReceipt
        {
            ReceiptCode = receiptCode,
            SupplierId = req.SupplierId,
            SupplierName = req.SupplierName,
            CreatedBy = req.CreatedBy,
            CreatedByName = req.CreatedByName,
            ReceiptDate = DateTime.UtcNow,
            Note = req.Note,
            TotalAmount = details.Sum(d => d.SubTotal),
            Status = "Draft",
            GoodsReceiptDetails = details
        };

        _ctx.GoodsReceipts.Add(receipt);
        await _ctx.SaveChangesAsync(ct);

        return new GoodsReceiptDto(receipt.Id, receipt.ReceiptCode, receipt.SupplierId, receipt.SupplierName, receipt.CreatedByName,
            receipt.ReceiptDate, receipt.Note, receipt.TotalAmount, receipt.Status, receipt.CreatedAt,
            details.Select(d => new GoodsReceiptDetailDto(d.Id, d.ProductId, products[d.ProductId].Name, d.Quantity, d.UnitPrice, d.SubTotal)).ToList());
    }
}

public record ConfirmGoodsReceiptCommand(Guid ReceiptId, Guid ConfirmedBy) : IRequest<bool>;

public class ConfirmGoodsReceiptCommandHandler : IRequestHandler<ConfirmGoodsReceiptCommand, bool>
{
    private readonly IProductInventoryDbContext _ctx;
    private readonly IPublishEndpoint _publishEndpoint;

    public ConfirmGoodsReceiptCommandHandler(IProductInventoryDbContext ctx, IPublishEndpoint publishEndpoint)
    {
        _ctx = ctx;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(ConfirmGoodsReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _ctx.GoodsReceipts
            .Include(g => g.GoodsReceiptDetails)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(g => g.Id == req.ReceiptId && g.Status == "Draft", ct);

        if (receipt == null) return false;

        receipt.Status = "Confirmed";

        foreach (var detail in receipt.GoodsReceiptDetails)
        {
            var inventory = await _ctx.Inventories.FirstOrDefaultAsync(i => i.ProductId == detail.ProductId, ct);
            var currentQuantityOnHand = inventory?.QuantityOnHand ?? 0;
            if (detail.Product != null)
            {
                detail.Product.ImportPrice = InventoryCostPolicy.CalculateWeightedAverageImportPrice(
                    currentQuantityOnHand,
                    detail.Product.ImportPrice,
                    detail.Quantity,
                    detail.UnitPrice);
            }

            var quantityAfter = detail.Quantity;

            if (inventory != null)
            {
                inventory.QuantityOnHand += detail.Quantity;
                inventory.LastUpdated = DateTime.UtcNow;
                quantityAfter = inventory.QuantityOnHand;
            }
            else
            {
                _ctx.Inventories.Add(new Models.Inventory
                {
                    ProductId = detail.ProductId,
                    QuantityOnHand = detail.Quantity,
                    MinThreshold = 5,
                    MaxThreshold = 500
                });
            }

            _ctx.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = detail.ProductId,
                TransactionType = "Import",
                QuantityChange = detail.Quantity,
                QuantityAfter = quantityAfter,
                ReferenceType = "GoodsReceipt",
                ReferenceId = receipt.Id,
                Note = $"Import from receipt {receipt.ReceiptCode}",
                CreatedBy = req.ConfirmedBy
            });
        }

        await _ctx.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new GoodsReceiptConfirmedEvent
        {
            EventId = $"goods-receipt-confirmed:{receipt.Id}",
            ReceiptId = receipt.Id,
            ReceiptCode = receipt.ReceiptCode,
            SupplierId = receipt.SupplierId,
            SupplierName = receipt.SupplierName,
            TotalAmount = receipt.TotalAmount,
            ConfirmedAt = DateTime.UtcNow
        }, ct);
        await _publishEndpoint.Publish(new AuditLoggedEvent
        {
            UserId = req.ConfirmedBy,
            ServiceName = "ProductInventoryService",
            Action = "GoodsReceiptConfirmed",
            EntityType = "GoodsReceipt",
            EntityId = receipt.Id.ToString(),
            Severity = "Info",
            Description = $"Goods receipt {receipt.ReceiptCode} confirmed with amount {receipt.TotalAmount}.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        foreach (var detail in receipt.GoodsReceiptDetails)
        {
            var inventory = await _ctx.Inventories.FirstOrDefaultAsync(i => i.ProductId == detail.ProductId, ct);
            var newQuantity = inventory?.QuantityOnHand ?? detail.Quantity;

            await _publishEndpoint.Publish(new InventoryUpdatedEvent
            {
                ProductId = detail.ProductId,
                ProductCode = detail.Product?.Code ?? "",
                QuantityChange = detail.Quantity,
                NewQuantityOnHand = newQuantity,
                Reason = $"Import (Receipt {receipt.ReceiptCode})",
                UpdatedAt = DateTime.UtcNow
            }, ct);
        }

        return true;
    }
}

public record CancelGoodsReceiptCommand(Guid ReceiptId) : IRequest<bool>;

public class CancelGoodsReceiptCommandHandler : IRequestHandler<CancelGoodsReceiptCommand, bool>
{
    private readonly IProductInventoryDbContext _ctx;

    public CancelGoodsReceiptCommandHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> Handle(CancelGoodsReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _ctx.GoodsReceipts.FirstOrDefaultAsync(g => g.Id == req.ReceiptId && g.Status == "Draft", ct);
        if (receipt == null) return false;

        receipt.Status = "Cancelled";
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}

public record GetStockQuery(bool? BelowMin, string? Search) : IRequest<List<StockDto>>;

public class GetStockQueryHandler : IRequestHandler<GetStockQuery, List<StockDto>>
{
    private readonly IProductInventoryDbContext _ctx;

    public GetStockQueryHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<List<StockDto>> Handle(GetStockQuery req, CancellationToken ct)
    {
        var q = _ctx.Inventories
            .Include(i => i.Product)
            .ThenInclude(p => p.Unit)
            .AsNoTracking()
            .AsQueryable();

        if (req.BelowMin == true)
        {
            q = q.Where(i => i.QuantityOnHand - i.QuantityReserved < i.MinThreshold);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.ToLower();
            q = q.Where(i => i.Product.Name.ToLower().Contains(search) || i.Product.Code.ToLower().Contains(search));
        }

        var items = await q.Select(i => new
        {
            i.ProductId,
            ProductCode = i.Product.Code,
            ProductName = i.Product.Name,
            UnitName = i.Product.Unit.Name,
            i.QuantityOnHand,
            i.QuantityReserved,
            i.MinThreshold,
            i.MaxThreshold
        }).ToListAsync(ct);

        return items.Select(i =>
        {
            var availableQuantity = Math.Max(0, i.QuantityOnHand - i.QuantityReserved);
            var alert = StockAlertPolicy.Calculate(new StockAlertInput(i.QuantityOnHand, i.QuantityReserved, i.MinThreshold, i.MaxThreshold));

            return new StockDto(i.ProductId, i.ProductCode, i.ProductName, i.UnitName, i.QuantityOnHand, i.QuantityReserved,
                availableQuantity, i.MinThreshold, i.MaxThreshold, availableQuantity < i.MinThreshold,
                alert.AlertLevel, alert.ReorderQuantity, alert.RecommendedOrderQuantity, alert.StockCoverageLabel);
        }).ToList();
    }
}

public record GetInventoryTransactionsQuery(Guid? ProductId, string? Type, DateTime? From, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<InventoryTransactionDto>>;

public class GetInventoryTransactionsQueryHandler : IRequestHandler<GetInventoryTransactionsQuery, PagedResponse<InventoryTransactionDto>>
{
    private readonly IProductInventoryDbContext _ctx;

    public GetInventoryTransactionsQueryHandler(IProductInventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResponse<InventoryTransactionDto>> Handle(GetInventoryTransactionsQuery req, CancellationToken ct)
    {
        var q = _ctx.InventoryTransactions.Include(t => t.Product).AsNoTracking().AsQueryable();

        if (req.ProductId.HasValue) q = q.Where(t => t.ProductId == req.ProductId.Value);
        if (!string.IsNullOrWhiteSpace(req.Type)) q = q.Where(t => t.TransactionType == req.Type);
        if (req.From.HasValue) q = q.Where(t => t.CreatedAt >= req.From.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.CreatedAt)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => new InventoryTransactionDto(t.Id, t.ProductId, t.Product.Name, t.TransactionType, t.QuantityChange,
                t.QuantityAfter, t.ReferenceType, t.ReferenceId, t.Note, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<InventoryTransactionDto> { Items = items, PageNumber = req.PageNumber, PageSize = req.PageSize, TotalCount = total };
    }
}
