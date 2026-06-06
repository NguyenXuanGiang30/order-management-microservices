using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Features.Customers;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomersController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/customers — Danh sách khách hàng</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<CustomerDto>>>> GetCustomers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCustomersQuery(search, page, pageSize));
        return Ok(ApiResponse<PagedResponse<CustomerDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/customers/{id} — Chi tiết khách hàng kèm lịch sử mua</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDetailDto>>> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        if (result == null) return NotFound(ApiResponse<CustomerDetailDto>.FailResponse("Không tìm thấy khách hàng."));
        return Ok(ApiResponse<CustomerDetailDto>.SuccessResponse(result));
    }

    /// <summary>POST /api/customers — Tạo khách hàng mới</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCustomer), new { id = result.Id },
            ApiResponse<CustomerDto>.SuccessResponse(result, "Tạo khách hàng thành công."));
    }

    /// <summary>PUT /api/customers/{id} — Cập nhật thông tin khách hàng</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateCustomer(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd);
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy khách hàng."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật khách hàng thành công."));
    }

    /// <summary>GET /api/customers/{id}/debt — Chi tiết công nợ</summary>
    [HttpGet("{id:guid}/debt")]
    public async Task<ActionResult<ApiResponse<CustomerDebtDto>>> GetDebt(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerDebtQuery(id));
        if (result == null) return NotFound(ApiResponse<CustomerDebtDto>.FailResponse("Không tìm thấy khách hàng."));
        return Ok(ApiResponse<CustomerDebtDto>.SuccessResponse(result));
    }
}
