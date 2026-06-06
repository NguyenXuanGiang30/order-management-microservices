namespace ProductInventoryService.Application.Models;

public class StocktakeLine : BaseEntity
{
    public Guid StocktakeSessionId { get; set; }
    public virtual StocktakeSession StocktakeSession { get; set; } = null!;

    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public int SystemQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public int VarianceQuantity { get; set; }
    public string? Note { get; set; }
}
