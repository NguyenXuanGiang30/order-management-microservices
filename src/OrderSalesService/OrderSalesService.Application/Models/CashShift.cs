namespace OrderSalesService.Application.Models;

public class CashShift : AuditableEntity
{
    public string ShiftCode { get; set; } = null!;
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = null!;
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal Variance { get; set; }
    public string Status { get; set; } = "Open";
    public string? Note { get; set; }
}
