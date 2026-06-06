using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserReportService.API.Security;
using UserReportService.Application.Common.Models;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Permissions;
using UserReportService.Application.Features.Users;

namespace UserReportService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) { _mediator = mediator; }

    /// <summary>GET /api/users/me — Thông tin cá nhân</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetMeQuery(userId));
        if (result == null) return NotFound(ApiResponse<UserDto>.FailResponse("Không tìm thấy người dùng."));
        return Ok(ApiResponse<UserDto>.SuccessResponse(result));
    }

    /// <summary>GET /api/users — Danh sách người dùng (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetUsers(
        [FromQuery] string? search, [FromQuery] string? role,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetUsersQuery(search, role, page, pageSize));
        return Ok(ApiResponse<PagedResponse<UserDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/users/{id} — Chi tiết người dùng (Admin)</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        var result = await _mediator.Send(new GetMeQuery(id));
        if (result == null) return NotFound(ApiResponse<UserDto>.FailResponse("Không tìm thấy người dùng."));
        return Ok(ApiResponse<UserDto>.SuccessResponse(result));
    }

    /// <summary>POST /api/users — Tạo tài khoản mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id },
            ApiResponse<UserDto>.SuccessResponse(result, "Tạo tài khoản thành công."));
    }

    /// <summary>PUT /api/users/{id} — Cập nhật thông tin (Admin hoặc chính user)</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var result = await _mediator.Send(new UpdateUserCommand(id, dto.FullName, dto.Email, dto.Phone, dto.Role, dto.AvatarUrl));
        if (result == null) return NotFound(ApiResponse<UserDto>.FailResponse("Không tìm thấy người dùng."));
        return Ok(ApiResponse<UserDto>.SuccessResponse(result, "Cập nhật thành công."));
    }

    /// <summary>PUT /api/users/{id}/toggle-active — Kích hoạt / Vô hiệu hóa (Admin)</summary>
    [HttpPut("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleActive(Guid id)
    {
        var result = await _mediator.Send(new ToggleUserActiveCommand(id));
        if (!result) return NotFound(ApiResponse<bool>.FailResponse("Không tìm thấy người dùng."));
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Cập nhật trạng thái thành công."));
    }

    /// <summary>GET /api/users/permissions - Permission catalog</summary>
    [HttpGet("permissions")]
    [RequirePermission("permissions.manage")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetPermissions()
    {
        var result = await _mediator.Send(new GetPermissionsQuery());
        return Ok(ApiResponse<List<PermissionDto>>.SuccessResponse(result));
    }

    /// <summary>GET /api/users/roles/{role}/permissions - Role permissions</summary>
    [HttpGet("roles/{role}/permissions")]
    [RequirePermission("permissions.manage")]
    public async Task<ActionResult<ApiResponse<RolePermissionsDto>>> GetRolePermissions(string role)
    {
        var result = await _mediator.Send(new GetRolePermissionsQuery(role));
        return Ok(ApiResponse<RolePermissionsDto>.SuccessResponse(result));
    }

    /// <summary>PUT /api/users/roles/{role}/permissions - Update role permissions</summary>
    [HttpPut("roles/{role}/permissions")]
    [RequirePermission("permissions.manage")]
    public async Task<ActionResult<ApiResponse<RolePermissionsDto>>> UpdateRolePermissions(
        string role,
        [FromBody] UpdateRolePermissionsRequest request)
    {
        var result = await _mediator.Send(new UpdateRolePermissionsCommand(role, request.Permissions));
        return Ok(ApiResponse<RolePermissionsDto>.SuccessResponse(result, "Cập nhật quyền thành công."));
    }
}

public record UpdateUserDto(string? FullName, string? Email, string? Phone, string? Role, string? AvatarUrl);
public record UpdateRolePermissionsRequest(List<string> Permissions);
