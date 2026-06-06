using MediatR;

namespace OrderSalesService.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid CustomerId,
    string CustomerName,
    Guid CreatedBy,
    string CreatedByName,
    string? PaymentMethod,
    string? PromotionCode,
    string? Note,
    List<CreateOrderItemDto> Items
) : IRequest<CreateOrderResponse>;

public record CreateOrderItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitName,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountPercent
);

public record CreateOrderResponse(
    Guid Id,
    string OrderCode,
    decimal SubTotal,
    decimal PromotionDiscountAmount,
    decimal FinalAmount,
    string? PromotionCode,
    string? PromotionName);
