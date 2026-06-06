namespace ProductInventoryService.Application.Models;

/// <summary>
/// Phiếu nhập kho - Có snapshot SupplierName, CreatedByName để hiển thị
/// mà không cần gọi API chéo service.
/// </summary>
public class GoodsReceipt : AuditableEntity
{
    public string ReceiptCode { get; set; } = null!;
    public Guid SupplierId { get; set; } // Ref ID -> OrderSalesDB.Supplier
    public string SupplierName { get; set; } = null!; // Snapshot tên NCC
    public Guid CreatedBy { get; set; } // Ref ID -> UserReportDB.User
    public string CreatedByName { get; set; } = null!; // Snapshot tên người tạo
    public DateTime ReceiptDate { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Confirmed, Cancelled

    // Navigation
    public virtual ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
}
