namespace UserReportService.Application.Features.Permissions;

public static class PermissionCatalog
{
    public static readonly string[] All =
    [
        "dashboard.read",
        "orders.read",
        "orders.write",
        "orders.return",
        "pos.sell",
        "inventory.read",
        "inventory.write",
        "stocktake.write",
        "purchasing.write",
        "customers.read",
        "customers.write",
        "suppliers.read",
        "suppliers.write",
        "reports.read",
        "reports.profit",
        "audit.read",
        "users.manage",
        "permissions.manage",
        "notifications.read",
        "backup.create",
        "backup.restore"
    ];

    public static IReadOnlyList<string> GetDefaultsForRole(string role)
    {
        return role switch
        {
            "Admin" => All,
            "Sales" =>
            [
                "dashboard.read",
                "pos.sell",
                "orders.read",
                "orders.write",
                "orders.return",
                "customers.read",
                "customers.write",
                "notifications.read"
            ],
            "Warehouse" =>
            [
                "dashboard.read",
                "inventory.read",
                "inventory.write",
                "stocktake.write",
                "purchasing.write",
                "suppliers.read",
                "suppliers.write",
                "notifications.read"
            ],
            _ => []
        };
    }
}
