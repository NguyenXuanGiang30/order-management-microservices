using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Models;

/// <summary>
/// Thanh toán công nợ nhà cung cấp.
/// </summary>
public class SupplierPayment : AuditableEntity
{
    public string PaymentCode { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Tiền mặt"; // Tiền mặt, Chuyển khoản
    public DateTime PaymentDate { get; set; }
    public string? Note { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = null!;

    // Navigation
    public virtual Supplier Supplier { get; set; } = null!;
}
