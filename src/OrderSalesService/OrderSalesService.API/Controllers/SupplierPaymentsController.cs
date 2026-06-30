using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.Features.SupplierPayments;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Warehouse,Sales")]
public class SupplierPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupplierPaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/supplierpayments — Danh sách thanh toán công nợ NCC</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<SupplierPaymentDto>>>> GetPayments(
        [FromQuery] string? search,
        [FromQuery] Guid? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSupplierPaymentsQuery(search, supplierId, page, pageSize));
        return Ok(ApiResponse<PagedResponse<SupplierPaymentDto>>.SuccessResponse(result));
    }

    /// <summary>POST /api/supplierpayments — Ghi nhận thanh toán công nợ cho NCC</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierPaymentDto>>> CreatePayment([FromBody] CreateSupplierPaymentRequest request)
    {
        var userId = CurrentUserId();
        var userName = CurrentUserName();

        var result = await _mediator.Send(new CreateSupplierPaymentCommand(
            request.SupplierId,
            request.Amount,
            request.PaymentMethod,
            request.Note,
            userId,
            userName
        ));

        return Ok(ApiResponse<SupplierPaymentDto>.SuccessResponse(result, "Thanh toán công nợ nhà cung cấp thành công."));
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

public record CreateSupplierPaymentRequest(
    Guid SupplierId,
    decimal Amount,
    string PaymentMethod,
    string? Note
);
