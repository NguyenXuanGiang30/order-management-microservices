using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UserReportService.Application.DTOs;
using UserReportService.Application.Features.Backups;
using UserReportService.Application.Interfaces;
using UserReportService.Application.Models;

namespace UserReportService.API.Services;

public class JsonUserReportBackupService : IUserReportBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IUserReportDbContext _context;
    private readonly IConfiguration _configuration;

    public JsonUserReportBackupService(IUserReportDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<BackupRecordDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await _context.BackupRecords.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BackupRecordDto(b.Id, b.BackupId, b.Status, b.CreatedByName, b.CreatedAt, b.RestoredAt, b.Note))
            .ToListAsync(cancellationToken);
    }

    public async Task<BackupRecordDto> CreateAsync(string createdByName, string? note, CancellationToken cancellationToken)
    {
        var backupId = $"user-report-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var filePath = ResolveBackupFilePath(backupId);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var snapshot = new UserReportBackupSnapshot
        {
            BackupId = backupId,
            CreatedAt = DateTime.UtcNow,
            CreatedByName = createdByName,
            Note = note,
            Permissions = await _context.Permissions.AsNoTracking().ToListAsync(cancellationToken),
            RolePermissions = await _context.RolePermissions.AsNoTracking().ToListAsync(cancellationToken),
            DailySalesSummaries = await _context.DailySalesSummaries.AsNoTracking().ToListAsync(cancellationToken),
            MonthlySalesSummaries = await _context.MonthlySalesSummaries.AsNoTracking().ToListAsync(cancellationToken),
            DailyProfitSummaries = await _context.DailyProfitSummaries.AsNoTracking().ToListAsync(cancellationToken),
            MonthlyProfitSummaries = await _context.MonthlyProfitSummaries.AsNoTracking().ToListAsync(cancellationToken),
            ProductProfitSummaries = await _context.ProductProfitSummaries.AsNoTracking().ToListAsync(cancellationToken),
            TopProductSummaries = await _context.TopProductSummaries.AsNoTracking().ToListAsync(cancellationToken),
            TopCustomerSummaries = await _context.TopCustomerSummaries.AsNoTracking().ToListAsync(cancellationToken),
            Notifications = await _context.Notifications.AsNoTracking().ToListAsync(cancellationToken)
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        var record = new BackupRecord
        {
            BackupId = backupId,
            FilePath = filePath,
            Status = "Completed",
            CreatedByName = createdByName,
            CreatedAt = DateTime.UtcNow,
            Note = note
        };

        _context.BackupRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    public async Task<BackupRecordDto?> RestoreAsync(string backupId, string restoredByName, bool confirm, string? note, CancellationToken cancellationToken)
    {
        if (!confirm)
        {
            throw new InvalidOperationException("Restore requires explicit confirmation.");
        }

        var record = await _context.BackupRecords.FirstOrDefaultAsync(b => b.BackupId == backupId, cancellationToken);
        if (record == null)
        {
            return null;
        }

        if (!File.Exists(record.FilePath))
        {
            throw new FileNotFoundException("Backup file was not found.", record.FilePath);
        }

        var json = await File.ReadAllTextAsync(record.FilePath, cancellationToken);
        var snapshot = JsonSerializer.Deserialize<UserReportBackupSnapshot>(json, JsonOptions)
            ?? throw new InvalidOperationException("Backup file is not valid.");

        await ReplaceAsync(_context.Permissions, snapshot.Permissions, cancellationToken);
        await ReplaceAsync(_context.RolePermissions, snapshot.RolePermissions, cancellationToken);
        await ReplaceAsync(_context.DailySalesSummaries, snapshot.DailySalesSummaries, cancellationToken);
        await ReplaceAsync(_context.MonthlySalesSummaries, snapshot.MonthlySalesSummaries, cancellationToken);
        await ReplaceAsync(_context.DailyProfitSummaries, snapshot.DailyProfitSummaries, cancellationToken);
        await ReplaceAsync(_context.MonthlyProfitSummaries, snapshot.MonthlyProfitSummaries, cancellationToken);
        await ReplaceAsync(_context.ProductProfitSummaries, snapshot.ProductProfitSummaries, cancellationToken);
        await ReplaceAsync(_context.TopProductSummaries, snapshot.TopProductSummaries, cancellationToken);
        await ReplaceAsync(_context.TopCustomerSummaries, snapshot.TopCustomerSummaries, cancellationToken);
        await ReplaceAsync(_context.Notifications, snapshot.Notifications, cancellationToken);

        record.Status = "Restored";
        record.RestoredAt = DateTime.UtcNow;
        record.Note = string.IsNullOrWhiteSpace(note)
            ? $"Restored by {restoredByName}"
            : $"{note} Restored by {restoredByName}";

        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(record);
    }

    private async Task ReplaceAsync<TEntity>(DbSet<TEntity> set, List<TEntity> replacements, CancellationToken ct)
        where TEntity : class
    {
        await set.ExecuteDeleteAsync(ct);
        set.AddRange(replacements);
    }

    private string ResolveBackupFilePath(string backupId)
    {
        var rootPath = _configuration["Backups:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "backups");
        return BackupPathPolicy.ResolveBackupPath(rootPath, $"{backupId}.json");
    }

    private static BackupRecordDto ToDto(BackupRecord record)
    {
        return new BackupRecordDto(record.Id, record.BackupId, record.Status, record.CreatedByName, record.CreatedAt, record.RestoredAt, record.Note);
    }

    private sealed class UserReportBackupSnapshot
    {
        public string BackupId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = null!;
        public string? Note { get; set; }
        public List<Permission> Permissions { get; set; } = [];
        public List<RolePermission> RolePermissions { get; set; } = [];
        public List<DailySalesSummary> DailySalesSummaries { get; set; } = [];
        public List<MonthlySalesSummary> MonthlySalesSummaries { get; set; } = [];
        public List<DailyProfitSummary> DailyProfitSummaries { get; set; } = [];
        public List<MonthlyProfitSummary> MonthlyProfitSummaries { get; set; } = [];
        public List<ProductProfitSummary> ProductProfitSummaries { get; set; } = [];
        public List<TopProductSummary> TopProductSummaries { get; set; } = [];
        public List<TopCustomerSummary> TopCustomerSummaries { get; set; } = [];
        public List<Notification> Notifications { get; set; } = [];
    }
}
