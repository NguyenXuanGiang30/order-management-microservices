using ProductInventoryService.Application.Models;

namespace ProductInventoryService.Application.Features.Products;

public static class ProductSearchPolicy
{
    public static bool Matches(Product product, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;

        var normalizedSearch = search.Trim();

        return Contains(product.Name, normalizedSearch)
            || Contains(product.Code, normalizedSearch)
            || Contains(product.Barcode, normalizedSearch);
    }

    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
