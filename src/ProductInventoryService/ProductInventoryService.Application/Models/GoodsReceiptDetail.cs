namespace ProductInventoryService.Application.Models;

/// <summary>
/// Chi tiết phiếu nhập kho - Từng sản phẩm trong phiếu nhập.
/// </summary>
public class GoodsReceiptDetail : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}
