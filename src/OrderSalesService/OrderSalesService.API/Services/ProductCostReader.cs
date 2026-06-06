using System.Net.Http.Json;
using OrderSalesService.Application.Features.Orders.Commands.CreateOrder;

namespace OrderSalesService.API.Services;

public class ProductCostReader : IProductCostReader
{
    private readonly HttpClient _httpClient;

    public ProductCostReader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetCostPriceAsync(Guid productId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/internal/products/{productId}/price-check", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Cannot resolve cost for product {productId}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<ProductPriceCheckResponse>(cancellationToken: cancellationToken);
        if (payload == null)
        {
            throw new InvalidOperationException($"Product cost response is empty for product {productId}.");
        }

        return payload.ImportPrice;
    }

    private sealed record ProductPriceCheckResponse(decimal ImportPrice);
}
