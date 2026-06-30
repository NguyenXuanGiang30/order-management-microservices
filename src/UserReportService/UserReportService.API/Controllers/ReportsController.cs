using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserReportService.API.Security;
using UserReportService.Application.Common.Models;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Reports;
using UserReportService.Application.Interfaces;

namespace UserReportService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserReportDbContext _context;
    private readonly IMapper _mapper;

    public ReportsController(IMediator mediator, IUserReportDbContext context, IMapper mapper)
    {
        _mediator = mediator;
        _context = context;
        _mapper = mapper;
    }

    [HttpGet("dashboard")]
    [RequirePermission("dashboard.read")]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var todaySummary = await _context.DailySalesSummaries.AsNoTracking().FirstOrDefaultAsync(d => d.ReportDate == today);
        var last7Days = await _context.DailySalesSummaries.AsNoTracking().Where(d => d.ReportDate >= today.AddDays(-7)).OrderByDescending(d => d.ReportDate).ToListAsync();
        var currentMonth = await _context.MonthlySalesSummaries.AsNoTracking().FirstOrDefaultAsync(m => m.Year == today.Year && m.Month == today.Month);

        var lastMonthDate = today.AddMonths(-1);
        var previousMonth = await _context.MonthlySalesSummaries.AsNoTracking().FirstOrDefaultAsync(m => m.Year == lastMonthDate.Year && m.Month == lastMonthDate.Month);
        var last14Days = await _context.DailySalesSummaries.AsNoTracking().Where(d => d.ReportDate >= today.AddDays(-14)).OrderByDescending(d => d.ReportDate).ToListAsync();

        var dashboard = new DashboardDto(
            _mapper.Map<DailySalesSummaryDto>(todaySummary),
            _mapper.Map<List<DailySalesSummaryDto>>(last7Days),
            _mapper.Map<MonthlySalesSummaryDto>(currentMonth),
            _mapper.Map<MonthlySalesSummaryDto>(previousMonth),
            _mapper.Map<List<DailySalesSummaryDto>>(last14Days));

        return Ok(ApiResponse<DashboardDto>.SuccessResponse(dashboard));
    }

    [HttpGet("top-products")]
    [RequirePermission("reports.read")]
    public async Task<ActionResult<ApiResponse<List<TopProductDto>>>> GetTopProducts(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int limit = 10)
    {
        var result = await _mediator.Send(new GetTopProductsQuery(year, month, limit));
        return Ok(ApiResponse<List<TopProductDto>>.SuccessResponse(result));
    }

    [HttpGet("top-customers")]
    [RequirePermission("reports.read")]
    public async Task<ActionResult<ApiResponse<List<TopCustomerDto>>>> GetTopCustomers(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int limit = 10)
    {
        var result = await _mediator.Send(new GetTopCustomersQuery(year, month, limit));
        return Ok(ApiResponse<List<TopCustomerDto>>.SuccessResponse(result));
    }

    [HttpGet("revenue-by-month")]
    [RequirePermission("reports.read")]
    public async Task<ActionResult<ApiResponse<List<MonthlyRevenueDto>>>> GetRevenueByMonth([FromQuery] int year)
    {
        var result = await _mediator.Send(new GetRevenueByMonthQuery(year));
        return Ok(ApiResponse<List<MonthlyRevenueDto>>.SuccessResponse(result));
    }

    [HttpGet("profit/daily")]
    [RequirePermission("reports.profit")]
    public async Task<ActionResult<ApiResponse<List<DailyProfitDto>>>> GetDailyProfit(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _mediator.Send(new GetDailyProfitQuery(from, to));
        return Ok(ApiResponse<List<DailyProfitDto>>.SuccessResponse(result));
    }

    [HttpGet("profit/monthly")]
    [RequirePermission("reports.profit")]
    public async Task<ActionResult<ApiResponse<List<MonthlyProfitDto>>>> GetMonthlyProfit([FromQuery] int? year)
    {
        var result = await _mediator.Send(new GetMonthlyProfitQuery(year));
        return Ok(ApiResponse<List<MonthlyProfitDto>>.SuccessResponse(result));
    }

    [HttpGet("profit/products")]
    [RequirePermission("reports.profit")]
    public async Task<ActionResult<ApiResponse<List<ProductProfitDto>>>> GetProductProfit(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new GetProductProfitQuery(year, month, limit));
        return Ok(ApiResponse<List<ProductProfitDto>>.SuccessResponse(result));
    }

    [HttpGet("activity-logs")]
    [RequirePermission("audit.read")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ActivityLogDto>>>> GetActivityLogs(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? serviceName,
        [FromQuery] string? severity,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetActivityLogsQuery(userId, from, to, serviceName, severity, action, entityType, page, pageSize));
        return Ok(ApiResponse<PagedResponse<ActivityLogDto>>.SuccessResponse(result));
    }

    [HttpGet("daily")]
    [RequirePermission("reports.read")]
    public async Task<ActionResult<ApiResponse<List<DailySalesSummaryDto>>>> GetDailyReports(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var q = _context.DailySalesSummaries.AsNoTracking().AsQueryable();
        if (from.HasValue) q = q.Where(d => d.ReportDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(d => d.ReportDate <= to.Value.Date);

        var results = await q.OrderByDescending(d => d.ReportDate).ToListAsync();
        return Ok(ApiResponse<List<DailySalesSummaryDto>>.SuccessResponse(_mapper.Map<List<DailySalesSummaryDto>>(results)));
    }

    [HttpGet("monthly")]
    [RequirePermission("reports.read")]
    public async Task<ActionResult<ApiResponse<List<MonthlySalesSummaryDto>>>> GetMonthlyReports([FromQuery] int? year)
    {
        var q = _context.MonthlySalesSummaries.AsNoTracking().AsQueryable();
        if (year.HasValue) q = q.Where(m => m.Year == year.Value);

        var results = await q.OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).ToListAsync();
        return Ok(ApiResponse<List<MonthlySalesSummaryDto>>.SuccessResponse(_mapper.Map<List<MonthlySalesSummaryDto>>(results)));
    }
}
