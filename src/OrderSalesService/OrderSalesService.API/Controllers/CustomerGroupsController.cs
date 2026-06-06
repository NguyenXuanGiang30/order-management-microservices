using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Features.CustomerGroups;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/customer-groups")]
[Authorize]
public class CustomerGroupsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomerGroupsController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/customer-groups — Danh sách nhóm khách hàng</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Sales")]
    public async Task<ActionResult<ApiResponse<List<CustomerGroupDto>>>> GetAll()
    {
        var result = await _mediator.Send(new GetCustomerGroupsQuery());
        return Ok(ApiResponse<List<CustomerGroupDto>>.SuccessResponse(result));
    }

    /// <summary>POST /api/customer-groups — Tạo nhóm khách hàng mới</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustomerGroupDto>>> Create([FromBody] CreateCustomerGroupCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), ApiResponse<CustomerGroupDto>.SuccessResponse(result, "Tạo nhóm khách hàng thành công."));
    }

    /// <summary>PUT /api/customer-groups/{id} — Cập nhật nhóm khách hàng</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CustomerGroupDto>>> Update(Guid id, [FromBody] UpdateCustomerGroupCommand command)
    {
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd);
        if (result == null) return NotFound(ApiResponse<CustomerGroupDto>.FailResponse("Không tìm thấy nhóm khách hàng."));
        return Ok(ApiResponse<CustomerGroupDto>.SuccessResponse(result, "Cập nhật nhóm khách hàng thành công."));
    }

    /// <summary>DELETE /api/customer-groups/{id} — Xóa nhóm khách hàng</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCustomerGroupCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy nhóm khách hàng."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa nhóm khách hàng thành công."));
    }
}
