namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public sealed record BulkImportSourceContext(
    Guid JobId,
    Guid TenantId,
    Guid FacilityId,
    string SourceProviderKind,
    string? ObservabilityJson,
    string SourceSystem);

public interface IBulkImportSourceContextAccessor
{
    BulkImportSourceContext? Current { get; set; }
}

public sealed class BulkImportSourceContextAccessor : IBulkImportSourceContextAccessor
{
    public BulkImportSourceContext? Current { get; set; }
}
