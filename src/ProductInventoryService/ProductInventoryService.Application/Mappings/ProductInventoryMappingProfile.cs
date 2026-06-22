using AutoMapper;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Mappings;

/// <summary>
/// AutoMapper Profile: Cấu hình ánh xạ Entity ↔ DTO.
/// Không trả Entity trực tiếp, luôn dùng DTO.
/// </summary>
public class ProductInventoryMappingProfile : Profile
{
    public ProductInventoryMappingProfile()
    {
        // Product → ProductDto (bao gồm thông tin liên kết)
        CreateMap<Product, ProductDto>()
            .ConstructUsing(s => new ProductDto(
                s.Id,
                s.Code,
                s.Name,
                s.Description,
                s.Barcode,
                s.ImportPrice,
                s.SellPrice,
                s.ImageUrl,
                s.Weight,
                s.CategoryId,
                s.Category != null ? s.Category.Name : "",
                s.UnitId,
                s.Unit != null ? s.Unit.Name : "",
                s.Inventory != null ? s.Inventory.QuantityOnHand : 0,
                s.Inventory != null ? s.Inventory.QuantityReserved : 0,
                s.IsActive,
                s.CreatedAt
            ));

        // Category mappings
        CreateMap<Category, CategoryDto>();

        // Unit mappings
        CreateMap<Unit, UnitDto>();

        // Inventory → InventoryDto
        CreateMap<Inventory, InventoryDto>()
            .ForMember(d => d.ProductCode, opt => opt.MapFrom(s => s.Product != null ? s.Product.Code : ""))
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : ""))
            .ForMember(d => d.Available, opt => opt.MapFrom(s => s.QuantityOnHand - s.QuantityReserved));

        // GoodsReceipt mappings
        CreateMap<GoodsReceipt, GoodsReceiptDto>();
        CreateMap<GoodsReceiptDetail, GoodsReceiptDetailDto>();
    }
}
