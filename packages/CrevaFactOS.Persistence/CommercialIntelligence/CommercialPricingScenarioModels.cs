namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialPricingScenarioListFilters(
    int Page = 1,
    int PageSize = 25,
    string? LifecycleStatus = null,
    string? GuardrailResult = null,
    Guid? ProfileId = null,
    Guid? MaterialId = null,
    string? CustomerReference = null);

public sealed record CommercialPricingScenarioDriverReadModel(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedComponentCount,
    string SupportingText);

public sealed record CommercialPricingScenarioEvaluationSnapshotReadModel(
    Guid Id,
    uint ScenarioVersionAtEvaluation,
    int EvaluationSequence,
    string GuardrailResult,
    string AssessmentOutcome,
    string EvidenceStatus,
    string CostCoverageStatus,
    string? LeadingDriverCode,
    string? InsufficientDataReason,
    decimal BaselineUnitPrice,
    decimal ProposedUnitPrice,
    decimal TotalIncludedUnitCost,
    decimal ExpectedUnitContribution,
    decimal? ExpectedContributionMarginRatio,
    decimal? BaselineContributionMarginRatio,
    decimal? ContributionMarginDelta,
    decimal ProfileMinimumContributionMarginRatio,
    Guid ProfileId,
    string ProfileVersion,
    string InputSnapshotJson,
    string OutputSnapshotJson,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    string EvaluatedBy,
    IReadOnlyList<CommercialPricingScenarioDriverReadModel> Drivers);

public sealed record CommercialPricingScenarioSummaryReadModel(
    Guid Id,
    string ScenarioReference,
    string LifecycleStatus,
    string? GuardrailResult,
    Guid ProfileId,
    Guid? MaterialId,
    string? CustomerReference,
    decimal BaselineUnitPrice,
    decimal ProposedUnitPrice,
    decimal? ProposedDiscountRatio,
    DateTimeOffset? ValidityWindowStartUtc,
    DateTimeOffset? ValidityWindowEndUtc,
    Guid? LinkedAiTaskId,
    int EvaluationCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record CommercialPricingScenarioAuditEventReadModel(
    Guid Id,
    string EventType,
    string PayloadJson,
    string ActorSubjectId,
    DateTimeOffset OccurredAtUtc);

public sealed record CommercialPricingScenarioDetailReadModel(
    Guid Id,
    string ScenarioReference,
    string LifecycleStatus,
    string? GuardrailResult,
    Guid ProfileId,
    string ProfileVersion,
    Guid? MaterialId,
    string? CustomerReference,
    Guid SalesEvidenceObservationId,
    decimal? Quantity,
    decimal BaselineUnitPrice,
    decimal ProposedUnitPrice,
    decimal? ProposedDiscountRatio,
    DateTimeOffset? ValidityWindowStartUtc,
    DateTimeOffset? ValidityWindowEndUtc,
    string CommercialRationale,
    Guid? LatestEvaluationSnapshotId,
    Guid? LinkedAiTaskId,
    string RuleSetVersion,
    string SourceProvenance,
    int EvaluationCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    CommercialPricingScenarioEvaluationSnapshotReadModel? LatestEvaluation,
    IReadOnlyList<CommercialPricingScenarioEvaluationSnapshotReadModel> EvaluationHistory,
    IReadOnlyList<CommercialPricingScenarioAuditEventReadModel> AuditEvents);

public sealed record CommercialPricingScenarioWorkspaceReadModel(
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    int DraftCount,
    int NeedsReviewCount,
    int EligibleCount,
    int BlockedCount,
    int ApprovedCount,
    bool CanRead,
    bool CanManage,
    bool CanReview,
    bool CanCreateTask);

public sealed record CreateCommercialPricingScenarioCommand(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid SalesEvidenceObservationId,
    decimal ProposedUnitPrice,
    decimal? ProposedDiscountRatio,
    decimal? Quantity,
    DateTimeOffset? ValidityWindowStartUtc,
    DateTimeOffset? ValidityWindowEndUtc,
    string CommercialRationale,
    string SourceProvenance);

public sealed record UpdateCommercialPricingScenarioCommand(
    Guid ScenarioId,
    uint ExpectedVersion,
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid SalesEvidenceObservationId,
    decimal ProposedUnitPrice,
    decimal? ProposedDiscountRatio,
    decimal? Quantity,
    DateTimeOffset? ValidityWindowStartUtc,
    DateTimeOffset? ValidityWindowEndUtc,
    string CommercialRationale);

public sealed record EvaluateCommercialPricingScenarioCommand(
    Guid ScenarioId,
    uint ExpectedVersion);

public sealed record TransitionCommercialPricingScenarioCommand(
    Guid ScenarioId,
    uint ExpectedVersion,
    string TargetLifecycleStatus,
    string? Note = null);

public sealed record LinkCommercialPricingScenarioTaskCommand(
    Guid ScenarioId,
    uint ExpectedVersion,
    Guid AiTaskId);

public sealed class CommercialPricingScenarioWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid EntityId { get; init; }
    public uint Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public CommercialPricingScenarioDetailReadModel? Scenario { get; init; }

    public static CommercialPricingScenarioWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialPricingScenarioDetailReadModel? scenario = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Scenario = scenario,
        };

    public static CommercialPricingScenarioWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public interface ICommercialPricingScenarioRepository
{
    Task<CommercialPricingScenarioWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        bool canReview,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialPricingScenarioSummaryReadModel>> ListScenariosAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPricingScenarioListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialPricingScenarioDetailReadModel?> GetScenarioByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid scenarioId,
        CancellationToken cancellationToken = default);

    Task<CommercialPricingScenarioWriteResult> CreateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialPricingScenarioWriteResult> UpdateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialPricingScenarioWriteResult> EvaluateScenarioAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialPricingScenarioWriteResult> TransitionScenarioAsync(
        CommercialIntelligenceOperationContext context,
        TransitionCommercialPricingScenarioCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialPricingScenarioWriteResult> LinkTaskAsync(
        CommercialIntelligenceOperationContext context,
        LinkCommercialPricingScenarioTaskCommand command,
        CancellationToken cancellationToken = default);
}
