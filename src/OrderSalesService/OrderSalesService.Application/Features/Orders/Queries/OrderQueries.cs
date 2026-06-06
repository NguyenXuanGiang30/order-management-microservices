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
