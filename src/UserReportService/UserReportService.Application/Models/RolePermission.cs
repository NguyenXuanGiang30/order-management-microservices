namespace UserReportService.Application.Models;

public class RolePermission : BaseEntity
{
    public string Role { get; set; } = null!;
    public string PermissionCode { get; set; } = null!;
}
