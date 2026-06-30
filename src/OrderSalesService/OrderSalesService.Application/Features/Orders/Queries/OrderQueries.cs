using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Interfaces;

namespace OrderSalesService.Application.Features.Orders.Queries;

public record GetOrdersQuery(string? Search, string? Status, Guid? CustomerId,
    int PageNumber = 1, int PageSize = 10, string SortBy = "CreatedAt", bool SortDescending = true)
    : IRequest<PagedResponse<OrderDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResponse<OrderDto>>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;
    public GetOrdersQueryHandler(IOrderSalesDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<PagedResponse<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders.Include(o => o.OrderDetails).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(o => o.OrderCode.ToLower().Contains(s) || o.CustomerName.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(o => o.Status == request.Status);
        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        query = request.SortBy?.ToLower() switch
        {
            "ordercode" => request.SortDescending ? query.OrderByDescending(o => o.OrderCode) : query.OrderBy(o => o.OrderCode),
            "finalamount" => request.SortDescending ? query.OrderByDescending(o => o.FinalAmount) : query.OrderBy(o => o.FinalAmount),
            _ => request.SortDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .AsNoTracking().ToListAsync(cancellationToken);

        return new PagedResponse<OrderDto>
        {
            Items = _mapper.Map<List<OrderDto>>(items),
            PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total
        };
    }
}

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderSalesDbContext _context;
    private readonly IMapper _mapper;
    public GetOrderByIdQueryHandler(IOrderSalesDbContext context, IMapper mapper) { _context = context; _mapper = mapper; }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.Include(o => o.OrderDetails)
            .AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }
}

public record GetReturnOrdersQuery(string? Search, int PageNumber = 1, int PageSize = 10) : IRequest<PagedResponse<ReturnOrderDto>>;

public class GetReturnOrdersQueryHandler : IRequestHandler<GetReturnOrdersQuery, PagedResponse<ReturnOrderDto>>
{
    private readonly IOrderSalesDbContext _context;

    public GetReturnOrdersQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<ReturnOrderDto>> Handle(GetReturnOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ReturnOrders
            .Include(r => r.Order)
            .ThenInclude(o => o.OrderDetails)
            .Include(r => r.ReturnOrderDetails)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(r => r.ReturnCode.ToLower().Contains(s) || r.Order.OrderCode.ToLower().Contains(s) || r.Order.CustomerName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReturnOrderDto(
                r.Id,
                r.ReturnCode,
                r.OrderId,
                r.Order.OrderCode,
                r.Order.CustomerName,
                r.TotalRefundAmount,
                r.ReturnReason,
                r.Status,
                r.CreatedAt,
                r.ReturnOrderDetails.Select(d => new ReturnOrderDetailDto(
                    d.Id,
                    d.OrderDetailId,
                    r.Order.OrderDetails.FirstOrDefault(od => od.Id == d.OrderDetailId)!.ProductCode ?? "",
                    r.Order.OrderDetails.FirstOrDefault(od => od.Id == d.OrderDetailId)!.ProductName ?? "",
                    r.Order.OrderDetails.FirstOrDefault(od => od.Id == d.OrderDetailId)!.UnitName ?? "",
                    r.Order.OrderDetails.FirstOrDefault(od => od.Id == d.OrderDetailId)!.UnitPrice,
                    d.ReturnQuantity,
                    d.RefundAmount
                )).ToList()
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReturnOrderDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}

public record GetStaffPerformanceQuery(DateTime? From, DateTime? To) : IRequest<List<StaffPerformanceDto>>;

public class GetStaffPerformanceQueryHandler : IRequestHandler<GetStaffPerformanceQuery, List<StaffPerformanceDto>>
{
    private readonly IOrderSalesDbContext _context;

    public GetStaffPerformanceQueryHandler(IOrderSalesDbContext context)
    {
        _context = context;
    }

    public async Task<List<StaffPerformanceDto>> Handle(GetStaffPerformanceQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsNoTracking().Where(o => o.Status != "Cancelled");

        if (request.From.HasValue)
        {
            var fromUtc = request.From.Value.ToUniversalTime();
            query = query.Where(o => o.OrderDate >= fromUtc);
        }

        if (request.To.HasValue)
        {
            var toUtc = request.To.Value.ToUniversalTime();
            query = query.Where(o => o.OrderDate <= toUtc);
        }

        var groups = await query
            .GroupBy(o => new { o.CreatedBy, o.CreatedByName })
            .Select(g => new
            {
                StaffId = g.Key.CreatedBy,
                StaffName = g.Key.CreatedByName,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.FinalAmount)
            })
            .ToListAsync(cancellationToken);

        return groups.Select(g => new StaffPerformanceDto(
            g.StaffId,
            g.StaffName,
            g.TotalOrders,
            g.TotalRevenue,
            g.TotalOrders > 0 ? Math.Round(g.TotalRevenue / g.TotalOrders, 2) : 0
        ))
        .OrderByDescending(s => s.TotalRevenue)
        .ToList();
    }
}

