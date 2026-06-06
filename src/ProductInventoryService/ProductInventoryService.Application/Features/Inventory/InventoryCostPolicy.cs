namespace ProductInventoryService.Application.Features.Inventory;

public static class InventoryCostPolicy
{
    public static decimal CalculateWeightedAverageImportPrice(
        int currentQuantityOnHand,
        decimal currentImportPrice,
        int receiptQuantity,
        decimal receiptUnitPrice)
    {
        if (receiptQuantity <= 0) throw new InvalidOperationException("Receipt quantity must be greater than zero.");
        if (currentImportPrice < 0) throw new InvalidOperationException("Current import price cannot be negative.");
        if (receiptUnitPrice < 0) throw new InvalidOperationException("Receipt unit price cannot be negative.");

        if (currentQuantityOnHand <= 0) return decimal.Round(receiptUnitPrice, 2, MidpointRounding.AwayFromZero);

        var totalQuantity = currentQuantityOnHand + receiptQuantity;
        var totalCost = currentQuantityOnHand * currentImportPrice + receiptQuantity * receiptUnitPrice;

        return decimal.Round(totalCost / totalQuantity, 2, MidpointRounding.AwayFromZero);
    }
}
