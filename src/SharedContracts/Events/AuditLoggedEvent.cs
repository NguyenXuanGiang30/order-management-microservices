namespace SharedContracts.Events;

public record AuditLoggedEvent
{
    public Guid AuditId { get; init; } = Guid.NewGuid();
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string ServiceName { get; init; } = null!;
    public string Action { get; init; } = null!;
    public string EntityType { get; init; } = null!;
    public string? EntityId { get; init; }
    public string Severity { get; init; } = "Info";
    public string? Description { get; init; }
    public string? IpAddress { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
