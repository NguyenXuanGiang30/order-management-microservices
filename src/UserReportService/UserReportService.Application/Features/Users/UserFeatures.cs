using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Common.Models;
using UserReportService.Application.Common.Security;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Permissions;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Application.Features.Users;

// ======================== Get Users (Paged + Search) ========================
public record GetUsersQuery(string? Search, string? Role, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserDto>>
{
    private readonly IUserReportDbContext _ctx;
    private readonly IMapper _mapper;
    public GetUsersQueryHandler(IUserReportDbContext ctx, IMapper m) { _ctx = ctx; _mapper = m; }

    public async Task<PagedResponse<UserDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var q = _ctx.Users.Where(u => u.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            q = q.Where(u => u.Username.ToLower().Contains(s) || u.FullName.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(req.Role))
            q = q.Where(u => u.Role == req.Role);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(u => u.Username).Skip((req.PageNumber - 1) * req.PageSize).Take(req.PageSize).AsNoTracking().ToListAsync(ct);
        var rolePermissions = await UserDtoProjection.LoadRolePermissionsAsync(_ctx, items.Select(u => u.Role).Distinct(), ct);
        return new PagedResponse<UserDto>
        {
            Items = items.Select(user => UserDtoProjection.ToDto(user, rolePermissions[user.Role])).ToList(),
            PageNumber = req.PageNumber,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}

// ======================== Get Me ========================
public record GetMeQuery(Guid UserId) : IRequest<UserDto?>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto?>
{
    private readonly IUserReportDbContext _ctx;
    private readonly IMapper _mapper;
    public GetMeQueryHandler(IUserReportDbContext ctx, IMapper m) { _ctx = ctx; _mapper = m; }

    public async Task<UserDto?> Handle(GetMeQuery req, CancellationToken ct)
    {
        var user = await _ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user == null) return null;

        var permissions = await PermissionResolver.GetPermissionsForRoleAsync(_ctx, user.Role, ct);
        return UserDtoProjection.ToDto(user, permissions);
    }
}

// ======================== Create User ========================
public record CreateUserCommand(string Username, string Password, string FullName, string? Email, string? Phone, string Role) : IRequest<UserDto>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserReportDbContext _ctx;
    private readonly IMapper _mapper;
    public CreateUserCommandHandler(IUserReportDbContext ctx, IMapper m) { _ctx = ctx; _mapper = m; }

    public async Task<UserDto> Handle(CreateUserCommand req, CancellationToken ct)
    {
        // Kiểm tra username trùng
        var exists = await _ctx.Users.AnyAsync(u => u.Username == req.Username, ct);
        if (exists) throw new InvalidOperationException($"Username '{req.Username}' đã tồn tại.");

        var user = new User
        {
            Username = req.Username,
            PasswordHash = PasswordHasher.HashPassword(req.Password),
            FullName = req.FullName,
            Email = req.Email,
            Phone = req.Phone,
            Role = req.Role
        };
        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync(ct);
        var permissions = await PermissionResolver.GetPermissionsForRoleAsync(_ctx, user.Role, ct);
        return UserDtoProjection.ToDto(user, permissions);
    }
}

// ======================== Update User ========================
public record UpdateUserCommand(Guid Id, string? FullName, string? Email, string? Phone, string? Role, string? AvatarUrl) : IRequest<UserDto?>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto?>
{
    private readonly IUserReportDbContext _ctx;
    private readonly IMapper _mapper;
    public UpdateUserCommandHandler(IUserReportDbContext ctx, IMapper m) { _ctx = ctx; _mapper = m; }

    public async Task<UserDto?> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user == null) return null;

        if (req.FullName != null) user.FullName = req.FullName;
        if (req.Email != null) user.Email = req.Email;
        if (req.Phone != null) user.Phone = req.Phone;
        if (req.Role != null) user.Role = req.Role;
        if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;

        await _ctx.SaveChangesAsync(ct);
        var permissions = await PermissionResolver.GetPermissionsForRoleAsync(_ctx, user.Role, ct);
        return UserDtoProjection.ToDto(user, permissions);
    }
}

internal static class UserDtoProjection
{
    public static async Task<Dictionary<string, IReadOnlyList<string>>> LoadRolePermissionsAsync(
        IUserReportDbContext ctx,
        IEnumerable<string> roles,
        CancellationToken ct)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            result[role] = await PermissionResolver.GetPermissionsForRoleAsync(ctx, role, ct);
        }

        return result;
    }

    public static UserDto ToDto(User user, IReadOnlyList<string> permissions)
    {
        return new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Phone,
            user.AvatarUrl,
            user.Role,
            permissions,
            user.LastLoginAt,
            user.IsActive,
            user.CreatedAt);
    }
}

// ======================== Toggle Active ========================
public record ToggleUserActiveCommand(Guid Id) : IRequest<bool>;

public class ToggleUserActiveCommandHandler : IRequestHandler<ToggleUserActiveCommand, bool>
{
    private readonly IUserReportDbContext _ctx;
    public ToggleUserActiveCommandHandler(IUserReportDbContext ctx) { _ctx = ctx; }

    public async Task<bool> Handle(ToggleUserActiveCommand req, CancellationToken ct)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _ctx.SaveChangesAsync(ct);
        return true;
    }
}
