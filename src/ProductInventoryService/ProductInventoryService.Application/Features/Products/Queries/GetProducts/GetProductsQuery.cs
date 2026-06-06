using MediatR;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;

namespace ProductInventoryService.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Query lấy danh sách sản phẩm có phân trang, tìm kiếm, lọc.
/// </summary>
public record GetProductsQuery(
    string? Search,
    Guid? CategoryId,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "Name",
    bool SortDescending = false
) : IRequest<PagedResponse<ProductDto>>;
