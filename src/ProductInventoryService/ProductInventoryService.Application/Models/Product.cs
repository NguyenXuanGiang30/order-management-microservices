namespace ProductInventoryService.Application.Models;

/// <summary>
/// Sản phẩm - Thông tin chi tiết về sản phẩm trong kho.
/// Có IsActive để soft-delete (ngừng bán mà không xóa).
/// Có Barcode để mở rộng cho việc quét mã vạch.
/// </summary>
public class Product : AuditableEntity
{
    public string Code { get; set; } = null!; // Mã SKU duy nhất
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal ImportPrice { get; set; }
    public decimal SellPrice { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Weight { get; set; }

    // Foreign Keys
    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public Guid UnitId { get; set; }
    public virtual Unit Unit { get; set; } = null!;

    // Navigation - 1:1 với Inventory
    public virtual Inventory? Inventory { get; set; }
    public virtual ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
