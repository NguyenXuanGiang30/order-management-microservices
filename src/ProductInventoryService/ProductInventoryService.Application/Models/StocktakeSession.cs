namespace ProductInventoryService.Application.Models;

public class StocktakeSession : AuditableEntity
{
    public string StocktakeCode { get; set; } = null!;
    public Guid CountedBy { get; set; }
    public string CountedByName { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
    public int TotalItems { get; set; }
    public int CountedItems { get; set; }
    public int TotalVarianceQuantity { get; set; }

    public virtual ICollection<StocktakeLine> Lines { get; set; } = new List<StocktakeLine>();
}
