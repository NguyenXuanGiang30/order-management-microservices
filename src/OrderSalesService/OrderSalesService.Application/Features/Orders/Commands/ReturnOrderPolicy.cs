using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Orders.Commands;

public record ReturnOrderPlan(decimal TotalRefundAmount, List<ReturnOrderPlanItem> Items);

public record ReturnOrderPlanItem(
    Guid OrderDetailId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int ReturnQuantity,
    decimal RefundAmount);

public static class ReturnOrderPolicy
{
    public static ReturnOrderPlan ValidateAndBuild(Order order, IEnumerable<ReturnItemDto> requestedItems)
    {
        var groupedItems = requestedItems
            .GroupBy(i => i.OrderDetailId)
            .Select(g => new ReturnItemDto(g.Key, g.Sum(i => i.ReturnQuantity)))
            .ToList();

        if (groupedItems.Count == 0)
        {
            throw new InvalidOperationException("Phiếu trả hàng phải có ít nhất một sản phẩm.");
        }

        var result = new List<ReturnOrderPlanItem>();

        foreach (var item in groupedItems)
        {
            if (item.ReturnQuantity <= 0)
            {
                throw new InvalidOperationException("Số lượng trả phải lớn hơn 0.");
            }

            var detail = order.OrderDetails.FirstOrDefault(d => d.Id == item.OrderDetailId);
            if (detail == null)
            {
                throw new InvalidOperationException("Sản phẩm trả không thuộc đơn hàng gốc.");
            }

            var alreadyReturned = order.ReturnOrders
                .Where(r => !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                .SelectMany(r => r.ReturnOrderDetails)
                .Where(d => d.OrderDetailId == detail.Id)
                .Sum(d => d.ReturnQuantity);

            var availableQuantity = detail.Quantity - alreadyReturned;
            if (item.ReturnQuantity > availableQuantity)
            {
                throw new InvalidOperationException(
                    $"Sản phẩm '{detail.ProductName}' chỉ còn {availableQuantity} sản phẩm có thể trả.");
            }

            var unitRefundAmount = detail.Quantity == 0 ? 0 : detail.SubTotal / detail.Quantity;
            var refundAmount = unitRefundAmount * item.ReturnQuantity;

            result.Add(new ReturnOrderPlanItem(
                detail.Id,
                detail.ProductId,
                detail.ProductCode,
                detail.ProductName,
                item.ReturnQuantity,
                refundAmount));
        }

        return new ReturnOrderPlan(result.Sum(i => i.RefundAmount), result);
    }

    public static bool IsFullyReturned(Order order, ReturnOrderPlan newReturn)
    {
        foreach (var detail in order.OrderDetails)
        {
            var alreadyReturned = order.ReturnOrders
                .Where(r => !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                .SelectMany(r => r.ReturnOrderDetails)
                .Where(d => d.OrderDetailId == detail.Id)
                .Sum(d => d.ReturnQuantity);

            var justReturned = newReturn.Items
                .Where(i => i.OrderDetailId == detail.Id)
                .Sum(i => i.ReturnQuantity);

            if (alreadyReturned + justReturned < detail.Quantity)
            {
                return false;
            }
        }

        return true;
    }
}
