using UserReportService.Application.Models;

namespace UserReportService.Application.Interfaces;

public interface INotificationBroadcaster
{
    Task BroadcastAsync(Notification notification, CancellationToken cancellationToken);
}
