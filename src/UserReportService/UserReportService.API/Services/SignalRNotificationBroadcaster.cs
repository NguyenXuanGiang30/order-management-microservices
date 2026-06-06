using Microsoft.AspNetCore.SignalR;
using UserReportService.API.Hubs;
using UserReportService.Application.DTOs;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.API.Services;

public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastAsync(Notification notification, CancellationToken cancellationToken)
    {
        var dto = new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Severity,
            notification.Link,
            notification.IsRead,
            notification.CreatedAt);

        return _hubContext.Clients.All.SendAsync("notification.received", dto, cancellationToken);
    }
}
