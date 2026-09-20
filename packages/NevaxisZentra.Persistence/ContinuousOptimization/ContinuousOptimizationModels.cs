namespace NevaxisZentra.Persistence.ContinuousOptimization;

public sealed record ContinuousOptimizationTriggerRequest(
    string TriggerType,
    string OptimizationType,
    string EntityType,
    string EntityId,
    Guid? LineId,
    decimal? DeltaValue,
    decimal? CurrentValue,
    decimal? BaselineValue,
    string? Detail,
    DateTimeOffset ObservedAtUtc);

public sealed record ContinuousOptimizationPolicyThresholds(
    decimal GapThresholdPercent = 5m,
    decimal MinimumGapPerHour = 20m,
    decimal MinimumValueThreshold = 15m,
    int CooldownMinutes = 30,
    int MaxRunsPerLinePerDay = 6,
    decimal HysteresisPercent = 2m);

public sealed record ContinuousOptimizationQualificationResult(
    bool Qualified,
    string ReasonCode,
    string? Detail,
    ContinuousOptimizationPolicyThresholds AppliedPolicy);

public sealed record OptimizationOpportunity(
    string OpportunityType,
    string ScopeEntityType,
    string ScopeEntityId,
    string TriggerType,
    string Title,
    string Summary,
    decimal BaselineValue,
    decimal CurrentValue,
    decimal EstimatedBenefit,
    decimal Confidence,
    string RiskClass,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RequiredActions,
    IReadOnlyList<string> Constraints,
    DateTimeOffset ExpiresAtUtc);

public sealed record ContinuousOptimizationTriggerResult(
    string ResultState,
    string? FailureState,
    ContinuousOptimizationQualificationResult Qualification,
    OptimizationOpportunity? Opportunity,
    ContinuousOptimizationRunSummary? Run,
    bool ModelInvoked,
    long LatencyMs);

public sealed record ContinuousOptimizationRunSummary(
    Guid RunId,
    string CurrentPhase,
    string OptimizationType,
    string TriggerType,
    string? Outcome,
    Guid? DigitalWorkCaseId,
    Guid? ThroughputOptimizerRunId,
    Guid? LineId,
    string? LineCode,
    DateTimeOffset UpdatedAtUtc);

public sealed record ContinuousOptimizationExplainability(
    string Trigger,
    string Observation,
    string Baseline,
    string ExpectedBenefit,
    IReadOnlyList<string> Constraints,
    string? ProposedAction,
    string RiskClass,
    string? ApprovalState,
    IReadOnlyList<string> Evidence);

public sealed record ContinuousOptimizationControlCenter(
    IReadOnlyList<ContinuousOptimizationRunSummary> ActiveRuns,
    IReadOnlyList<OptimizationOpportunity> ActiveOpportunities,
    IReadOnlyList<ContinuousOptimizationRunSummary> AwaitingApproval,
    IReadOnlyList<ContinuousOptimizationRunSummary> Observing,
    IReadOnlyList<ContinuousOptimizationVerifiedGainItem> VerifiedGains,
    IReadOnlyList<ContinuousOptimizationRunSummary> RejectedOrFailed,
    ContinuousOptimizationKpiSnapshot Kpis,
    DateTimeOffset GeneratedAtUtc);

public sealed record ContinuousOptimizationVerifiedGainItem(
    Guid RunId,
    Guid? CaseId,
    string LineCode,
    string OptimizationType,
    string Outcome,
    decimal? VerifiedImprovementPerHour,
    string Summary,
    DateTimeOffset VerifiedAtUtc);

public sealed record ContinuousOptimizationKpiSnapshot(
    int OpportunitiesDetected,
    int OpportunitiesQualified,
    int OpportunitiesSuppressed,
    int RunsActive,
    int VerifiedGains,
    int NoGainOutcomes,
    decimal OpportunityToActionRate,
    decimal VerifiedGainRate);

public sealed record ContinuousOptimizationHistoryItem(
    Guid RunId,
    string OptimizationType,
    string TriggerType,
    string Phase,
    string? Outcome,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ContinuousOptimizationHistorySnapshot(
    string EntityType,
    string EntityId,
    IReadOnlyList<ContinuousOptimizationHistoryItem> Items);

public sealed record ContinuousOptimizationGoldenContinuousResult(
    Guid ControllerRunId,
    Guid CaseId,
    Guid LineId,
    string FinalPhase,
    string Outcome,
    bool KnowledgePromoted,
    string Message);

public sealed record ContinuousOptimizationGoldenNoOpResult(
    string ResultState,
    string QualificationReason,
    bool RunCreated,
    bool CaseCreated,
    bool ModelInvoked,
    string Message);

public sealed record ContinuousOptimizationPolicySnapshot(
    Guid PolicyId,
    string OptimizationType,
    string ScopeType,
    string ScopeId,
    bool IsEnabled,
    bool IsPaused,
    ContinuousOptimizationPolicyThresholds Thresholds);

public sealed record UpsertContinuousOptimizationPolicyRequest(
    string OptimizationType,
    string ScopeType,
    string ScopeId,
    bool IsEnabled,
    bool IsPaused,
    ContinuousOptimizationPolicyThresholds Thresholds);
