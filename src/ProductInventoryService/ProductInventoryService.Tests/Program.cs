using ProductInventoryService.Application.Features.Inventory;
using ProductInventoryService.Application.Features.Products;
using ProductInventoryService.Application.Models;

var tests = new List<(string Name, Action Test)>
{
    ("marks empty stock as out of stock and recommends max refill", MarksEmptyStockAsOutOfStock),
    ("marks available stock below half threshold as critical", MarksAvailableStockBelowHalfThresholdAsCritical),
    ("marks available stock below threshold as low", MarksAvailableStockBelowThresholdAsLow),
    ("marks stock over max threshold as overstock", MarksStockOverMaxThresholdAsOverstock),
    ("calculates positive stocktake adjustment", CalculatesPositiveStocktakeAdjustment),
    ("calculates negative stocktake adjustment", CalculatesNegativeStocktakeAdjustment),
    ("matches product search by barcode for receipt entry", MatchesProductSearchByBarcodeForReceiptEntry),
    ("calculates weighted average import price after goods receipt", CalculatesWeightedAverageImportPriceAfterGoodsReceipt),
    ("formats inventory balance report CSV correctly", FormatsInventoryBalanceReportCsvCorrectly),
};

foreach (var test in tests)
{
    try
    {
        test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
        Environment.ExitCode = 1;
        return;
    }
}

static void MarksEmptyStockAsOutOfStock()
{
    var result = StockAlertPolicy.Calculate(new StockAlertInput(
        QuantityOnHand: 0,
        QuantityReserved: 0,
        MinThreshold: 10,
        MaxThreshold: 100));

    AssertEqual("OutOfStock", result.AlertLevel, nameof(result.AlertLevel));
    AssertEqual(100, result.RecommendedOrderQuantity, nameof(result.RecommendedOrderQuantity));
    AssertEqual(10, result.ReorderQuantity, nameof(result.ReorderQuantity));
}

static void MarksAvailableStockBelowHalfThresholdAsCritical()
{
    var result = StockAlertPolicy.Calculate(new StockAlertInput(
        QuantityOnHand: 8,
        QuantityReserved: 4,
        MinThreshold: 10,
        MaxThreshold: 60));

    AssertEqual("Critical", result.AlertLevel, nameof(result.AlertLevel));
    AssertEqual(52, result.RecommendedOrderQuantity, nameof(result.RecommendedOrderQuantity));
    AssertEqual(6, result.ReorderQuantity, nameof(result.ReorderQuantity));
}

static void MarksAvailableStockBelowThresholdAsLow()
{
    var result = StockAlertPolicy.Calculate(new StockAlertInput(
        QuantityOnHand: 12,
        QuantityReserved: 3,
        MinThreshold: 10,
        MaxThreshold: 40));

    AssertEqual("Low", result.AlertLevel, nameof(result.AlertLevel));
    AssertEqual(28, result.RecommendedOrderQuantity, nameof(result.RecommendedOrderQuantity));
    AssertEqual(1, result.ReorderQuantity, nameof(result.ReorderQuantity));
}

static void MarksStockOverMaxThresholdAsOverstock()
{
    var result = StockAlertPolicy.Calculate(new StockAlertInput(
        QuantityOnHand: 120,
        QuantityReserved: 0,
        MinThreshold: 10,
        MaxThreshold: 100));

    AssertEqual("Overstock", result.AlertLevel, nameof(result.AlertLevel));
    AssertEqual(0, result.RecommendedOrderQuantity, nameof(result.RecommendedOrderQuantity));
    AssertEqual(0, result.ReorderQuantity, nameof(result.ReorderQuantity));
}

static void CalculatesPositiveStocktakeAdjustment()
{
    var result = StocktakeAdjustmentPolicy.Calculate(
        systemQuantity: 12,
        countedQuantity: 15);

    AssertEqual(3, result.VarianceQuantity, nameof(result.VarianceQuantity));
    AssertEqual(15, result.QuantityAfter, nameof(result.QuantityAfter));
    AssertEqual("Increase", result.Direction, nameof(result.Direction));
}

static void CalculatesNegativeStocktakeAdjustment()
{
    var result = StocktakeAdjustmentPolicy.Calculate(
        systemQuantity: 12,
        countedQuantity: 8);

    AssertEqual(-4, result.VarianceQuantity, nameof(result.VarianceQuantity));
    AssertEqual(8, result.QuantityAfter, nameof(result.QuantityAfter));
    AssertEqual("Decrease", result.Direction, nameof(result.Direction));
}

static void MatchesProductSearchByBarcodeForReceiptEntry()
{
    var product = new Product
    {
        Code = "SKU-MILK-001",
        Name = "Sua tuoi hop 1L",
        Barcode = "8934567890123"
    };

    var result = ProductSearchPolicy.Matches(product, "8934567890123");

    AssertEqual(true, result, "barcode search result");
}

static void CalculatesWeightedAverageImportPriceAfterGoodsReceipt()
{
    var result = InventoryCostPolicy.CalculateWeightedAverageImportPrice(
        currentQuantityOnHand: 10,
        currentImportPrice: 3_000m,
        receiptQuantity: 5,
        receiptUnitPrice: 3_600m);

    AssertEqual(3_200m, result, "weighted average import price");
}

static void FormatsInventoryBalanceReportCsvCorrectly()
{
    var reports = new List<ProductInventoryService.Application.DTOs.InventoryBalanceReportDto>
    {
        new()
        {
            ProductId = Guid.Parse("d3b07384-d113-4416-a517-e7d6363c4e33"),
            ProductCode = "PROD-A",
            ProductName = "Product A",
            UnitName = "cai",
            OpeningStock = 10,
            ReceivedQuantity = 5,
            ShippedQuantity = 3,
            ClosingStock = 12
        }
    };

    var csv = InventoryCsvService.ToBalanceReportCsv(reports);
    var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    AssertEqual(2, lines.Length, "csv lines length");
    AssertEqual("ProductId,ProductCode,ProductName,UnitName,OpeningStock,ReceivedQuantity,ShippedQuantity,ClosingStock", lines[0], "csv header");
    AssertEqual("d3b07384-d113-4416-a517-e7d6363c4e33,PROD-A,Product A,cai,10,5,3,12", lines[1], "csv row content");
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'");
    }
}
