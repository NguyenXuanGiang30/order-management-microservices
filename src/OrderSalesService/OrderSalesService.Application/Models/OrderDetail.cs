namespace OrderSalesService.Application.Models;

/// <summary>
/// Chi tiết đơn hàng - Lưu kèm snapshot sản phẩm (tên, mã, giá, đơn vị) từ ProductInventoryDB.
/// </summary>
public class OrderDetail : BaseEntity
{
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
    public Guid ProductId { get; set; } // Ref ID -> ProductInventoryDB.Product
    public string ProductCode { get; set; } = null!; // Snapshot
    public string ProductName { get; set; } = null!; // Snapshot
    public string UnitName { get; set; } = null!; // Snapshot
    public decimal UnitPrice { get; set; } // Snapshot
    public decimal CostPrice { get; set; }
    public decimal CostTotal { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal SubTotal { get; set; }
}
