using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedContracts.Events;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Infrastructure.Consumers;

public class LowStockAlertConsumer : IConsumer<LowStockAlertEvent>
{
    private static readonly Guid SystemAdminId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
    private readonly IUserReportDbContext _context;
    private readonly INotificationBroadcaster _notificationBroadcaster;

    public LowStockAlertConsumer(IUserReportDbContext context, INotificationBroadcaster notificationBroadcaster)
    {
        _context = context;
        _notificationBroadcaster = notificationBroadcaster;
    }

    public async Task Consume(ConsumeContext<LowStockAlertEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId?.ToString() ?? $"LowStock:{message.ProductId}:{message.CreatedAt:O}";

        var isProcessed = await _context.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(LowStockAlertConsumer), context.CancellationToken);

        if (isProcessed)
        {
            return;
        }

        var warningLog = new ActivityLog
        {
            UserId = SystemAdminId,
            Action = "LowStockWarning",
            EntityType = "Product",
            EntityId = message.ProductId.ToString(),
            ServiceName = "ProductInventoryService",
            Severity = "Warning",
            Description = message.AlertMessage,
            IpAddress = "SystemChannel",
            CreatedAt = DateTime.UtcNow
        };

        var notification = new Notification
        {
            Title = "Low stock alert",
            Message = message.AlertMessage,
            Severity = "Warning",
            Link = $"/inventory?productId={message.ProductId}",
            CreatedAt = DateTime.UtcNow
        };

        _context.ActivityLogs.Add(warningLog);
        _context.Notifications.Add(notification);
        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = nameof(LowStockAlertConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(context.CancellationToken);
        await _notificationBroadcaster.BroadcastAsync(notification, context.CancellationToken);
    }
}
