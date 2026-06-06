namespace ProductInventoryService.Application.Models;

/// <summary>
/// Lịch sử biến động kho - Ghi nhận mọi thay đổi tồn kho (nhập, xuất, điều chỉnh, trả hàng).
/// Phục vụ truy vết và đối soát.
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public string TransactionType { get; set; } = null!; // Import, Export, Adjust, Return
    public int QuantityChange { get; set; } // +50 (nhập) hoặc -3 (xuất)
    public int QuantityAfter { get; set; } // Tồn kho sau giao dịch

    public string ReferenceType { get; set; } = null!; // GoodsReceipt, Order, Manual
    public Guid? ReferenceId { get; set; }

    public string? Note { get; set; }
    public Guid CreatedBy { get; set; } // Ref ID -> UserReportDB.User
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
