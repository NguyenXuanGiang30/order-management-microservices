namespace UserReportService.Application.DTOs;

public record UserDto(Guid Id, string Username, string FullName, string? Email, string? Phone, string? AvatarUrl, string Role, IReadOnlyList<string> Permissions, DateTime? LastLoginAt, bool IsActive, DateTime CreatedAt);
public record DailySalesSummaryDto(Guid Id, DateTime ReportDate, int TotalOrders, decimal TotalRevenue, decimal TotalDiscount, int TotalItemsSold, int TotalNewCustomers);
public record MonthlySalesSummaryDto(Guid Id, int Year, int Month, int TotalOrders, decimal TotalRevenue, decimal TotalDiscount, int TotalItemsSold, int TotalNewCustomers);
public record DashboardDto(DailySalesSummaryDto? Today, List<DailySalesSummaryDto> Last7Days, MonthlySalesSummaryDto? CurrentMonth, MonthlySalesSummaryDto? PreviousMonth = null, List<DailySalesSummaryDto>? Last14Days = null);

// Reports DTOs
public record TopProductDto(Guid ProductId, string ProductCode, string ProductName, int TotalQuantitySold, decimal TotalRevenue);
public record TopCustomerDto(Guid CustomerId, string CustomerName, string? CustomerPhone, int TotalOrders, decimal TotalSpent);
public record MonthlyRevenueDto(int Month, int TotalOrders, decimal TotalRevenue, decimal TotalDiscount, int TotalItemsSold, int TotalNewCustomers);
public record DailyProfitDto(DateTime ReportDate, int TotalOrders, decimal TotalRevenue, decimal TotalCost, decimal GrossProfit, decimal MarginPercent);
public record MonthlyProfitDto(int Year, int Month, int TotalOrders, decimal TotalRevenue, decimal TotalCost, decimal GrossProfit, decimal MarginPercent);
public record ProductProfitDto(Guid ProductId, string ProductCode, string ProductName, int TotalQuantitySold, decimal TotalRevenue, decimal TotalCost, decimal GrossProfit, decimal MarginPercent);
public record ActivityLogDto(Guid Id, Guid UserId, string Username, string Action, string EntityType, string? EntityId, string ServiceName, string Severity, string? Description, string? IpAddress, DateTime CreatedAt);
public record NotificationDto(Guid Id, string Title, string Message, string Severity, string? Link, bool IsRead, DateTime CreatedAt);
public record BackupRecordDto(Guid Id, string BackupId, string Status, string CreatedByName, DateTime CreatedAt, DateTime? RestoredAt, string? Note);
