using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Features.Products;
using ProductInventoryService.Application.Features.Products.Commands.CreateProduct;
using ProductInventoryService.Application.Features.Products.Queries.GetProductById;
using ProductInventoryService.Application.Features.Products.Queries.GetProducts;

namespace ProductInventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/products — Danh sách sản phẩm (phân trang, lọc, sắp xếp)</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductDto>>>> GetProducts(
        [FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Name", [FromQuery] bool sortDescending = false)
    {
        var result = await _mediator.Send(new GetProductsQuery(search, categoryId, isActive, pageNumber, pageSize, sortBy, sortDescending));
        return Ok(ApiResponse<PagedResponse<ProductDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/products/{id} — Chi tiết sản phẩm</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        if (result == null) return NotFound(ApiResponse<ProductDto>.FailResponse("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<ProductDto>.SuccessResponse(result));
    }

    /// <summary>POST /api/products — Tạo sản phẩm mới</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<CreateProductResponse>>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id },
            ApiResponse<CreateProductResponse>.SuccessResponse(result, "Tạo sản phẩm thành công."));
    }

    /// <summary>PUT /api/products/{id} — Cập nhật sản phẩm</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd);
        if (result == null) return NotFound(ApiResponse<ProductDto>.FailResponse("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<ProductDto>.SuccessResponse(result, "Cập nhật sản phẩm thành công."));
    }

    /// <summary>PUT /api/products/{id}/toggle-active — Ngừng/Mở bán</summary>
    [HttpPut("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleActive(Guid id)
    {
        var result = await _mediator.Send(new ToggleProductActiveCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy sản phẩm."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật trạng thái thành công."));
    }
}
