using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductInventoryService.Application.Common.Models;
using ProductInventoryService.Application.DTOs;
using ProductInventoryService.Application.Features.Units;

namespace ProductInventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnitsController : ControllerBase
{
    private readonly IMediator _mediator;
    public UnitsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UnitDto>>>> GetAll()
    {
        var result = await _mediator.Send(new GetUnitsQuery());
        return Ok(ApiResponse<List<UnitDto>>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<UnitDto>>> Create([FromBody] CreateUnitCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), ApiResponse<UnitDto>.SuccessResponse(result, "Tạo đơn vị tính thành công."));
    }
}
