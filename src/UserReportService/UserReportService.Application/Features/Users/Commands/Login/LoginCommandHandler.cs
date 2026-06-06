using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.Common.Security;
using UserReportService.Application.Features.Permissions;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.Application.Features.Users.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserReportDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public LoginCommandHandler(IUserReportDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user == null)
            return new LoginResponse(false, null, null, 0, "Tên đăng nhập hoặc mật khẩu không đúng.");

        if (!PasswordHasher.VerifyPassword(user.PasswordHash, request.Password))
            return new LoginResponse(false, null, null, 0, "Tên đăng nhập hoặc mật khẩu không đúng.");

        user.LastLoginAt = DateTime.UtcNow;

        // Tạo Access Token
        int expiresInMinutes = 60;
        var permissions = await PermissionResolver.GetPermissionsForRoleAsync(_context, user.Role, cancellationToken);
        string accessToken = _jwtTokenService.GenerateToken(user.Id, user.Username, user.Role, user.Email, permissions);

        // Tạo Refresh Token + lưu DB
        var refreshTokenStr = GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "system"
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userInfo = new LoginUserInfo(user.Id, user.Username, user.FullName, user.Role, user.AvatarUrl, permissions);

        return new LoginResponse(true, accessToken, refreshTokenStr, expiresInMinutes * 60, "Đăng nhập thành công.", userInfo);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
