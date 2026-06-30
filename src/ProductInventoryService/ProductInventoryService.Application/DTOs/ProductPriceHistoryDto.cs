using System;

namespace ProductInventoryService.Application.DTOs;

public class ProductPriceHistoryDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal OldImportPrice { get; set; }
    public decimal NewImportPrice { get; set; }
    public decimal OldSellPrice { get; set; }
    public decimal NewSellPrice { get; set; }
    public string ChangedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
