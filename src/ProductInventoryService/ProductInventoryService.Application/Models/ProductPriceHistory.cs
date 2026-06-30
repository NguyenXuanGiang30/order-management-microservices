using System;

namespace ProductInventoryService.Application.Models;

/// <summary>
/// Lưu lịch sử thay đổi giá bán hoặc giá nhập của sản phẩm.
/// </summary>
public class ProductPriceHistory : AuditableEntity
{
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public decimal OldImportPrice { get; set; }
    public decimal NewImportPrice { get; set; }

    public decimal OldSellPrice { get; set; }
    public decimal NewSellPrice { get; set; }

    public string ChangedBy { get; set; } = "System";
}
