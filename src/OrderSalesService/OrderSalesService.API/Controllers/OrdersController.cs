using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderSalesService.Application.Common.Models;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Features.Orders.Commands;
using OrderSalesService.Application.Features.Orders.Commands.CreateOrder;
using OrderSalesService.Application.Features.Orders.Queries;

namespace OrderSalesService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/orders — Danh sách đơn hàng</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<OrderDto>>>> GetOrders(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] Guid? customerId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "CreatedAt", [FromQuery] bool sortDescending = true)
    {
        var result = await _mediator.Send(new GetOrdersQuery(search, status, customerId, page, pageSize, sortBy, sortDescending));
        return Ok(ApiResponse<PagedResponse<OrderDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/orders/{id} — Chi tiết đơn hàng</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result == null) return NotFound(ApiResponse<OrderDto>.FailResponse("Không tìm thấy đơn hàng."));
        return Ok(ApiResponse<OrderDto>.SuccessResponse(result));
    }

    /// <summary>POST /api/orders — Tạo đơn hàng mới</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateOrderResponse>>> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id },
            ApiResponse<CreateOrderResponse>.SuccessResponse(result, "Tạo đơn hàng thành công."));
    }

    /// <summary>PUT /api/orders/{id} — Sửa đơn hàng (khi Pending)</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateOrder(Guid id, [FromBody] UpdateOrderCommand command)
    {
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd);
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy đơn hàng hoặc đơn đã xác nhận."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật đơn hàng thành công."));
    }

    /// <summary>PUT /api/orders/{id}/cancel — Hủy đơn hàng</summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";
        var result = await _mediator.Send(new CancelOrderCommand(id, userId, userName, request.Reason));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy đơn hàng hoặc đơn đã bị hủy."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Hủy đơn hàng thành công."));
    }

    /// <summary>POST /api/orders/{id}/return — Trả hàng</summary>
    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<ApiResponse<ReturnOrderResultDto>>> ReturnOrder(Guid id, [FromBody] ReturnOrderRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var result = await _mediator.Send(new CreateReturnOrderCommand(id, request.ReturnReason, userId, request.Items));
        return Ok(ApiResponse<ReturnOrderResultDto>.SuccessResponse(result, "Tạo phiếu trả hàng thành công."));
    }

    /// <summary>GET /api/orders/{id}/invoice — Xuất hóa đơn</summary>
    [HttpGet("{id:guid}/invoice")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(Guid id)
    {
        var result = await _mediator.Send(new GetOrderInvoiceQuery(id));
        if (result == null) return NotFound(ApiResponse<InvoiceDto>.FailResponse("Không tìm thấy đơn hàng."));
        return Ok(ApiResponse<InvoiceDto>.SuccessResponse(result));
    }

    /// <summary>GET /api/orders/{id}/status-history — Lịch sử trạng thái</summary>
    [HttpGet("{id:guid}/status-history")]
    public async Task<ActionResult<ApiResponse<List<StatusHistoryDto>>>> GetStatusHistory(Guid id)
    {
        var result = await _mediator.Send(new GetOrderStatusHistoryQuery(id));
        return Ok(ApiResponse<List<StatusHistoryDto>>.SuccessResponse(result));
    }
}

public record CancelOrderRequest(string? Reason);
public record ReturnOrderRequest(string ReturnReason, List<ReturnItemDto> Items);
