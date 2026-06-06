namespace UserReportService.Application.Models;

public class Permission : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Group { get; set; } = null!;
    public string? Description { get; set; }
}
