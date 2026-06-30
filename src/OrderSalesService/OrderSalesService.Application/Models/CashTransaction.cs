namespace OrderSalesService.Application.Models;

/// <summary>
/// Giao dịch thu chi ngoài (Sổ Quỹ)
/// </summary>
public class CashTransaction : BaseEntity
{
    public string TransactionCode { get; set; } = string.Empty; // e.g. SQ0001, SQ0002
    public string Type { get; set; } = "Receipt"; // Receipt (Thu), Payment (Chi)
    public decimal Amount { get; set; }
    public string SourceOrRecipient { get; set; } = string.Empty; // Người nộp/nhận
    public string Category { get; set; } = "Khác"; // Bán hàng, Chi phí vận hành, Nhập hàng, Khác
    public Guid? ReferenceId { get; set; } // Reference OrderId, ShiftId, etc.
    public string? Note { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
