using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Interfaces;

namespace ProductInventoryService.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Handler xử lý truy vấn danh sách sản phẩm với phân trang + tìm kiếm + sắp xếp.
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<ProductDto>>
{
    private readonly IProductInventoryDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IProductInventoryDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResponse<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.Inventory)
            .AsQueryable();

        // Tìm kiếm theo tên hoặc mã sản phẩm
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower)
                                  || p.Code.ToLower().Contains(searchLower)
                                  || (p.Barcode != null && p.Barcode.ToLower().Contains(searchLower)));
        }

        // Lọc theo danh mục
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        // Lọc theo trạng thái
        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        // Sắp xếp động
        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code),
            "price" => request.SortDescending ? query.OrderByDescending(p => p.SellPrice) : query.OrderBy(p => p.SellPrice),
            "createdat" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(items),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
