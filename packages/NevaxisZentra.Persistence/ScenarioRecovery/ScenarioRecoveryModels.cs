using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.ScenarioRecovery;

public sealed record ScenarioRecoveryObjective(
    string ObjectiveType,
    string Label,
    int Priority,
    bool IsHardConstraint);

public sealed record ScenarioRecoveryProblemStatement(
    string Summary,
    string? OrderReference,
    string? LineCode,
    string? ConstraintStationCode,
    decimal? BaselineGoodThroughputPerHour,
    decimal? TargetGoodThroughputPerHour,
    IReadOnlyList<string> ContributingFactors);

public sealed record ScenarioRecoveryActionDefinition(
    string ActionType,
    string TargetEntityType,
    string? TargetEntityCode,
    IReadOnlyDictionary<string, string> Parameters,
    string RiskClass,
    bool RequiresApproval,
    bool HasRollback);

public sealed record ScenarioRecoveryOptionAction(
    ScenarioRecoveryActionDefinition Action,
    string FeasibilityState,
    IReadOnlyList<string> FeasibilityReasons);

public sealed record ScenarioRecoveryComparisonMetric(
    string MetricCode,
    string Label,
    decimal? BaselineValue,
    decimal? OptionValue,
    decimal? Delta,
    string? Unit,
    string? Direction);

public sealed record ScenarioRecoveryTradeOff(
    string OptionLabel,
    IReadOnlyList<string> Advantages,
    IReadOnlyList<string> Disadvantages,
    IReadOnlyList<string> ComputedFacts);

public sealed record ScenarioRecoveryRecommendationRationale(
    string RecommendedOptionLabel,
    IReadOnlyList<string> ScoreComponents,
    IReadOnlyList<string> GuardrailNotes,
    bool IsPolicyDriven);

public sealed record ScenarioRecoveryOptionSnapshot(
    string OptionLabel,
    string Title,
    string Category,
    string FeasibilityState,
    IReadOnlyList<string> FeasibilityReasons,
    IReadOnlyList<ScenarioRecoveryOptionAction> Actions,
    ScenarioMetricSnapshot Simulation,
    IReadOnlyList<ScenarioRecoveryComparisonMetric> Metrics,
    IReadOnlyList<string> Assumptions,
    bool RequiresApproval,
    bool IsDominated,
    decimal RecommendationScore);

public sealed record ScenarioRecoveryComparisonMatrix(
    IReadOnlyList<string> MetricCodes,
    IReadOnlyList<ScenarioRecoveryOptionSnapshot> Options,
    IReadOnlyList<ScenarioRecoveryTradeOff> TradeOffs,
    ScenarioRecoveryRecommendationRationale Recommendation,
    string? PredictedConstraintBefore,
    string? PredictedConstraintAfterRecommended);

public sealed record ScenarioRecoverySnapshotSummary(
    Guid SnapshotId,
    string DataQuality,
    DateTimeOffset CapturedAtUtc,
    string? LineCode,
    string? FacilityScope,
    IReadOnlyList<string> Assumptions);

public sealed record ScenarioRecoveryRecoveryPlan(
    string ObjectiveSummary,
    string SelectedOptionLabel,
    IReadOnlyList<ScenarioRecoveryActionDefinition> Actions,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> ApprovalRequirements,
    string ExecutionMode,
    int ObservationWindowMinutes,
    IReadOnlyList<string> VerificationCriteria);

public sealed record ScenarioRecoveryVerificationSnapshot(
    string Outcome,
    VerifiedImprovementBaseline Baseline,
    decimal PredictedGoodThroughputPerHour,
    VerifiedImprovementActual? Actual,
    decimal? ThroughputPredictionError,
    bool QualityGuardPassed,
    bool SafetyGuardPassed,
    DateTimeOffset? ObservationStartUtc,
    DateTimeOffset? ObservationEndUtc,
    DateTimeOffset? VerifiedAtUtc);

public sealed record ScenarioRecoveryHistoryItem(
    Guid RunId,
    string ProblemSummary,
    string Category,
    string SelectedOptionLabel,
    string Phase,
    string? VerificationOutcome,
    decimal? PredictionError,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ScenarioRecoveryWorkspace(
    Guid RunId,
    Guid? DigitalWorkCaseId,
    Guid LineId,
    string LineCode,
    string CurrentPhase,
    string ExecutionState,
    string Category,
    string EntryPoint,
    ScenarioRecoveryProblemStatement Problem,
    IReadOnlyList<ScenarioRecoveryObjective> Objectives,
    ScenarioRecoverySnapshotSummary? Snapshot,
    ScenarioRecoveryComparisonMatrix? Comparison,
    string? SelectedOptionLabel,
    ScenarioRecoveryRecoveryPlan? RecoveryPlan,
    ScenarioRecoveryVerificationSnapshot? Verification,
    bool HasPendingApproval,
    bool IsVerifiedClosed,
    string EngineVersion,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateScenarioRecoveryRunRequest(
    Guid LineId,
    string Category,
    string EntryPoint,
    ScenarioRecoveryProblemStatement Problem,
    IReadOnlyList<ScenarioRecoveryObjective> Objectives,
    string? SourceEntityType,
    string? SourceEntityId,
    Guid? LinkedDigitalWorkCaseId);

public sealed record SelectScenarioRecoveryOptionRequest(
    string OptionLabel,
    long? ExpectedConcurrencyToken);

public sealed record ScenarioRecoveryGoldenDemoResult(
    Guid RunId,
    Guid CaseId,
    Guid LineId,
    string FinalPhase,
    string VerificationOutcome,
    bool KnowledgePromoted,
    decimal BaselineGoodThroughputPerHour,
    decimal PredictedGoodThroughputPerHour,
    decimal? ActualGoodThroughputPerHour,
    decimal? PredictionError,
    string Message);

public sealed record SimulateScenarioRecoveryActionRequest(
    string ActionType,
    string? TargetEntityCode,
    IReadOnlyDictionary<string, string>? Parameters);
