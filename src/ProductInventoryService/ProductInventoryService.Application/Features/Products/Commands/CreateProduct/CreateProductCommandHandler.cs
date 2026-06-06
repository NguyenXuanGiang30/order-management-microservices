using MediatR;
using ProductInventoryService.Application.Models;
using ProductInventoryService.Application.Interfaces;

namespace ProductInventoryService.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Handler xử lý lệnh tạo sản phẩm mới.
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, CreateProductResponse>
{
    private readonly IProductInventoryDbContext _context;

    public CreateProductCommandHandler(IProductInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<CreateProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Barcode = request.Barcode,
            ImportPrice = request.ImportPrice,
            SellPrice = request.SellPrice,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            UnitId = request.UnitId,
            Weight = request.Weight
        };

        _context.Products.Add(product);

        // Tạo bản ghi Inventory tương ứng (1:1)
        var inventory = new ProductInventoryService.Application.Models.Inventory
        {
            ProductId = product.Id,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            MinThreshold = 10,
            MaxThreshold = 1000
        };

        _context.Inventories.Add(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateProductResponse(product.Id, product.Code, product.Name);
    }
}
