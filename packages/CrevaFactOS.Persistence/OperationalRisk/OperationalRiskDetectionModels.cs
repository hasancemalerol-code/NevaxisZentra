namespace CrevaFactOS.Persistence.OperationalRisk;

public sealed record OperationalRiskDetectionProfileReadModel(
    Guid Id,
    string ProfileCode,
    string DisplayName,
    string Status,
    string RuleSetVersion,
    string SourceProvenance,
    uint Version,
    IReadOnlyList<OperationalRiskDetectionRuleReadModel> Rules,
    bool IsPilotData);

public sealed record OperationalRiskDetectionRuleReadModel(
    Guid Id,
    string RuleCode,
    string SourceModule,
    string SourceOutcome,
    string RiskCategory,
    int DefaultLikelihood,
    int DefaultImpact,
    decimal? ThresholdVarianceBand,
    string DriverReasonCode,
    int PriorityOrder,
    bool IsActive);

public sealed record OperationalRiskDetectionRunSummaryReadModel(
    Guid Id,
    string RunReference,
    Guid ProfileId,
    string ProfileCode,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string Status,
    int SourceCount,
    int ResultCount,
    string RuleSetVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool IsPilotData);

public sealed record OperationalRiskDetectionRunDetailReadModel(
    Guid Id,
    string RunReference,
    Guid ProfileId,
    string ProfileCode,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string Status,
    int SourceCount,
    int ResultCount,
    string RuleSetVersion,
    string SourceProvenance,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<OperationalRiskDetectionResultReadModel> Results,
    bool IsPilotData);

public sealed record OperationalRiskDetectionResultReadModel(
    Guid Id,
    string SourceModule,
    string SourceReference,
    string RiskCategory,
    string ScopeType,
    string? ScopeReference,
    string EvidenceState,
    string SourceOutcome,
    string DetectionOutcome,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int? LikelihoodLevel,
    int? ImpactLevel,
    int? RiskScore,
    string? SeverityBand,
    string? LeadingDriverCode,
    Guid? LinkedCaseId,
    string DetectionKey,
    string SourceAssessmentReference,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<OperationalRiskDetectionDriverReadModel> Drivers);

public sealed record OperationalRiskDetectionDriverReadModel(
    Guid Id,
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    string SupportingText,
    int SortOrder);

public sealed record CreateOperationalRiskDetectionProfileCommand(
    string ProfileCode,
    string DisplayName,
    string SourceProvenance,
    string IdempotencyKey,
    IReadOnlyList<CreateOperationalRiskDetectionRuleCommand> Rules);

public sealed record CreateOperationalRiskDetectionRuleCommand(
    string RuleCode,
    string SourceModule,
    string SourceOutcome,
    string RiskCategory,
    int DefaultLikelihood,
    int DefaultImpact,
    decimal? ThresholdVarianceBand,
    string DriverReasonCode,
    int PriorityOrder,
    bool IsActive);

public sealed record UpdateOperationalRiskDetectionProfileCommand(
    Guid ProfileId,
    uint ExpectedVersion,
    string DisplayName,
    string Status,
    string SourceProvenance);

public sealed record ExecuteOperationalRiskDetectionRunCommand(
    Guid ProfileId,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string SourceProvenance,
    string IdempotencyKey);

public interface IOperationalRiskDetectionRepository
{
    Task<IReadOnlyList<OperationalRiskDetectionProfileReadModel>> ListProfilesAsync(
        OperationalRiskOperationContext context,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskDetectionProfileReadModel?> GetProfileByIdAsync(
        OperationalRiskOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> CreateProfileAsync(
        OperationalRiskOperationContext context,
        CreateOperationalRiskDetectionProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> UpdateProfileAsync(
        OperationalRiskOperationContext context,
        UpdateOperationalRiskDetectionProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalRiskDetectionRunSummaryReadModel>> ListRunsAsync(
        OperationalRiskOperationContext context,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskDetectionRunDetailReadModel?> GetRunByIdAsync(
        OperationalRiskOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<OperationalRiskWriteResult> ExecuteRunAsync(
        OperationalRiskOperationContext context,
        ExecuteOperationalRiskDetectionRunCommand command,
        CancellationToken cancellationToken = default);
}
