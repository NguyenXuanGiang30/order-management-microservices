using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Common.Models;
using UserReportService.Application.DTOs;
using UserReportService.Application.Interfaces;

namespace UserReportService.Application.Features.Reports;

// ======================== Top Products ========================
public record GetTopProductsQuery(int? Year, int? Month, int Limit = 10) : IRequest<List<TopProductDto>>;

public class GetTopProductsQueryHandler : IRequestHandler<GetTopProductsQuery, List<TopProductDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetTopProductsQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<List<TopProductDto>> Handle(GetTopProductsQuery req, CancellationToken ct)
    {
        var q = _ctx.TopProductSummaries.AsNoTracking().AsQueryable();
        if (req.Year.HasValue) q = q.Where(t => t.Year == req.Year.Value);
        if (req.Month.HasValue) q = q.Where(t => t.Month == req.Month.Value);

        return await q.OrderByDescending(t => t.TotalQuantitySold).Take(req.Limit)
            .Select(t => new TopProductDto(t.ProductId, t.ProductCode, t.ProductName, t.TotalQuantitySold, t.TotalRevenueGenerated))
            .ToListAsync(ct);
    }
}

// ======================== Top Customers ========================
public record GetTopCustomersQuery(int? Year, int? Month, int Limit = 10) : IRequest<List<TopCustomerDto>>;

public class GetTopCustomersQueryHandler : IRequestHandler<GetTopCustomersQuery, List<TopCustomerDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetTopCustomersQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<List<TopCustomerDto>> Handle(GetTopCustomersQuery req, CancellationToken ct)
    {
        var q = _ctx.TopCustomerSummaries.AsNoTracking().AsQueryable();
        if (req.Year.HasValue) q = q.Where(t => t.Year == req.Year.Value);
        if (req.Month.HasValue) q = q.Where(t => t.Month == req.Month.Value);

        return await q.OrderByDescending(t => t.TotalSpent).Take(req.Limit)
            .Select(t => new TopCustomerDto(t.CustomerId, t.CustomerName, t.CustomerPhone, t.TotalOrders, t.TotalSpent))
            .ToListAsync(ct);
    }
}

// ======================== Revenue by Month ========================
public record GetRevenueByMonthQuery(int Year) : IRequest<List<MonthlyRevenueDto>>;

public class GetRevenueByMonthQueryHandler : IRequestHandler<GetRevenueByMonthQuery, List<MonthlyRevenueDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetRevenueByMonthQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<List<MonthlyRevenueDto>> Handle(GetRevenueByMonthQuery req, CancellationToken ct)
    {
        return await _ctx.MonthlySalesSummaries.AsNoTracking()
            .Where(m => m.Year == req.Year)
            .OrderBy(m => m.Month)
            .Select(m => new MonthlyRevenueDto(m.Month, m.TotalOrders, m.TotalRevenue, m.TotalDiscount, m.TotalItemsSold, m.TotalNewCustomers))
            .ToListAsync(ct);
    }
}

// ======================== Profit Reports ========================
public record GetDailyProfitQuery(DateTime? From, DateTime? To) : IRequest<List<DailyProfitDto>>;

public class GetDailyProfitQueryHandler : IRequestHandler<GetDailyProfitQuery, List<DailyProfitDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetDailyProfitQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<List<DailyProfitDto>> Handle(GetDailyProfitQuery req, CancellationToken ct)
    {
        var q = _ctx.DailyProfitSummaries.AsNoTracking().AsQueryable();
        if (req.From.HasValue) q = q.Where(p => p.ReportDate >= req.From.Value.Date);
        if (req.To.HasValue) q = q.Where(p => p.ReportDate <= req.To.Value.Date);

        return await q.OrderByDescending(p => p.ReportDate)
            .Select(p => new DailyProfitDto(p.ReportDate, p.TotalOrders, p.TotalRevenue, p.TotalCost, p.GrossProfit, p.MarginPercent))
            .ToListAsync(ct);
    }
}

