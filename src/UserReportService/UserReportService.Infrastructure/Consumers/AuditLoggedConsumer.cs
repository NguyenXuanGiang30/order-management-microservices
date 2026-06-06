using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedContracts.Events;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Infrastructure.Consumers;

public class AuditLoggedConsumer : IConsumer<AuditLoggedEvent>
{
    private static readonly Guid SystemAdminId = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
    private readonly IUserReportDbContext _context;

    public AuditLoggedConsumer(IUserReportDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<AuditLoggedEvent> context)
    {
        var message = context.Message;
        var eventId = context.MessageId?.ToString() ?? $"AuditLogged:{message.AuditId}";

        var isProcessed = await _context.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId && pe.ConsumerName == nameof(AuditLoggedConsumer), context.CancellationToken);

        if (isProcessed)
        {
            return;
        }

        var userId = await ResolveUserIdAsync(message.UserId, message.UserName, context.CancellationToken);
        _context.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = message.Action,
            EntityType = message.EntityType,
            EntityId = message.EntityId,
            ServiceName = message.ServiceName,
            Severity = message.Severity,
            Description = message.Description,
            IpAddress = message.IpAddress,
            CreatedAt = message.CreatedAt.ToUniversalTime()
        });

        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ConsumerName = nameof(AuditLoggedConsumer),
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(context.CancellationToken);
    }

    private async Task<Guid> ResolveUserIdAsync(Guid? userId, string? userName, CancellationToken ct)
    {
        if (userId.HasValue && await _context.Users.AnyAsync(u => u.Id == userId.Value, ct))
        {
            return userId.Value;
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var matchedUserId = await _context.Users
                .Where(u => u.Username == userName)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(ct);

            if (matchedUserId.HasValue)
            {
                return matchedUserId.Value;
            }
        }

        return SystemAdminId;
    }
}
