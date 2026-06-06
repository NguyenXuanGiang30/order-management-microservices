using MediatR;
using MassTransit;
using SharedContracts.Events;
using OrderSalesService.Application.Models;
using OrderSalesService.Application.Interfaces;
using OrderSalesService.Application.Common;
using OrderSalesService.Application.Features.Promotions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace OrderSalesService.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IStockCache _stockCache;
    private readonly IProductCostReader _productCostReader;

    public CreateOrderCommandHandler(IOrderSalesDbContext context, IPublishEndpoint publishEndpoint, IStockCache stockCache, IProductCostReader productCostReader)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _stockCache = stockCache;
        _productCostReader = productCostReader;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Kiểm tra tồn kho trước khi đặt hàng qua StockCache
        foreach (var item in request.Items)
        {
            if (!_stockCache.IsInStock(item.ProductId, item.Quantity))
            {
                var currentStock = _stockCache.GetStock(item.ProductId);
                var currentStockStr = currentStock >= 0 ? currentStock.ToString() : "0 (Hết hàng)";
                
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.Items), $"Sản phẩm '{item.ProductName}' không đủ tồn kho! Số lượng yêu cầu: {item.Quantity}, Tồn kho hiện hành: {currentStockStr}")
                });
            }
        }

        var orderCode = $"DH-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        var costsByProductId = new Dictionary<Guid, decimal>();
        foreach (var item in request.Items)
        {
            costsByProductId[item.ProductId] = await _productCostReader.GetCostPriceAsync(item.ProductId, cancellationToken);
        }

        var details = request.Items.Select(item =>
        {
            var subTotal = item.UnitPrice * item.Quantity;
            var discountAmount = subTotal * item.DiscountPercent / 100;
            var costPrice = costsByProductId[item.ProductId];
            return new OrderDetail
            {
                ProductId = item.ProductId, ProductCode = item.ProductCode, ProductName = item.ProductName,
                UnitName = item.UnitName, UnitPrice = item.UnitPrice, Quantity = item.Quantity,
                CostPrice = costPrice, CostTotal = OrderCostPolicy.CalculateLineCost(item.Quantity, costPrice),
                DiscountPercent = item.DiscountPercent, SubTotal = subTotal - discountAmount
            };
        }).ToList();

        var totalSubTotal = details.Sum(d => d.SubTotal);
        Promotion? promotion = null;
        PromotionCalculationResult? promotionResult = null;

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var promotionCode = PromotionMapper.NormalizeCode(request.PromotionCode);
            promotion = await _context.Promotions
                .Include(p => p.PromotionItems)
                .FirstOrDefaultAsync(p => p.Code == promotionCode, cancellationToken);

            if (promotion == null)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.PromotionCode), "Khong tim thay ma khuyen mai.")
                });
            }

            try
            {
                promotionResult = PromotionPolicy.Calculate(
                    promotion,
                    details.Select(detail => new PromotionOrderLine(
                        detail.ProductId,
                        detail.ProductCode,
                        detail.ProductName,
                        detail.Quantity,
                        detail.SubTotal)).ToList(),
                    DateTime.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.PromotionCode), ex.Message)
                });
            }
        }

        var promotionDiscountAmount = promotionResult?.DiscountAmount ?? 0m;
        var finalAmount = Math.Max(0, totalSubTotal - promotionDiscountAmount);

        var order = new Order
        {
            OrderCode = orderCode, CustomerId = request.CustomerId, CustomerName = request.CustomerName,
            CreatedBy = request.CreatedBy, CreatedByName = request.CreatedByName, OrderDate = DateTime.UtcNow,
            SubTotal = totalSubTotal, DiscountPercent = 0, DiscountAmount = 0,
            PromotionId = promotion?.Id, PromotionCode = promotion?.Code, PromotionName = promotion?.Name,
            PromotionDiscountAmount = promotionDiscountAmount, FinalAmount = finalAmount,
            PaidAmount = 0, DebtAmount = finalAmount, PaymentMethod = request.PaymentMethod,
            Status = "Pending", Note = request.Note, OrderDetails = details
        };

        _context.Orders.Add(order);

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id, OldStatus = "", NewStatus = "Pending",
            Note = "Đơn hàng mới được tạo.", ChangedBy = request.CreatedBy,
            ChangedByName = request.CreatedByName, CreatedAt = DateTime.UtcNow
        });

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer != null)
        {
            customer.TotalPurchased += order.FinalAmount;
            customer.DebtAmount += order.DebtAmount;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Publish OrderCreatedEvent bất đồng bộ để trừ kho và làm báo cáo
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            FinalAmount = order.FinalAmount,
            OrderDate = order.OrderDate,
            Items = order.OrderDetails.Select(item => new OrderCreatedItem
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CostPrice = item.CostPrice,
                CostTotal = item.CostTotal,
                SubTotal = item.SubTotal
            }).ToList()
        };
        await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);
        await _publishEndpoint.Publish(new AuditLoggedEvent
        {
            UserId = request.CreatedBy,
            UserName = request.CreatedByName,
            ServiceName = "OrderSalesService",
            Action = "OrderCreated",
            EntityType = "Order",
            EntityId = order.Id.ToString(),
            Severity = "Info",
            Description = $"Order {order.OrderCode} created with amount {order.FinalAmount}.",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return new CreateOrderResponse(
            order.Id,
            order.OrderCode,
            order.SubTotal,
            order.PromotionDiscountAmount,
            order.FinalAmount,
            order.PromotionCode,
            order.PromotionName);
    }
}
