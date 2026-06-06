namespace OrderSalesService.Application.Models;

/// <summary>
/// Nhà cung cấp - Quản lý thông tin NCC, liên hệ.
/// </summary>
public class Supplier : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public decimal DebtAmount { get; set; }
    public string? Note { get; set; }
    public Guid CreatedBy { get; set; } // Ref ID -> UserReportDB.User
}
