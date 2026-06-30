using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Features.Promotions;

public record PromotionOrderLine(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal SubTotal);

public record PromotionCalculationResult(
    decimal EligibleAmount,
    decimal DiscountAmount);

public static class PromotionPolicy
{
    public static PromotionCalculationResult Calculate(
        Promotion promotion,
        IReadOnlyCollection<PromotionOrderLine> lines,
        DateTime nowUtc)
    {
        if (!promotion.IsActive)
        {
            throw new InvalidOperationException("Khuyen mai khong con hoat dong.");
        }

        if (nowUtc < promotion.StartAt || nowUtc > promotion.EndAt)
        {
            throw new InvalidOperationException("Khuyen mai khong nam trong thoi gian ap dung.");
        }

        if (string.Equals(promotion.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase) &&
            (promotion.DiscountValue < 0 || promotion.DiscountValue > 100))
        {
            throw new InvalidOperationException("Phan tram giam gia khong hop le.");
        }

        if (string.Equals(promotion.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase) &&
            promotion.DiscountValue < 0)
        {
            throw new InvalidOperationException("So tien giam gia khong hop le.");
        }

        var orderSubTotal = lines.Sum(line => line.SubTotal);
        if (promotion.MinimumOrderAmount > 0 && orderSubTotal < promotion.MinimumOrderAmount)
        {
            throw new InvalidOperationException("Don hang chua dat gia tri toi thieu de ap dung khuyen mai.");
        }

        var eligibleAmount = GetEligibleAmount(promotion, lines);
        if (eligibleAmount <= 0)
        {
            throw new InvalidOperationException("Don hang khong du dieu kien ap dung khuyen mai.");
        }

        var discountAmount = CalculateDiscountAmount(promotion, eligibleAmount);
        return new PromotionCalculationResult(eligibleAmount, Math.Min(eligibleAmount, discountAmount));
    }

    private static decimal GetEligibleAmount(Promotion promotion, IReadOnlyCollection<PromotionOrderLine> lines)
    {
        if (string.Equals(promotion.PromotionType, "Order", StringComparison.OrdinalIgnoreCase))
        {
            return lines.Sum(line => line.SubTotal);
        }

        if (string.Equals(promotion.PromotionType, "Product", StringComparison.OrdinalIgnoreCase))
        {
            var productIds = promotion.PromotionItems.Select(item => item.ProductId).ToHashSet();
            return lines
                .Where(line => productIds.Contains(line.ProductId))
                .Sum(line => line.SubTotal);
        }

        if (string.Equals(promotion.PromotionType, "Combo", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var requiredItem in promotion.PromotionItems)
            {
                var line = lines.FirstOrDefault(item => item.ProductId == requiredItem.ProductId);
                if (line == null || line.Quantity < requiredItem.RequiredQuantity)
                {
                    throw new InvalidOperationException("Don hang chua du san pham trong combo khuyen mai.");
                }
            }

            var comboProductIds = promotion.PromotionItems.Select(item => item.ProductId).ToHashSet();
            return lines
                .Where(line => comboProductIds.Contains(line.ProductId))
                .Sum(line => line.SubTotal);
        }

        if (string.Equals(promotion.PromotionType, "BuyXGetY", StringComparison.OrdinalIgnoreCase))
        {
            if (promotion.PromotionItems.Count < 2)
            {
                throw new InvalidOperationException("Khuyen mai BuyXGetY phai co it nhat 2 san pham (san pham mua va san pham tang).");
            }

            var itemsList = promotion.PromotionItems.ToList();
            var buyItem = itemsList[0];
            var getItem = itemsList[1];

            var buyLine = lines.FirstOrDefault(item => item.ProductId == buyItem.ProductId);
            var getLine = lines.FirstOrDefault(item => item.ProductId == getItem.ProductId);

            if (buyLine == null || buyLine.Quantity < buyItem.RequiredQuantity)
            {
                throw new InvalidOperationException($"Chua du so luong san pham mua yeu cau ({buyItem.ProductName}).");
            }

            if (getLine == null || getLine.Quantity < getItem.RequiredQuantity)
            {
                throw new InvalidOperationException($"Chua du so luong san pham tang yeu cau ({getItem.ProductName}).");
            }

            var multiplier = buyLine.Quantity / buyItem.RequiredQuantity;
            var actualGiftQty = Math.Min(multiplier * getItem.RequiredQuantity, getLine.Quantity);
            var giftUnitPrice = getLine.SubTotal / getLine.Quantity;

            return actualGiftQty * giftUnitPrice;
        }

        throw new InvalidOperationException("Loai khuyen mai khong hop le.");
    }

    private static decimal CalculateDiscountAmount(Promotion promotion, decimal eligibleAmount)
    {
        if (string.Equals(promotion.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase))
        {
            return eligibleAmount * promotion.DiscountValue / 100m;
        }

        if (string.Equals(promotion.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
        {
            return promotion.DiscountValue;
        }

        throw new InvalidOperationException("Loai giam gia khong hop le.");
    }
}
