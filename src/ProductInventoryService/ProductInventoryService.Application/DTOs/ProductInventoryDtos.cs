namespace ProductInventoryService.Application.DTOs;

// ======================== Product DTOs ========================
public record ProductDto(
    Guid Id, string Code, string Name, string? Description, string? Barcode,
    decimal ImportPrice, decimal SellPrice, string? ImageUrl, decimal? Weight,
    Guid CategoryId, string CategoryName, Guid UnitId, string UnitName,
    int QuantityOnHand, int QuantityReserved, bool IsActive, DateTime CreatedAt);

public record CreateProductDto(
    string Code, string Name, string? Description, string? Barcode,
    decimal ImportPrice, decimal SellPrice, string? ImageUrl,
    Guid CategoryId, Guid UnitId, decimal? Weight);

public record UpdateProductDto(
    string Name, string? Description, string? Barcode,
    decimal ImportPrice, decimal SellPrice, string? ImageUrl,
    Guid CategoryId, Guid UnitId, decimal? Weight);

// ======================== Category DTOs ========================
public record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentId, int SortOrder, bool IsActive, List<CategoryDto> Children = null!);
public record CreateCategoryDto(string Name, string? Description, Guid? ParentId, int SortOrder);
public record UpdateCategoryDto(string Name, string? Description, Guid? ParentId, int SortOrder);

// ======================== Unit DTOs ========================
public record UnitDto(Guid Id, string Name, string? Abbreviation, bool IsActive);
public record CreateUnitDto(string Name, string? Abbreviation);

// ======================== Inventory DTOs ========================
public record InventoryDto(Guid Id, Guid ProductId, string ProductCode, string ProductName,
    int QuantityOnHand, int QuantityReserved, int Available, int MinThreshold, int MaxThreshold, DateTime LastUpdated);

// ======================== GoodsReceipt DTOs ========================
public record GoodsReceiptDto(Guid Id, string ReceiptCode, Guid SupplierId, string SupplierName,
    string CreatedByName, DateTime ReceiptDate, string? Note, decimal TotalAmount, string Status,
    DateTime CreatedAt, List<GoodsReceiptDetailDto> Details);

public record GoodsReceiptDetailDto(Guid Id, Guid ProductId, string? ProductName, int Quantity, decimal UnitPrice, decimal SubTotal);

public record CreateGoodsReceiptDto(Guid SupplierId, string SupplierName, Guid CreatedBy,
    string CreatedByName, string? Note, List<CreateGoodsReceiptDetailDto> Details);

public record CreateGoodsReceiptDetailDto(Guid ProductId, int Quantity, decimal UnitPrice);
