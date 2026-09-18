using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Persistence.AdaptiveProductionPlanning;

public sealed record AdaptivePlanningWorkspaceSnapshot(
    ProductionPlan? ActivePlan,
    ProductionPlanVersion? ActiveVersion,
    IReadOnlyList<ProductionPlanOperation> Operations,
    IReadOnlyList<PlanningScenario> Scenarios,
    IReadOnlyList<PlanDeviation> Deviations,
    IReadOnlyList<PlanChange> RecentChanges,
    IReadOnlyList<PlanActivityEvent> ActivityFeed,
    AdaptivePlanningPolicy Policy,
    AdaptivePlanningMetrics Metrics,
    AdaptivePlanningRiskSummary Risks,
    DateTimeOffset DataFreshnessUtc);

public sealed record AdaptivePlanningMetrics(
    decimal PlanAdherencePercent,
    decimal ScheduleAdherencePercent,
    int OrdersAtRisk,
    int LateOrders,
    int PlanVersionsToday,
    int ReplansToday,
    int OperationsMovedToday,
    int FrozenWindowOverridesToday,
    decimal PlannedUtilizationPercent,
    decimal ActualUtilizationPercent);

public sealed record AdaptivePlanningRiskSummary(
    IReadOnlyList<DeadlineRiskRow> DeadlineRisks,
    IReadOnlyList<string> CapacityOverloads,
    IReadOnlyList<string> MachineRisks,
    IReadOnlyList<string> MaterialRisks,
    IReadOnlyList<string> MoldRisks,
    IReadOnlyList<string> QualityRisks,
    IReadOnlyList<string> MaintenanceRisks);

public sealed record DeadlineRiskRow(
    string OrderId,
    string RiskLevel,
    DateTimeOffset? DeadlineUtc,
    DateTimeOffset? ExpectedCompletionUtc,
    decimal ConfidenceScore);

public sealed record BuildPlanRequest(
    Guid ProductionPlanId,
    string PlanningHorizon,
    string ObjectiveProfileJson,
    long? ExpectedConcurrencyToken);

public sealed record SimulatePlanRequest(
    Guid ProductionPlanId,
    string TriggerType,
    string TriggerContextJson,
    int MaxAlternatives);

public sealed record PublishPlanRequest(
    Guid PlanVersionId,
    long ConcurrencyToken,
    bool OverrideFrozenWindow,
    string? ApprovalNotes);

public sealed record ApproveReplanRequest(
    Guid PlanVersionId,
    long ConcurrencyToken,
    bool Approved,
    string? Notes);

public sealed record EvaluateReplanRequest(
    Guid ProductionPlanId,
    string TriggerType,
    string TriggerContextJson);

public sealed record PlanBuildResult(
    ProductionPlanVersion Version,
    IReadOnlyList<ProductionPlanOperation> Operations,
    PlanningConstraintSnapshot Snapshot,
    FeasibilityResult Feasibility,
    decimal Score,
    string ComparisonLabel);

public sealed record FeasibilityResult(
    bool IsFeasible,
    IReadOnlyList<string> HardViolations,
    IReadOnlyList<string> SoftWarnings,
    string? Explanation);

public sealed record ReplanEvaluationResult(
    string RecommendedAction,
    string Rationale,
    bool NervousnessBlocked,
    bool FrozenWindowBlocked,
    IReadOnlyList<PlanningScenario> Alternatives);

public sealed record PlanSimulationResult(
    PlanningScenario Scenario,
    IReadOnlyList<PlanningScenarioMetric> Metrics,
    string ComparisonJson);

public sealed record PlanPublishResult(
    ProductionPlanVersion Version,
    ProductionPlan Plan,
    PlanChange Change,
    bool AutoPublished,
    bool ApprovalRequired);

public sealed record PlanOutcomeVerificationResult(
    Guid PlanVersionId,
    string OutcomeStatus,
    string ExpectedImpactJson,
    string ActualImpactJson,
    decimal AdherencePercent,
    Guid? LearningMemoryId);

public sealed record PromiseDateInsight(
    string OrderId,
    DateTimeOffset? EarliestFeasibleUtc,
    DateTimeOffset? ExpectedCompletionUtc,
    decimal ConfidenceScore,
    string DataClassification);

public sealed record PlanDiffRow(
    string OrderId,
    string Field,
    string OldValue,
    string NewValue,
    string ImpactSummary);

public sealed record UpdateAdaptivePlanningPolicyRequest(
    long ConcurrencyToken,
    int? FrozenWindowMinutes,
    int? NervousnessCooldownMinutes,
    decimal? MinBenefitThreshold,
    string? AutonomyMode,
    string? PolicyEnvelopeJson);

public sealed record ConstraintOrderRow(
    string OrderId,
    string Title,
    DateTimeOffset DeadlineUtc,
    int PriorityScore,
    string MaterialState,
    decimal Quantity,
    IReadOnlyList<string> RequiredMachineIds,
    IReadOnlyList<string> RequiredMoldIds,
    bool QualityHold);

public sealed record ConstraintMachineRow(
    string MachineId,
    string LineId,
    bool Available,
    bool MaintenanceBlocked,
    IReadOnlyList<string> CompatibleMoldIds,
    decimal CycleMinutes,
    decimal UtilizationPercent);

public sealed record ConstraintSnapshotPayload(
    IReadOnlyList<ConstraintOrderRow> Orders,
    IReadOnlyList<ConstraintMachineRow> Machines,
    IReadOnlyList<string> HardConstraints,
    IReadOnlyList<string> SoftConstraints,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset FreshnessUtc);
