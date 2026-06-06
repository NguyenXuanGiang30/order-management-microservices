namespace ProductInventoryService.Application.Features.Inventory;

public record StocktakeAdjustmentResult(int VarianceQuantity, int QuantityAfter, string Direction);

public static class StocktakeAdjustmentPolicy
{
    public static StocktakeAdjustmentResult Calculate(int systemQuantity, int countedQuantity)
    {
        if (countedQuantity < 0)
        {
            throw new InvalidOperationException("Counted quantity cannot be negative.");
        }

        var variance = countedQuantity - systemQuantity;
        var direction = variance switch
        {
            > 0 => "Increase",
            < 0 => "Decrease",
            _ => "NoChange"
        };

        return new StocktakeAdjustmentResult(variance, countedQuantity, direction);
    }
}
