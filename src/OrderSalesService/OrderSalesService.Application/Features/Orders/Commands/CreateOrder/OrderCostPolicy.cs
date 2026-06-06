namespace OrderSalesService.Application.Features.Orders.Commands.CreateOrder;

public static class OrderCostPolicy
{
    public static decimal CalculateLineCost(int quantity, decimal costPrice)
    {
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");
        if (costPrice < 0) throw new InvalidOperationException("Cost price cannot be negative.");

        return quantity * costPrice;
    }
}
