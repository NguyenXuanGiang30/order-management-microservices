namespace OrderSalesService.Application.DTOs;

// ======================== Order DTOs ========================
public record OrderDto(Guid Id, string OrderCode, Guid CustomerId, string CustomerName,
    string CreatedByName, DateTime OrderDate, decimal SubTotal, decimal DiscountPercent,
    decimal DiscountAmount, Guid? PromotionId, string? PromotionCode, string? PromotionName,
    decimal PromotionDiscountAmount, decimal FinalAmount, decimal PaidAmount, decimal DebtAmount,
    string? PaymentMethod, string Status, string? Note, DateTime CreatedAt,
    List<OrderDetailDto> OrderDetails);

public record OrderDetailDto(Guid Id, Guid ProductId, string ProductCode, string ProductName,
    string UnitName, decimal UnitPrice, int Quantity, decimal DiscountPercent, decimal SubTotal);

// ======================== Customer DTOs ========================
public record CustomerDto(Guid Id, string Code, string FullName, string? Phone, string? Email,
    string? Address, string? TaxCode, Guid? CustomerGroupId, string? CustomerGroupName,
    decimal TotalPurchased, decimal DebtAmount, bool IsActive, DateTime CreatedAt);

public record CreateCustomerDto(string Code, string FullName, string? Phone, string? Email,
    string? Address, string? TaxCode, Guid? CustomerGroupId, string? Note);

// ======================== Supplier DTOs ========================
public record SupplierDto(Guid Id, string Code, string Name, string? ContactPerson,
    string? ContactPhone, string? ContactEmail, string? Address, string? TaxCode, bool IsActive);

public record CreateSupplierDto(string Code, string Name, string? ContactPerson,
    string? ContactPhone, string? ContactEmail, string? Address, string? TaxCode, Guid CreatedBy);

// ======================== CustomerGroup DTOs ========================
public record CustomerGroupDto(Guid Id, string Name, decimal DefaultDiscountPercent, string? Note);

