namespace CrevaFactOS.Persistence.Authorization;

public sealed class PermissionDefinition
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Description { get; set; }
    public AuthorizationStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
