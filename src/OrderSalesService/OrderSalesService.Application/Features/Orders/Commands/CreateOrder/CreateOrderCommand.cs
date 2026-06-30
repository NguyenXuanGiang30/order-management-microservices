using MediatR;

namespace OrderSalesService.Application.Features.Orders.Commands.CreateOrder;

public record CreatePaymentTransactionDto(
    string PaymentMethod,
    decimal Amount,
    string? Note = null
);

public record CreateOrderCommand(
    Guid CustomerId,
    string CustomerName,
    Guid CreatedBy,
    string CreatedByName,
    string? PaymentMethod,
    string? PromotionCode,
    string? Note,
    List<CreateOrderItemDto> Items,
    string? Status = "Pending",
    List<CreatePaymentTransactionDto>? Payments = null
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
