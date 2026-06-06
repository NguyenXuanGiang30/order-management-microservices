using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserReportService.API.Security;
using UserReportService.Application.Common.Models;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Backups;

namespace UserReportService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BackupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission("backup.create")]
    public async Task<ActionResult<ApiResponse<List<BackupRecordDto>>>> GetBackups()
    {
        var result = await _mediator.Send(new GetBackupsQuery());
        return Ok(ApiResponse<List<BackupRecordDto>>.SuccessResponse(result));
    }

    [HttpPost]
    [RequirePermission("backup.create")]
    public async Task<ActionResult<ApiResponse<BackupRecordDto>>> CreateBackup([FromBody] CreateBackupRequest request)
    {
        var result = await _mediator.Send(new CreateBackupCommand(User.Identity?.Name ?? "System", request.Note));
        return Ok(ApiResponse<BackupRecordDto>.SuccessResponse(result, "Backup created."));
    }

    [HttpPost("{backupId}/restore")]
    [RequirePermission("backup.restore")]
    public async Task<ActionResult<ApiResponse<BackupRecordDto>>> RestoreBackup(
        string backupId,
        [FromBody] RestoreBackupRequest request)
    {
        var result = await _mediator.Send(new RestoreBackupCommand(
            backupId,
            User.Identity?.Name ?? "System",
            request.Confirm,
            request.Note));

        if (result == null) return NotFound(ApiResponse<BackupRecordDto>.FailResponse("Backup not found."));
        return Ok(ApiResponse<BackupRecordDto>.SuccessResponse(result, "Backup restored."));
    }
}

public record CreateBackupRequest(string? Note);
public record RestoreBackupRequest(bool Confirm, string? Note);
