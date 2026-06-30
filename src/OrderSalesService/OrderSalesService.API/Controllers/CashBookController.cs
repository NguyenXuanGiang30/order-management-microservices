using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Features.CashBook;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class CashBookController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashBookController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<CashTransactionDto>>>> GetTransactions(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetCashTransactionsQuery(search, type, category, page, pageSize));
        return Ok(ApiResponse<PagedResponse<CashTransactionDto>>.SuccessResponse(result));
    }

    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<CashBookBalanceDto>>> GetBalance()
    {
        var result = await _mediator.Send(new GetCashBookBalanceQuery());
        return Ok(ApiResponse<CashBookBalanceDto>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CashTransactionDto>>> CreateTransaction([FromBody] CreateCashTxRequest request)
    {
        var userId = CurrentUserId();
        var userName = CurrentUserName();
        var result = await _mediator.Send(new CreateCashTransactionCommand(
            request.Type,
            request.Amount,
            request.SourceOrRecipient,
            request.Category,
            request.Note,
            userId,
            userName
        ));
        return Ok(ApiResponse<CashTransactionDto>.SuccessResponse(result, "Ghi nhận giao dịch quỹ thành công."));
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    private string CurrentUserName() =>
        User.FindFirstValue(ClaimTypes.Name) ??
        User.Identity?.Name ??
        "System";
}

public record CreateCashTxRequest(
    string Type,
    decimal Amount,
    string SourceOrRecipient,
    string Category,
    string? Note);
