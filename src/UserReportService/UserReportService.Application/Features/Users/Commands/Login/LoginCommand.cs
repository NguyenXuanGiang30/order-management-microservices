using MediatR;

namespace UserReportService.Application.Features.Users.Commands.Login;

public record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;

public record LoginResponse(
    bool IsSuccess,
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string Message,
    LoginUserInfo? User = null);

public record LoginUserInfo(Guid Id, string Username, string FullName, string Role, string? AvatarUrl, IReadOnlyList<string> Permissions);
