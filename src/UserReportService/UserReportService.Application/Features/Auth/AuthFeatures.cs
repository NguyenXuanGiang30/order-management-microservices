using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Common.Security;
using UserReportService.Application.Features.Permissions;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Application.Features.Auth;

// ======================== Refresh Token ========================
public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResponse>;
public record RefreshTokenResponse(bool IsSuccess, string? AccessToken, string? RefreshToken, int ExpiresIn, string Message);

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUserReportDbContext _context;
    private readonly JwtTokenService _jwtTokenService;
    public RefreshTokenCommandHandler(IUserReportDbContext ctx, JwtTokenService jwt) { _context = ctx; _jwtTokenService = jwt; }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var token = await _context.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (token == null || !token.IsActiveToken)
            return new RefreshTokenResponse(false, null, null, 0, "Refresh token không hợp lệ hoặc đã hết hạn.");

        // Revoke token cũ
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = "system";

        // Tạo token mới (rotation)
        var newRefreshStr = GenerateRefreshToken();
        token.ReplacedByToken = newRefreshStr;

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId, Token = newRefreshStr,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedByIp = "system"
        };
        _context.RefreshTokens.Add(newRefreshToken);

        var user = token.User;
        var permissions = await PermissionResolver.GetPermissionsForRoleAsync(_context, user.Role, ct);
        var accessToken = _jwtTokenService.GenerateToken(user.Id, user.Username, user.Role, user.Email, permissions);
        await _context.SaveChangesAsync(ct);

        return new RefreshTokenResponse(true, accessToken, newRefreshStr, 3600, "Cấp lại token thành công.");
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// ======================== Logout ========================
public record LogoutCommand(string RefreshToken) : IRequest<bool>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IUserReportDbContext _context;
    public LogoutCommandHandler(IUserReportDbContext ctx) { _context = ctx; }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken ct)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);
        if (token == null) return false;

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = "system";
        await _context.SaveChangesAsync(ct);
        return true;
    }
}

// ======================== Change Password ========================
public record ChangePasswordCommand(Guid UserId, string OldPassword, string NewPassword) : IRequest<ChangePasswordResponse>;
public record ChangePasswordResponse(bool IsSuccess, string Message);

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IUserReportDbContext _context;
    public ChangePasswordCommandHandler(IUserReportDbContext ctx) { _context = ctx; }

    public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, ct);
        if (user == null) return new ChangePasswordResponse(false, "Không tìm thấy người dùng.");

        if (!PasswordHasher.VerifyPassword(user.PasswordHash, request.OldPassword))
            return new ChangePasswordResponse(false, "Mật khẩu hiện tại không đúng.");

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync(ct);
        return new ChangePasswordResponse(true, "Đổi mật khẩu thành công.");
    }
}
