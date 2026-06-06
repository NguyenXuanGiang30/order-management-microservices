namespace OrderSalesService.Application.Models;

/// <summary>
/// Product requirement for product-level and combo promotions.
/// </summary>
public class PromotionItem : BaseEntity
{
    public Guid PromotionId { get; set; }
    public virtual Promotion Promotion { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int RequiredQuantity { get; set; } = 1;
}