public record GetMonthlyProfitQuery(int? Year) : IRequest<List<MonthlyProfitDto>>;

public class GetMonthlyProfitQueryHandler : IRequestHandler<GetMonthlyProfitQuery, List<MonthlyProfitDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetMonthlyProfitQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<List<MonthlyProfitDto>> Handle(GetMonthlyProfitQuery req, CancellationToken ct)
    {
        var q = _ctx.MonthlyProfitSummaries.AsNoTracking().AsQueryable();
        if (req.Year.HasValue) q = q.Where(p => p.Year == req.Year.Value);

        return await q.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new MonthlyProfitDto(p.Year, p.Month, p.TotalOrders, p.TotalRevenue, p.TotalCost, p.GrossProfit, p.MarginPercent))
            .ToListAsync(ct);
    }
}

public record GetProductProfitQuery(int? Year, int? Month, int Limit = 20) : IRequest<List<ProductProfitDto>>;

public class GetProductProfitQueryHandler : IRequestHandler<GetProductProfitQuery, List<ProductProfitDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetProductProfitQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<List<ProductProfitDto>> Handle(GetProductProfitQuery req, CancellationToken ct)
    {
        var q = _ctx.ProductProfitSummaries.AsNoTracking().AsQueryable();
        if (req.Year.HasValue) q = q.Where(p => p.Year == req.Year.Value);
        if (req.Month.HasValue) q = q.Where(p => p.Month == req.Month.Value);

        var limit = req.Limit <= 0 ? 20 : Math.Min(req.Limit, 100);
        return await q.OrderByDescending(p => p.GrossProfit).Take(limit)
            .Select(p => new ProductProfitDto(p.ProductId, p.ProductCode, p.ProductName, p.TotalQuantitySold, p.TotalRevenue, p.TotalCost, p.GrossProfit, p.MarginPercent))
            .ToListAsync(ct);
    }
}

// ======================== Activity Logs ========================
public record GetActivityLogsQuery(
    Guid? UserId,
    DateTime? From,
    DateTime? To,
    string? ServiceName = null,
    string? Severity = null,
    string? Action = null,
    string? EntityType = null,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PagedResponse<ActivityLogDto>>;

public class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, PagedResponse<ActivityLogDto>>
{
    private readonly IUserReportDbContext _ctx;
    public GetActivityLogsQueryHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<PagedResponse<ActivityLogDto>> Handle(GetActivityLogsQuery req, CancellationToken ct)
    {
        var q = _ctx.ActivityLogs.Include(a => a.User).AsNoTracking().AsQueryable();
        if (req.UserId.HasValue) q = q.Where(a => a.UserId == req.UserId.Value);
        if (req.From.HasValue) q = q.Where(a => a.CreatedAt >= req.From.Value);
        if (req.To.HasValue) q = q.Where(a => a.CreatedAt <= req.To.Value);
        if (!string.IsNullOrWhiteSpace(req.ServiceName)) q = q.Where(a => a.ServiceName == req.ServiceName);
        if (!string.IsNullOrWhiteSpace(req.Severity)) q = q.Where(a => a.Severity == req.Severity);
        if (!string.IsNullOrWhiteSpace(req.Action)) q = q.Where(a => a.Action.Contains(req.Action));
        if (!string.IsNullOrWhiteSpace(req.EntityType)) q = q.Where(a => a.EntityType == req.EntityType);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((req.PageNumber - 1) * req.PageSize).Take(req.PageSize)
            .Select(a => new ActivityLogDto(a.Id, a.UserId, a.User.Username, a.Action, a.EntityType, a.EntityId, a.ServiceName, a.Severity, a.Description, a.IpAddress, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<ActivityLogDto> { Items = items, PageNumber = req.PageNumber, PageSize = req.PageSize, TotalCount = total };
    }
}
