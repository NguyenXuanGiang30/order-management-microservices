using MediatR;

namespace ProductInventoryService.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Command tạo sản phẩm mới (CQRS Pattern).
/// </summary>
public record CreateProductCommand(
    string Code,
    string Name,
    string? Description,
    string? Barcode,
    decimal ImportPrice,
    decimal SellPrice,
    string? ImageUrl,
    Guid CategoryId,
    Guid UnitId,
    decimal? Weight
) : IRequest<CreateProductResponse>;

public record CreateProductResponse(Guid Id, string Code, string Name);
