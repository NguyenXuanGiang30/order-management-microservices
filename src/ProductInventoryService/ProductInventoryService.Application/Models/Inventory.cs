namespace ProductInventoryService.Application.Models;

/// <summary>
/// Tồn kho - Tách riêng khỏi Product để cập nhật tồn kho không lock bảng Product.
/// Có QuantityReserved cho đơn hàng đang xử lý (giữ hàng).
/// Có MinThreshold và MaxThreshold để cảnh báo 2 chiều.
/// </summary>
public class Inventory : BaseEntity
{
    public Guid ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int MinThreshold { get; set; }
    public int MaxThreshold { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation - 1:1 với Product
    public virtual Product Product { get; set; } = null!;
}
