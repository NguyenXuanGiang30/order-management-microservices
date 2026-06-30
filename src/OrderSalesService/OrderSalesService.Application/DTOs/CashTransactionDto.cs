namespace OrderSalesService.Application.DTOs;

public class CashTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string Type { get; set; } = "Receipt"; // Receipt/Payment
    public decimal Amount { get; set; }
    public string SourceOrRecipient { get; set; } = string.Empty;
    public string Category { get; set; } = "Khác";
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
