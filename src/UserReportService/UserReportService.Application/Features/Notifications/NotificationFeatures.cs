using MediatR;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.DTOs;
using UserReportService.Application.Interfaces;

namespace UserReportService.Application.Features.Notifications;

public record GetNotificationsQuery(Guid? UserId, bool? IsRead, int PageNumber = 1, int PageSize = 20)
    : IRequest<List<NotificationDto>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IUserReportDbContext _ctx;

    public GetNotificationsQueryHandler(IUserReportDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var q = _ctx.Notifications.AsNoTracking()
            .Where(n => n.UserId == null || n.UserId == request.UserId)
            .AsQueryable();

        if (request.IsRead.HasValue) q = q.Where(n => n.IsRead == request.IsRead.Value);

        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return await q.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Severity, n.Link, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public record GetUnreadNotificationCountQuery(Guid? UserId) : IRequest<int>;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IUserReportDbContext _ctx;

    public GetUnreadNotificationCountQueryHandler(IUserReportDbContext ctx)
    {
        _ctx = ctx;
    }

    public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        return _ctx.Notifications
            .CountAsync(n => !n.IsRead && (n.UserId == null || n.UserId == request.UserId), cancellationToken);
    }
}

public record MarkNotificationReadCommand(Guid Id, Guid? UserId) : IRequest<bool>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly IUserReportDbContext _ctx;

    public MarkNotificationReadCommandHandler(IUserReportDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _ctx.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && (n.UserId == null || n.UserId == request.UserId), cancellationToken);

        if (notification == null) return false;

        notification.IsRead = true;
        await _ctx.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record MarkAllNotificationsReadCommand(Guid? UserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IUserReportDbContext _ctx;

    public MarkAllNotificationsReadCommandHandler(IUserReportDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var notifications = await _ctx.Notifications
            .Where(n => !n.IsRead && (n.UserId == null || n.UserId == request.UserId))
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _ctx.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }
}
