namespace ProductInventoryService.Application.Features.Inventory;

public record StockAlertInput(int QuantityOnHand, int QuantityReserved, int MinThreshold, int MaxThreshold);

public record StockAlertResult(
    string AlertLevel,
    int ReorderQuantity,
    int RecommendedOrderQuantity,
    string StockCoverageLabel);

public static class StockAlertPolicy
{
    public static StockAlertResult Calculate(StockAlertInput input)
    {
        var quantityOnHand = Math.Max(0, input.QuantityOnHand);
        var quantityReserved = Math.Max(0, input.QuantityReserved);
        var minThreshold = Math.Max(0, input.MinThreshold);
        var maxThreshold = input.MaxThreshold > 0 ? input.MaxThreshold : Math.Max(minThreshold * 2, quantityOnHand);
        var availableQuantity = Math.Max(0, quantityOnHand - quantityReserved);

        var alertLevel = CalculateAlertLevel(quantityOnHand, availableQuantity, minThreshold, maxThreshold);
        var shouldReorder = alertLevel is "OutOfStock" or "Critical" or "Low";
        var reorderQuantity = shouldReorder ? Math.Max(0, minThreshold - availableQuantity) : 0;
        var recommendedOrderQuantity = shouldReorder ? Math.Max(0, maxThreshold - quantityOnHand) : 0;

        return new StockAlertResult(
            alertLevel,
            reorderQuantity,
            recommendedOrderQuantity,
            BuildCoverageLabel(alertLevel, availableQuantity, minThreshold, maxThreshold));
    }

    private static string CalculateAlertLevel(int quantityOnHand, int availableQuantity, int minThreshold, int maxThreshold)
    {
        if (quantityOnHand <= 0 || availableQuantity <= 0) return "OutOfStock";
        if (maxThreshold > 0 && quantityOnHand > maxThreshold) return "Overstock";
        if (minThreshold <= 0) return "Healthy";
        if (availableQuantity <= Math.Ceiling(minThreshold / 2m)) return "Critical";
        if (availableQuantity < minThreshold) return "Low";
        return "Healthy";
    }

    private static string BuildCoverageLabel(string alertLevel, int availableQuantity, int minThreshold, int maxThreshold)
    {
        return alertLevel switch
        {
            "OutOfStock" => "No available stock",
            "Critical" => $"Critical: {availableQuantity}/{minThreshold} available",
            "Low" => $"Low: {availableQuantity}/{minThreshold} available",
            "Overstock" => $"Over max: {availableQuantity}/{maxThreshold}",
            _ => $"Healthy: {availableQuantity} available"
        };
    }
}
