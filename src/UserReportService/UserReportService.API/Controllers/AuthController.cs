using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserReportService.Application.Common.Models;
using UserReportService.Application.Features.Auth;
using UserReportService.Application.Features.Users.Commands.Login;

namespace UserReportService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) { _mediator = mediator; }

    /// <summary>POST /api/auth/login — Đăng nhập</summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) return Unauthorized(ApiResponse<LoginResponse>.FailResponse(result.Message));
        return Ok(ApiResponse<LoginResponse>.SuccessResponse(result, "Đăng nhập thành công."));
    }

    /// <summary>POST /api/auth/refresh-token — Cấp lại Access Token</summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) return Unauthorized(ApiResponse<RefreshTokenResponse>.FailResponse(result.Message));
        return Ok(ApiResponse<RefreshTokenResponse>.SuccessResponse(result));
    }

    /// <summary>POST /api/auth/logout — Đăng xuất (Revoke Refresh Token)</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] LogoutCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<bool>.SuccessResponse(result, "Đăng xuất thành công."));
    }

    /// <summary>PUT /api/auth/change-password — Đổi mật khẩu</summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ChangePasswordResponse>>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new ChangePasswordCommand(userId, dto.OldPassword, dto.NewPassword));
        if (!result.IsSuccess) return BadRequest(ApiResponse<ChangePasswordResponse>.FailResponse(result.Message));
        return Ok(ApiResponse<ChangePasswordResponse>.SuccessResponse(result));
    }
}

public record ChangePasswordDto(string OldPassword, string NewPassword);
