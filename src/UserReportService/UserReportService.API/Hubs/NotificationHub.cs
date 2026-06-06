using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UserReportService.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
