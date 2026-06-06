using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UserReportService.Application.Common.Security;

/// <summary>
/// Dịch vụ phát hành JWT Token thực tế sử dụng HMAC-SHA256.
/// UserReportService đóng vai trò Identity Provider cho toàn hệ thống.
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Tạo JWT Access Token cho user đã xác thực.
    /// Token chứa claims: sub (userId), unique_name (username), role, email.
    /// </summary>
    public string GenerateToken(Guid userId, string username, string role, string? email = null, IEnumerable<string>? permissions = null)
    {
        var secret = _configuration["JwtSettings:Secret"]
            ?? "ThisIsAVerySecretKeyThatMustBeAtLeast32CharactersLongToWorkWithHmacSha256!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "retail_user_report_service";
        var audience = _configuration["JwtSettings:Audience"] ?? "retail_app";
        var expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out var min) ? min : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));

        foreach (var permission in permissions ?? [])
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
