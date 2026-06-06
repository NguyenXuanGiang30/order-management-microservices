using MediatR;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Application.Features.Permissions;

public record PermissionDto(string Code, string Name, string Group, string? Description);
public record RolePermissionsDto(string Role, IReadOnlyList<string> Permissions);

public static class PermissionResolver
{
    public static async Task<IReadOnlyList<string>> GetPermissionsForRoleAsync(IUserReportDbContext ctx, string role, CancellationToken ct)
    {
        var configured = await ctx.RolePermissions
            .AsNoTracking()
            .Where(p => p.Role == role)
            .Select(p => p.PermissionCode)
            .OrderBy(p => p)
            .ToListAsync(ct);

        return configured.Count > 0 ? configured : PermissionCatalog.GetDefaultsForRole(role);
    }
}

public record GetPermissionsQuery : IRequest<List<PermissionDto>>;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
{
    public Task<List<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var items = PermissionCatalog.All
            .Select(code => new PermissionDto(code, ToName(code), code.Split('.')[0], null))
            .ToList();

        return Task.FromResult(items);
    }

    private static string ToName(string code)
    {
        return string.Join(' ', code.Split('.').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}

public record GetRolePermissionsQuery(string Role) : IRequest<RolePermissionsDto>;

public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, RolePermissionsDto>
{
    private readonly IUserReportDbContext _ctx;

    public GetRolePermissionsQueryHandler(IUserReportDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<RolePermissionsDto> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await PermissionResolver.GetPermissionsForRoleAsync(_ctx, request.Role, cancellationToken);
        return new RolePermissionsDto(request.Role, permissions);
    }
}

public record UpdateRolePermissionsCommand(string Role, List<string> Permissions) : IRequest<RolePermissionsDto>;

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, RolePermissionsDto>
{
    private readonly IUserReportDbContext _ctx;

    public UpdateRolePermissionsCommandHandler(IUserReportDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<RolePermissionsDto> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var valid = PermissionCatalog.All.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = request.Permissions
            .Where(permission => valid.Contains(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToList();

        var existing = await _ctx.RolePermissions.Where(p => p.Role == request.Role).ToListAsync(cancellationToken);
        _ctx.RolePermissions.RemoveRange(existing);

        foreach (var permission in selected)
        {
            _ctx.RolePermissions.Add(new RolePermission
            {
                Role = request.Role,
                PermissionCode = permission
            });
        }

        await _ctx.SaveChangesAsync(cancellationToken);
        return new RolePermissionsDto(request.Role, selected);
    }
}
