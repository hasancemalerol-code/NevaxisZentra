namespace NevaxisFactOS.Persistence.Authorization;

public sealed class PermissionDefinition
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public string NormalizedKey { get; set; } = string.Empty;
    public required string Description { get; set; }
    public string TurkishLabel { get; set; } = string.Empty;
    public string ModuleResourceCode { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string SensitivityClassification { get; set; } = "standard";
    public bool RequiresRecentAuth { get; set; }
    public string SupportedRecordScopes { get; set; } = "facility,tenant";
    public AuthorizationStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
