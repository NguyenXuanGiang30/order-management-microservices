using System.Globalization;
using System.Text;

namespace ProductInventoryService.Application.Features.Inventory;

public record ImportedStocktakeCount(Guid? ProductId, string ProductCode, int CountedQuantity, string? Note);

public static class InventoryCsvService
{
    public static string ToStockCsv(IEnumerable<StockDto> rows)
    {
        var builder = NewBuilder("ProductId,ProductCode,ProductName,UnitName,QuantityOnHand,QuantityReserved,AvailableQuantity,MinThreshold,MaxThreshold,AlertLevel,RecommendedOrderQuantity");
        foreach (var row in rows)
        {
            builder.AppendLine(JoinCsv(
                row.ProductId.ToString(),
                row.ProductCode,
                row.ProductName,
                row.UnitName,
                row.QuantityOnHand.ToString(CultureInfo.InvariantCulture),
                row.QuantityReserved.ToString(CultureInfo.InvariantCulture),
                row.AvailableQuantity.ToString(CultureInfo.InvariantCulture),
                row.MinThreshold.ToString(CultureInfo.InvariantCulture),
                row.MaxThreshold.ToString(CultureInfo.InvariantCulture),
                row.AlertLevel,
                row.RecommendedOrderQuantity.ToString(CultureInfo.InvariantCulture)));
        }

        return builder.ToString();
    }

    public static string ToTransactionsCsv(IEnumerable<InventoryTransactionDto> rows)
    {
        var builder = NewBuilder("Id,ProductId,ProductName,TransactionType,QuantityChange,QuantityAfter,ReferenceType,ReferenceId,Note,CreatedAt");
        foreach (var row in rows)
        {
            builder.AppendLine(JoinCsv(
                row.Id.ToString(),
                row.ProductId.ToString(),
                row.ProductName,
                row.TransactionType,
                row.QuantityChange.ToString(CultureInfo.InvariantCulture),
                row.QuantityAfter.ToString(CultureInfo.InvariantCulture),
                row.ReferenceType,
                row.ReferenceId?.ToString() ?? "",
                row.Note ?? "",
                row.CreatedAt.ToString("O", CultureInfo.InvariantCulture)));
        }

        return builder.ToString();
    }

    public static string ToStocktakeTemplateCsv(StocktakeSessionDto session)
    {
        var builder = NewBuilder("LineId,ProductId,ProductCode,ProductName,UnitName,SystemQuantity,CountedQuantity,Note");
        foreach (var line in session.Lines)
        {
            builder.AppendLine(JoinCsv(
                line.Id.ToString(),
                line.ProductId.ToString(),
                line.ProductCode,
                line.ProductName,
                line.UnitName,
                line.SystemQuantity.ToString(CultureInfo.InvariantCulture),
                line.CountedQuantity?.ToString(CultureInfo.InvariantCulture) ?? "",
                line.Note ?? ""));
        }

        return builder.ToString();
    }

    public static IReadOnlyList<ImportedStocktakeCount> ParseStocktakeCounts(string csvContent)
    {
        var rows = ParseRows(csvContent).ToList();
        if (rows.Count <= 1) return [];

        var header = rows[0].Select(NormalizeHeader).ToList();
        var productIdIndex = header.IndexOf("productid");
        var productCodeIndex = header.IndexOf("productcode");
        var countedQuantityIndex = header.IndexOf("countedquantity");
        var noteIndex = header.IndexOf("note");

        if (countedQuantityIndex < 0)
        {
            throw new InvalidOperationException("CSV must contain CountedQuantity column.");
        }

        var imported = new List<ImportedStocktakeCount>();
        foreach (var row in rows.Skip(1))
        {
            if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

            var productIdText = Get(row, productIdIndex);
            Guid? productId = Guid.TryParse(productIdText, out var parsedProductId) ? parsedProductId : null;
            var productCode = Get(row, productCodeIndex);
            var countedQuantityText = Get(row, countedQuantityIndex);

            if (!int.TryParse(countedQuantityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var countedQuantity))
            {
                throw new InvalidOperationException($"Invalid CountedQuantity '{countedQuantityText}'.");
            }

            imported.Add(new ImportedStocktakeCount(productId, productCode, countedQuantity, Get(row, noteIndex)));
        }

        return imported;
    }

    public static byte[] ToUtf8BomBytes(string csv)
    {
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
    }

    private static StringBuilder NewBuilder(string header)
    {
        var builder = new StringBuilder();
        builder.AppendLine(header);
        return builder;
    }

    private static string JoinCsv(params string[] values)
    {
        return string.Join(",", values.Select(Escape));
    }

    private static string Escape(string value)
    {
        if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static IEnumerable<List<string>> ParseRows(string csvContent)
    {
        using var reader = new StringReader(csvContent.TrimStart('\ufeff'));
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return ParseLine(line);
        }
    }

    private static List<string> ParseLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"' && inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
                continue;
            }

            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static string NormalizeHeader(string value)
    {
        return value.Trim().Replace(" ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    }

    private static string Get(IReadOnlyList<string> row, int index)
    {
        return index >= 0 && index < row.Count ? row[index].Trim() : "";
    }
}
