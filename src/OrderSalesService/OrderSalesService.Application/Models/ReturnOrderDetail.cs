namespace OrderSalesService.Application.Models;

/// <summary>
/// Chi tiết phiếu trả hàng - Đối chiếu số lượng trả không vượt quá đơn gốc.
/// </summary>
public class ReturnOrderDetail : BaseEntity
{
    public Guid ReturnOrderId { get; set; }
    public virtual ReturnOrder ReturnOrder { get; set; } = null!;
    public Guid OrderDetailId { get; set; } // Dòng SP trong đơn gốc
    public Guid ProductId { get; set; } // Ref ID -> ProductInventoryDB.Product
    public int ReturnQuantity { get; set; }
    public decimal RefundAmount { get; set; }
}
