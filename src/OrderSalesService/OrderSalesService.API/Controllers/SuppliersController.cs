using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Features.Suppliers;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Warehouse")]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;
    public SuppliersController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/suppliers — Danh sách nhà cung cấp</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<SupplierDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(search, page, pageSize));
        return Ok(ApiResponse<PagedResponse<SupplierDto>>.SuccessResponse(result));
    }

    /// <summary>POST /api/suppliers — Tạo nhà cung cấp</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Create([FromBody] CreateSupplierCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), ApiResponse<SupplierDto>.SuccessResponse(result, "Tạo nhà cung cấp thành công."));
    }

    /// <summary>PUT /api/suppliers/{id} — Cập nhật nhà cung cấp</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Update(Guid id, [FromBody] UpdateSupplierCommand command)
    {
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd);
        if (result == null) return NotFound(ApiResponse<SupplierDto>.FailResponse("Không tìm thấy nhà cung cấp."));
        return Ok(ApiResponse<SupplierDto>.SuccessResponse(result, "Cập nhật nhà cung cấp thành công."));
    }
}
