namespace UserReportService.Application.Features.Backups;

public static class BackupPathPolicy
{
    public static string ResolveBackupPath(string rootPath, string backupId)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) throw new InvalidOperationException("Backup root path is required.");
        if (string.IsNullOrWhiteSpace(backupId)) throw new InvalidOperationException("Backup id is required.");
        if (backupId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || backupId.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Backup id contains invalid path characters.");
        }

        var root = Path.GetFullPath(rootPath);
        var candidate = Path.GetFullPath(Path.Combine(root, backupId));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Backup path escapes configured root.");
        }

        return candidate;
    }
}
