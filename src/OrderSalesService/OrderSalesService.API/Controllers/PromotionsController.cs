using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Features.Promotions;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class PromotionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromotionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PromotionDto>>>> GetAll()
    {
        var result = await _mediator.Send(new GetPromotionsQuery());
        return Ok(ApiResponse<List<PromotionDto>>.SuccessResponse(result));
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<PromotionDto>>>> GetActive()
    {
        var result = await _mediator.Send(new GetActivePromotionsQuery());
        return Ok(ApiResponse<List<PromotionDto>>.SuccessResponse(result));
    }

    [HttpGet("{code}/validate")]
    public async Task<ActionResult<ApiResponse<PromotionDto>>> Validate(string code)
    {
        var result = await _mediator.Send(new GetPromotionByCodeQuery(code));
        if (result == null)
        {
            return NotFound(ApiResponse<PromotionDto>.FailResponse("Khong tim thay ma khuyen mai."));
        }

        return Ok(ApiResponse<PromotionDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PromotionDto>>> Create([FromBody] CreatePromotionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Validate), new { code = result.Code },
            ApiResponse<PromotionDto>.SuccessResponse(result, "Tao khuyen mai thanh cong."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PromotionDto>>> Update(Guid id, [FromBody] UpdatePromotionCommand command)
    {
        var result = await _mediator.Send(command with { Id = id });
        if (result == null)
        {
            return NotFound(ApiResponse<PromotionDto>.FailResponse("Khong tim thay khuyen mai."));
        }

        return Ok(ApiResponse<PromotionDto>.SuccessResponse(result, "Cap nhat khuyen mai thanh cong."));
    }

    [HttpPut("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PromotionDto>>> ToggleActive(Guid id)
    {
        var result = await _mediator.Send(new TogglePromotionActiveCommand(id));
        if (result == null)
        {
            return NotFound(ApiResponse<PromotionDto>.FailResponse("Khong tim thay khuyen mai."));
        }

        return Ok(ApiResponse<PromotionDto>.SuccessResponse(result, "Cap nhat trang thai khuyen mai thanh cong."));
    }
}
