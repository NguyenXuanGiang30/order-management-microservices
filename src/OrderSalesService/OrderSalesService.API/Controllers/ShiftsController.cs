using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Features.Shifts;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShiftsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<CashShiftDto?>>> GetCurrentShift()
    {
        var userId = CurrentUserId();
        var result = await _mediator.Send(new GetCurrentCashShiftQuery(userId));
        return Ok(ApiResponse<CashShiftDto?>.SuccessResponse(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<CashShiftDto>>>> GetShifts(
        [FromQuery] Guid? cashierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCashShiftsQuery(cashierId, page, pageSize));
        return Ok(ApiResponse<PagedResponse<CashShiftDto>>.SuccessResponse(result));
    }

    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<CashShiftDto>>> OpenShift([FromBody] OpenShiftRequest request)
    {
        var userId = CurrentUserId();
        var userName = CurrentUserName();
        var result = await _mediator.Send(new OpenCashShiftCommand(userId, userName, request.OpeningCash, request.Note));
        return Ok(ApiResponse<CashShiftDto>.SuccessResponse(result, "Mở ca bán hàng thành công."));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<CashShiftDto>>> CloseShift(Guid id, [FromBody] CloseShiftRequest request)
    {
        var userId = CurrentUserId();
        var result = await _mediator.Send(new CloseCashShiftCommand(id, userId, request.ActualCash, request.Note));
        return Ok(ApiResponse<CashShiftDto>.SuccessResponse(result, "Đóng ca bán hàng thành công."));
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    private string CurrentUserName() =>
        User.FindFirstValue(ClaimTypes.Name) ??
        User.Identity?.Name ??
        "POS";
}

public record OpenShiftRequest(decimal OpeningCash, string? Note);
public record CloseShiftRequest(decimal ActualCash, string? Note);
