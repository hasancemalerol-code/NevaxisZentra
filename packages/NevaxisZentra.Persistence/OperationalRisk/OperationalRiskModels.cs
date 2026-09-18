namespace NevaxisZentra.Persistence.OperationalRisk;

public sealed record OperationalRiskOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record OperationalRiskCaseSummaryReadModel(
    Guid Id,
    string CaseReference,
    string Category,
    string ScopeType,
    string? ScopeReference,
    string Title,
    string EvidenceState,
    int? LikelihoodLevel,
    int? ImpactLevel,
    int? RiskScore,
    string? SeverityBand,
    string AssessmentOutcome,
    string LifecycleStatus,
    string RuleSetVersion,
    DateTimeOffset FirstDetectedAtUtc,
    DateTimeOffset LastObservedAtUtc,
    uint Version,
    bool IsPilotData);

public sealed record OperationalRiskEvidenceReadModel(
    Guid Id,
    string EvidenceTypeCode,
    string EvidenceState,
    DateTimeOffset ObservedAtUtc,
    string? SourceModule,
    string? SourceReference,
    string Summary,
    decimal? NumericValue,
    string? Unit,
    string? ComparisonReference,
    string SourceProvenance,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record OperationalRiskStatusHistoryReadModel(
    Guid Id,
    string PreviousStatus,
    string NewStatus,
    string? ReasonNote,
    string ActorSubjectId,
    DateTimeOffset OccurredAtUtc,
    uint ExpectedCaseVersion,
    string SourceProvenance);

public sealed record OperationalRiskCaseDetailReadModel(
    Guid Id,
    string CaseReference,
    string Category,
    string ScopeType,
    string? ScopeReference,
    string Title,
    string Description,
    string EvidenceState,
    int? LikelihoodLevel,
    int? ImpactLevel,
    int? RiskScore,
    string? SeverityBand,
    string AssessmentOutcome,
    string LifecycleStatus,
    string RuleSetVersion,
    string? SourceModule,
    string? SourceReference,
    string SourceProvenance,
    DateTimeOffset FirstDetectedAtUtc,
    DateTimeOffset LastObservedAtUtc,
    uint Version,
    IReadOnlyList<OperationalRiskEvidenceReadModel> Evidence,
    IReadOnlyList<OperationalRiskStatusHistoryReadModel> StatusHistory,
    bool IsPilotData);

public sealed record OperationalRiskCaseListReadModel(
    IReadOnlyList<OperationalRiskCaseSummaryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record OperationalRiskCaseListFilters(
    string? Category,
    string? ScopeType,
    string? LifecycleStatus,
    string? SeverityBand,
    string? AssessmentOutcome,
    int Page,
    int PageSize);

public sealed record CreateOperationalRiskCaseCommand(
    string Category,
    string ScopeType,
    string? ScopeReference,
    string Title,
    string Description,
    string EvidenceState,
    int? LikelihoodLevel,
    int? ImpactLevel,
    string? SourceModule,
    string? SourceReference,
    string SourceProvenance,
    DateTimeOffset FirstDetectedAtUtc,
    DateTimeOffset LastObservedAtUtc,
    string IdempotencyKey);

public sealed record AppendOperationalRiskEvidenceCommand(
    Guid CaseId,
    string EvidenceTypeCode,
    string EvidenceState,
    DateTimeOffset ObservedAtUtc,
    string? SourceModule,
    string? SourceReference,
    string Summary,
    decimal? NumericValue,
    string? Unit,
    string? ComparisonReference,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record UpdateOperationalRiskAssessmentCommand(
    Guid CaseId,
    uint ExpectedVersion,
    string EvidenceState,
    int? LikelihoodLevel,
    int? ImpactLevel,
    DateTimeOffset? LastObservedAtUtc);

public sealed record TransitionOperationalRiskCaseCommand(
    Guid CaseId,
    uint ExpectedVersion,
    string TargetStatus,
    string? Note,
    string SourceProvenance);

public sealed class OperationalRiskWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static OperationalRiskWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static OperationalRiskWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
