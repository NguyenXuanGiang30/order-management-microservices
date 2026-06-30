using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.Features.Inventory;

namespace ProductInventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitConversionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UnitConversionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UnitConversionDto>>>> Get([FromQuery] Guid? productId)
    {
        var result = await _mediator.Send(new GetUnitConversionsQuery(productId));
        return Ok(ApiResponse<List<UnitConversionDto>>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<UnitConversionDto>>> Create([FromBody] CreateUnitConversionCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<UnitConversionDto>.SuccessResponse(result, "Tạo quy đổi đơn vị tính thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteUnitConversionCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy quy tắc quy đổi đơn vị."));
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Xóa quy đổi đơn vị tính thành công."));
    }
}
