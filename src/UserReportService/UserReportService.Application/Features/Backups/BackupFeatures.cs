using MediatR;
using UserReportService.Application.DTOs;

namespace UserReportService.Application.Features.Backups;

public interface IUserReportBackupService
{
    Task<List<BackupRecordDto>> ListAsync(CancellationToken cancellationToken);
    Task<BackupRecordDto> CreateAsync(string createdByName, string? note, CancellationToken cancellationToken);
    Task<BackupRecordDto?> RestoreAsync(string backupId, string restoredByName, bool confirm, string? note, CancellationToken cancellationToken);
}

public record GetBackupsQuery : IRequest<List<BackupRecordDto>>;

public class GetBackupsQueryHandler : IRequestHandler<GetBackupsQuery, List<BackupRecordDto>>
{
    private readonly IUserReportBackupService _backupService;

    public GetBackupsQueryHandler(IUserReportBackupService backupService)
    {
        _backupService = backupService;
    }

    public Task<List<BackupRecordDto>> Handle(GetBackupsQuery request, CancellationToken cancellationToken)
    {
        return _backupService.ListAsync(cancellationToken);
    }
}

public record CreateBackupCommand(string CreatedByName, string? Note) : IRequest<BackupRecordDto>;

public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, BackupRecordDto>
{
    private readonly IUserReportBackupService _backupService;

    public CreateBackupCommandHandler(IUserReportBackupService backupService)
    {
        _backupService = backupService;
    }

    public Task<BackupRecordDto> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        return _backupService.CreateAsync(request.CreatedByName, request.Note, cancellationToken);
    }
}

public record RestoreBackupCommand(string BackupId, string RestoredByName, bool Confirm, string? Note) : IRequest<BackupRecordDto?>;

public class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, BackupRecordDto?>
{
    private readonly IUserReportBackupService _backupService;

    public RestoreBackupCommandHandler(IUserReportBackupService backupService)
    {
        _backupService = backupService;
    }

    public Task<BackupRecordDto?> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        return _backupService.RestoreAsync(request.BackupId, request.RestoredByName, request.Confirm, request.Note, cancellationToken);
    }
}
