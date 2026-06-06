using UserReportService.Application.Features.Backups;
using UserReportService.Application.Features.Permissions;
using UserReportService.Application.Features.Reports;

var tests = new List<(string Name, Action Test)>
{
    ("calculates gross profit and margin", CalculatesGrossProfitAndMargin),
    ("returns warehouse default permissions", ReturnsWarehouseDefaultPermissions),
    ("rejects backup path traversal", RejectsBackupPathTraversal),
    ("resolves backup path inside configured root", ResolvesBackupPathInsideConfiguredRoot),
};

foreach (var test in tests)
{
    try
    {
        test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
        Environment.ExitCode = 1;
        return;
    }
}

static void CalculatesGrossProfitAndMargin()
{
    var result = ProfitPolicy.Calculate(revenue: 1_000_000m, cost: 650_000m);

    AssertEqual(1_000_000m, result.Revenue, nameof(result.Revenue));
    AssertEqual(650_000m, result.Cost, nameof(result.Cost));
    AssertEqual(350_000m, result.GrossProfit, nameof(result.GrossProfit));
    AssertEqual(35m, result.MarginPercent, nameof(result.MarginPercent));
}

static void ReturnsWarehouseDefaultPermissions()
{
    var permissions = PermissionCatalog.GetDefaultsForRole("Warehouse");

    AssertTrue(permissions.Contains("inventory.read"), "Warehouse can read inventory");
    AssertTrue(permissions.Contains("stocktake.write"), "Warehouse can perform stocktake");
    AssertTrue(!permissions.Contains("backup.restore"), "Warehouse cannot restore backup");
}

static void RejectsBackupPathTraversal()
{
    AssertThrows<InvalidOperationException>(() =>
        BackupPathPolicy.ResolveBackupPath(
            rootPath: @"D:\BaiTapLonFullStack\backups",
            backupId: @"..\outside"));
}

static void ResolvesBackupPathInsideConfiguredRoot()
{
    var path = BackupPathPolicy.ResolveBackupPath(
        rootPath: @"D:\BaiTapLonFullStack\backups",
        backupId: "backup-20260530");

    AssertTrue(path.EndsWith(@"backups\backup-20260530", StringComparison.OrdinalIgnoreCase), nameof(path));
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException(name);
    }
}

static void AssertThrows<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name} was not thrown.");
}
