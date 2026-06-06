using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserReportService.API.Security;
using UserReportService.Application.Common.Models;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Notifications;

namespace UserReportService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission("notifications.read")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications(
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(GetUserId(), isRead, page, pageSize));
        return Ok(ApiResponse<List<NotificationDto>>.SuccessResponse(result));
    }

    [HttpGet("unread-count")]
    [RequirePermission("notifications.read")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadNotificationCountQuery(GetUserId()));
        return Ok(ApiResponse<int>.SuccessResponse(result));
    }

    [HttpPut("{id:guid}/read")]
    [RequirePermission("notifications.read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkRead(Guid id)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id, GetUserId()));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Notification not found."));
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    [HttpPut("read-all")]
    [RequirePermission("notifications.read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllRead()
    {
        var result = await _mediator.Send(new MarkAllNotificationsReadCommand(GetUserId()));
        return Ok(ApiResponse<int>.SuccessResponse(result));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
