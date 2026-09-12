namespace NevaxisFactOS.Persistence.OperationalRisk;

public sealed record OperationalRiskSourceSnapshot(
    Guid TenantId,
    Guid FacilityId,
    string SourceModule,
    string SourceEntityType,
    string SourceReference,
    string RiskCategory,
    string ScopeType,
    string? ScopeReference,
    DateTimeOffset ObservedAtUtc,
    string EvidenceState,
    string SourceOutcome,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    string SourceProvenance,
    string SourceRuleSetVersion,
    string SourceAssessmentReference,
    bool IsStale,
    bool IsAvailable);

public sealed record OperationalRiskSourceAdapterResult(
    bool Succeeded,
    IReadOnlyList<OperationalRiskSourceSnapshot> Snapshots,
    string? FailureCode,
    string? FailureReason)
{
    public static OperationalRiskSourceAdapterResult Success(IReadOnlyList<OperationalRiskSourceSnapshot> snapshots) =>
        new(true, snapshots, null, null);

    public static OperationalRiskSourceAdapterResult Unavailable(string module, string reason) =>
        new(false, [], "operational-risk.source-unavailable", $"{module}: {reason}");

    public static OperationalRiskSourceAdapterResult Empty() =>
        new(true, [], null, null);
}

public interface IOperationalRiskEvidenceSource
{
    string SourceModule { get; }

    Task<OperationalRiskSourceAdapterResult> CollectSnapshotsAsync(
        OperationalRiskOperationContext context,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken = default);
}
