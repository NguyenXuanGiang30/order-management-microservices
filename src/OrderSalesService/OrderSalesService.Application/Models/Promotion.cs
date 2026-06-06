namespace OrderSalesService.Application.Models;

/// <summary>
/// Checkout promotion rule owned by OrderSales because it affects order money.
/// </summary>
public class Promotion : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string PromotionType { get; set; } = "Order";
    public string DiscountType { get; set; } = "Percent";
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual ICollection<PromotionItem> PromotionItems { get; set; } = new List<PromotionItem>();
}
