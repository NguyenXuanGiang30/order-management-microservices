namespace OrderSalesService.Application.Features.Orders.Commands.CreateOrder;

public interface IProductCostReader
{
    Task<decimal> GetCostPriceAsync(Guid productId, CancellationToken cancellationToken);
}
