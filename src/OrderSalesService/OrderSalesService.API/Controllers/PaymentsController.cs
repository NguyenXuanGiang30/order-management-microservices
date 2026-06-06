using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Features.Payments;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) { _mediator = mediator; }

    /// <summary>POST /api/payments — Ghi nhận thanh toán</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> CreatePayment([FromBody] CreatePaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPayments), ApiResponse<PaymentDto>.SuccessResponse(result, "Ghi nhận thanh toán thành công."));
    }

    /// <summary>GET /api/payments — Lịch sử thanh toán</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<PaymentDto>>>> GetPayments(
        [FromQuery] Guid? customerId, [FromQuery] Guid? orderId, [FromQuery] DateTime? from,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPaymentsQuery(customerId, orderId, from, page, pageSize));
        return Ok(ApiResponse<PagedResponse<PaymentDto>>.SuccessResponse(result));
    }
}
