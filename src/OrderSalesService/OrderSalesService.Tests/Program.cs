using OrderSalesService.Application.Features.Payments.BankTransfers;
using OrderSalesService.Application.Features.Orders.Commands;
using OrderSalesService.Application.Features.Orders.Commands.CreateOrder;
using OrderSalesService.Application.Features.Promotions;
using OrderSalesService.Application.Features.Shifts;
using OrderSalesService.Application.Features.Suppliers;
using OrderSalesService.Application.Models;

var tests = new List<(string Name, Action Test)>
{
    ("extracts order code from SePay transfer content", ExtractsOrderCode),
    ("extracts lowercase order code", ExtractsLowercaseOrderCode),
    ("returns null when transfer content has no order code", ReturnsNullWhenNoOrderCode),
    ("marks order paid when bank transfer covers debt", MarksOrderPaid),
    ("marks order partially paid when bank transfer is below debt", MarksOrderPartiallyPaid),
    ("calculates valid return refund", CalculatesValidReturnRefund),
    ("rejects return quantity above available quantity", RejectsReturnQuantityAboveAvailableQuantity),
    ("applies order percent promotion", AppliesOrderPercentPromotion),
    ("clamps fixed promotion discount to eligible amount", ClampsFixedPromotionDiscount),
    ("applies product promotion only to matching items", AppliesProductPromotionOnlyToMatchingItems),
    ("requires all combo products", RequiresAllComboProducts),
    ("rejects inactive promotion", RejectsInactivePromotion),
    ("calculates cash shift closing variance", CalculatesCashShiftClosingVariance),
    ("applies confirmed goods receipt to supplier debt", AppliesConfirmedGoodsReceiptToSupplierDebt),
    ("calculates order line cost snapshot", CalculatesOrderLineCostSnapshot),
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

static void ExtractsOrderCode()
{
    var result = BankTransferPaymentMatcher.ExtractOrderCode("SEPAY thanh toan DH-20260529183000123 tai quay");

    AssertEqual("DH-20260529183000123", result, nameof(result));
}

static void ExtractsLowercaseOrderCode()
{
    var result = BankTransferPaymentMatcher.ExtractOrderCode("noi dung: dh-20260529183000123");

    AssertEqual("DH-20260529183000123", result, nameof(result));
}

static void ReturnsNullWhenNoOrderCode()
{
    var result = BankTransferPaymentMatcher.ExtractOrderCode("SEPAY thanh toan khong co ma don");

    AssertEqual<string?>(null, result, nameof(result));
}

static void MarksOrderPaid()
{
    var order = NewOrder(finalAmount: 250_000m, paidAmount: 0m);
    var customer = NewCustomer(debtAmount: 250_000m);

    var payment = PaymentSettlement.ApplyBankTransfer(
        order,
        customer,
        amount: 250_000m,
        externalTransactionId: "SEPAY-001",
        receivedAt: new DateTime(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc));

    AssertEqual(250_000m, payment.Amount, nameof(payment.Amount));
    AssertEqual("Chuyển khoản", payment.PaymentMethod, nameof(payment.PaymentMethod));
    AssertEqual("Paid", order.Status, nameof(order.Status));
    AssertEqual(250_000m, order.PaidAmount, nameof(order.PaidAmount));
    AssertEqual(0m, order.DebtAmount, nameof(order.DebtAmount));
    AssertEqual(0m, customer.DebtAmount, nameof(customer.DebtAmount));
    AssertEqual(1, order.OrderStatusHistories.Count, nameof(order.OrderStatusHistories.Count));
}

static void MarksOrderPartiallyPaid()
{
    var order = NewOrder(finalAmount: 250_000m, paidAmount: 0m);
    var customer = NewCustomer(debtAmount: 250_000m);

    PaymentSettlement.ApplyBankTransfer(
        order,
        customer,
        amount: 100_000m,
        externalTransactionId: "SEPAY-002",
        receivedAt: new DateTime(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc));

    AssertEqual("PartialPaid", order.Status, nameof(order.Status));
    AssertEqual(100_000m, order.PaidAmount, nameof(order.PaidAmount));
    AssertEqual(150_000m, order.DebtAmount, nameof(order.DebtAmount));
    AssertEqual(150_000m, customer.DebtAmount, nameof(customer.DebtAmount));
}

static void CalculatesValidReturnRefund()
{
    var order = NewOrder(finalAmount: 300_000m, paidAmount: 300_000m);
    var detail = NewOrderDetail(quantity: 3, subTotal: 300_000m);
    order.OrderDetails.Add(detail);

    var result = ReturnOrderPolicy.ValidateAndBuild(
        order,
        new[] { new ReturnItemDto(detail.Id, 2) });

    AssertEqual(200_000m, result.TotalRefundAmount, nameof(result.TotalRefundAmount));
    AssertEqual(2, result.Items[0].ReturnQuantity, nameof(result.Items));
}

static void RejectsReturnQuantityAboveAvailableQuantity()
{
    var order = NewOrder(finalAmount: 300_000m, paidAmount: 300_000m);
    var detail = NewOrderDetail(quantity: 3, subTotal: 300_000m);
    order.OrderDetails.Add(detail);
    order.ReturnOrders.Add(new ReturnOrder
    {
        Status = "Completed",
        ReturnOrderDetails =
        {
            new ReturnOrderDetail
            {
                OrderDetailId = detail.Id,
                ProductId = detail.ProductId,
                ReturnQuantity = 2,
                RefundAmount = 200_000m
            }
        }
    });

    AssertThrows<InvalidOperationException>(() =>
        ReturnOrderPolicy.ValidateAndBuild(order, new[] { new ReturnItemDto(detail.Id, 2) }));
}

static void CalculatesCashShiftClosingVariance()
{
    var result = CashShiftCalculator.CalculateClosing(
        openingCash: 500_000m,
        cashPayments: 250_000m,
        actualCash: 760_000m);

    AssertEqual(750_000m, result.ExpectedCash, nameof(result.ExpectedCash));
    AssertEqual(10_000m, result.Variance, nameof(result.Variance));
}

static void AppliesConfirmedGoodsReceiptToSupplierDebt()
{
    var supplier = new Supplier
    {
        Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
        Code = "NCC001",
        Name = "Test Supplier",
        DebtAmount = 120_000m
    };

    var updatedDebt = SupplierDebtPolicy.ApplyConfirmedGoodsReceipt(
        supplier,
        receiptAmount: 380_000m);

    AssertEqual(500_000m, updatedDebt, nameof(updatedDebt));
    AssertEqual(500_000m, supplier.DebtAmount, nameof(supplier.DebtAmount));
}

static void CalculatesOrderLineCostSnapshot()
{
    var result = OrderCostPolicy.CalculateLineCost(quantity: 3, costPrice: 12_000m);

    AssertEqual(36_000m, result, nameof(result));
}

static void AppliesOrderPercentPromotion()
{
    var promotion = NewPromotion("Order", "Percent", 10m);

    var result = PromotionPolicy.Calculate(
        promotion,
        new[] { NewPromotionLine("50000000-0000-0000-0000-000000000001", 2, 100_000m) },
        ActivePromotionNow());

    AssertEqual(20_000m, result.DiscountAmount, nameof(result.DiscountAmount));
    AssertEqual(200_000m, result.EligibleAmount, nameof(result.EligibleAmount));
}

static void ClampsFixedPromotionDiscount()
{
    var promotion = NewPromotion("Order", "FixedAmount", 300_000m);

    var result = PromotionPolicy.Calculate(
        promotion,
        new[] { NewPromotionLine("50000000-0000-0000-0000-000000000001", 1, 120_000m) },
        ActivePromotionNow());

    AssertEqual(120_000m, result.DiscountAmount, nameof(result.DiscountAmount));
}

static void AppliesProductPromotionOnlyToMatchingItems()
{
    var matchingProductId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    var promotion = NewPromotion("Product", "Percent", 25m);
    promotion.PromotionItems.Add(new PromotionItem
    {
        ProductId = matchingProductId,
        ProductCode = "SP001",
        ProductName = "Sáº£n pháº©m khuyáº¿n mÃ£i",
        RequiredQuantity = 1
    });

    var result = PromotionPolicy.Calculate(
        promotion,
        new[]
        {
            NewPromotionLine(matchingProductId.ToString(), 1, 200_000m),
            NewPromotionLine("60000000-0000-0000-0000-000000000001", 1, 500_000m)
        },
        ActivePromotionNow());

    AssertEqual(50_000m, result.DiscountAmount, nameof(result.DiscountAmount));
    AssertEqual(200_000m, result.EligibleAmount, nameof(result.EligibleAmount));
}

static void RequiresAllComboProducts()
{
    var promotion = NewPromotion("Combo", "FixedAmount", 40_000m);
    promotion.PromotionItems.Add(new PromotionItem
    {
        ProductId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
        ProductCode = "SP001",
        ProductName = "Sáº£n pháº©m A",
        RequiredQuantity = 1
    });
    promotion.PromotionItems.Add(new PromotionItem
    {
        ProductId = Guid.Parse("60000000-0000-0000-0000-000000000001"),
        ProductCode = "SP002",
        ProductName = "Sáº£n pháº©m B",
        RequiredQuantity = 2
    });

    AssertThrows<InvalidOperationException>(() =>
        PromotionPolicy.Calculate(
            promotion,
            new[] { NewPromotionLine("50000000-0000-0000-0000-000000000001", 1, 100_000m) },
            ActivePromotionNow()));
}

static void RejectsInactivePromotion()
{
    var promotion = NewPromotion("Order", "Percent", 10m);
    promotion.IsActive = false;

    AssertThrows<InvalidOperationException>(() =>
        PromotionPolicy.Calculate(
            promotion,
            new[] { NewPromotionLine("50000000-0000-0000-0000-000000000001", 1, 100_000m) },
            ActivePromotionNow()));
}

static Order NewOrder(decimal finalAmount, decimal paidAmount)
{
    return new Order
    {
        Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        OrderCode = "DH-20260529183000123",
        CustomerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
        CustomerName = "Khách lẻ",
        CreatedBy = Guid.Parse("30000000-0000-0000-0000-000000000001"),
        CreatedByName = "System",
        OrderDate = new DateTime(2026, 5, 29, 11, 55, 0, DateTimeKind.Utc),
        FinalAmount = finalAmount,
        PaidAmount = paidAmount,
        DebtAmount = finalAmount - paidAmount,
        Status = "Pending"
    };
}

static OrderDetail NewOrderDetail(int quantity, decimal subTotal)
{
    return new OrderDetail
    {
        Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
        OrderId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        ProductId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
        ProductCode = "SP001",
        ProductName = "Sản phẩm test",
        UnitName = "Cái",
        UnitPrice = subTotal / quantity,
        Quantity = quantity,
        DiscountPercent = 0,
        SubTotal = subTotal
    };
}

static Customer NewCustomer(decimal debtAmount)
{
    return new Customer
    {
        Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
        Code = "C001",
        FullName = "Khách lẻ",
        DebtAmount = debtAmount
    };
}

static Promotion NewPromotion(string promotionType, string discountType, decimal discountValue)
{
    var now = ActivePromotionNow();
    return new Promotion
    {
        Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
        Code = "KMTEST",
        Name = "Khuyáº¿n mÃ£i test",
        PromotionType = promotionType,
        DiscountType = discountType,
        DiscountValue = discountValue,
        MinimumOrderAmount = 0m,
        StartAt = now.AddDays(-1),
        EndAt = now.AddDays(1),
        IsActive = true,
        CreatedBy = Guid.Parse("30000000-0000-0000-0000-000000000001")
    };
}

static PromotionOrderLine NewPromotionLine(string productId, int quantity, decimal unitPrice)
{
    return new PromotionOrderLine(
        Guid.Parse(productId),
        $"SP-{productId[..4]}",
        $"Sáº£n pháº©m {productId[..4]}",
        quantity,
        unitPrice * quantity);
}

static DateTime ActivePromotionNow() => new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'");
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name} was not thrown.");
}
