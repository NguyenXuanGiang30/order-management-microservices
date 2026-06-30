using System;

namespace ProductInventoryService.Application.DTOs;

public class InventoryBalanceReportDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public int OpeningStock { get; set; }
    public int ReceivedQuantity { get; set; }
    public int ShippedQuantity { get; set; }
    public int ClosingStock { get; set; }
}
