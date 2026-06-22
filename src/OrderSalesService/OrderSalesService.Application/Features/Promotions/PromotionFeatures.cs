using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Promotions;

public record PromotionItemDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int RequiredQuantity);

public record PromotionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string PromotionType,
    string DiscountType,
    decimal DiscountValue,
    decimal MinimumOrderAmount,
    DateTime StartAt,
    DateTime EndAt,
    bool IsActive,
    List<PromotionItemDto> Items);

public record UpsertPromotionItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int RequiredQuantity);

public record GetActivePromotionsQuery : IRequest<List<PromotionDto>>;

public class GetActivePromotionsQueryHandler : IRequestHandler<GetActivePromotionsQuery, List<PromotionDto>>
{
    private readonly IOrderSalesDbContext _context;

    public GetActivePromotionsQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromotionDto>> Handle(GetActivePromotionsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var promotions = await _context.Promotions
            .Include(p => p.PromotionItems)
            .AsNoTracking()
            .Where(p => p.IsActive && p.StartAt <= now && p.EndAt >= now)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return promotions.Select(PromotionMapper.ToDto).ToList();
    }
}

public record GetPromotionsQuery : IRequest<List<PromotionDto>>;

public class GetPromotionsQueryHandler : IRequestHandler<GetPromotionsQuery, List<PromotionDto>>
{
    private readonly IOrderSalesDbContext _context;

    public GetPromotionsQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromotionDto>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        var promotions = await _context.Promotions
            .Include(p => p.PromotionItems)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return promotions.Select(PromotionMapper.ToDto).ToList();
    }
}

public record GetPromotionByCodeQuery(string Code) : IRequest<PromotionDto?>;

public class GetPromotionByCodeQueryHandler : IRequestHandler<GetPromotionByCodeQuery, PromotionDto?>
{
    private readonly IOrderSalesDbContext _context;

    public GetPromotionByCodeQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionDto?> Handle(GetPromotionByCodeQuery request, CancellationToken cancellationToken)
    {
        var code = PromotionMapper.NormalizeCode(request.Code);
        var promotion = await _context.Promotions
            .Include(p => p.PromotionItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

        return promotion == null ? null : PromotionMapper.ToDto(promotion);
    }
}

public record CreatePromotionCommand(
    string Code,
    string Name,
    string? Description,
    string PromotionType,
    string DiscountType,
    decimal DiscountValue,
    decimal MinimumOrderAmount,
    DateTime StartAt,
    DateTime EndAt,
    Guid CreatedBy,
    List<UpsertPromotionItemDto> Items) : IRequest<PromotionDto>;

public class CreatePromotionCommandHandler : IRequestHandler<CreatePromotionCommand, PromotionDto>
{
    private readonly IOrderSalesDbContext _context;

    public CreatePromotionCommandHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionDto> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = new Promotion
        {
            Code = PromotionMapper.NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            PromotionType = request.PromotionType.Trim(),
            DiscountType = request.DiscountType.Trim(),
            DiscountValue = request.DiscountValue,
            MinimumOrderAmount = request.MinimumOrderAmount,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            CreatedBy = request.CreatedBy,
            PromotionItems = request.Items.Select(PromotionMapper.ToEntity).ToList()
        };

        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync(cancellationToken);

        return PromotionMapper.ToDto(promotion);
    }
}

public record UpdatePromotionCommand(
    Guid Id,
    string Name,
    string? Description,
    string PromotionType,
    string DiscountType,
    decimal DiscountValue,
    decimal MinimumOrderAmount,
    DateTime StartAt,
    DateTime EndAt,
    List<UpsertPromotionItemDto> Items) : IRequest<PromotionDto?>;

public class UpdatePromotionCommandHandler : IRequestHandler<UpdatePromotionCommand, PromotionDto?>
{
    private readonly IOrderSalesDbContext _context;

    public UpdatePromotionCommandHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionDto?> Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await _context.Promotions
            .Include(p => p.PromotionItems)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (promotion == null)
        {
            return null;
        }

        promotion.Name = request.Name.Trim();
        promotion.Description = request.Description?.Trim();
        promotion.PromotionType = request.PromotionType.Trim();
        promotion.DiscountType = request.DiscountType.Trim();
        promotion.DiscountValue = request.DiscountValue;
        promotion.MinimumOrderAmount = request.MinimumOrderAmount;
        promotion.StartAt = request.StartAt;
        promotion.EndAt = request.EndAt;

        promotion.PromotionItems.Clear();
        foreach (var item in request.Items)
        {
            promotion.PromotionItems.Add(PromotionMapper.ToEntity(item));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return PromotionMapper.ToDto(promotion);
    }
}

public record TogglePromotionActiveCommand(Guid Id) : IRequest<PromotionDto?>;

public class TogglePromotionActiveCommandHandler : IRequestHandler<TogglePromotionActiveCommand, PromotionDto?>
{
    private readonly IOrderSalesDbContext _context;

    public TogglePromotionActiveCommandHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionDto?> Handle(TogglePromotionActiveCommand request, CancellationToken cancellationToken)
    {
        var promotion = await _context.Promotions
            .Include(p => p.PromotionItems)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (promotion == null)
        {
            return null;
        }

        promotion.IsActive = !promotion.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return PromotionMapper.ToDto(promotion);
    }
}

internal static class PromotionMapper
{
    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static PromotionDto ToDto(Promotion promotion)
    {
        return new PromotionDto(
            promotion.Id,
            promotion.Code,
            promotion.Name,
            promotion.Description,
            promotion.PromotionType,
            promotion.DiscountType,
            promotion.DiscountValue,
            promotion.MinimumOrderAmount,
            promotion.StartAt,
            promotion.EndAt,
            promotion.IsActive,
            promotion.PromotionItems.Select(item => new PromotionItemDto(
                item.Id,
                item.ProductId,
                item.ProductCode,
                item.ProductName,
                item.RequiredQuantity)).ToList());
    }

    public static PromotionItem ToEntity(UpsertPromotionItemDto item)
    {
        return new PromotionItem
        {
            ProductId = item.ProductId,
            ProductCode = item.ProductCode.Trim(),
            ProductName = item.ProductName.Trim(),
            RequiredQuantity = item.RequiredQuantity
        };
    }
}
