namespace OrderSalesService.Application.Models;

/// <summary>
/// Khách hàng - Quản lý thông tin, công nợ, tổng tiền đã mua.
/// </summary>
public class Customer : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public virtual CustomerGroup? CustomerGroup { get; set; }
    public decimal TotalPurchased { get; set; }
    public decimal DebtAmount { get; set; }
    public string? Note { get; set; }

    // Navigation
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
