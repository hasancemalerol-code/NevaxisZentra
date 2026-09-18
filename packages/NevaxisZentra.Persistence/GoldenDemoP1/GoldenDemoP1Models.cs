namespace NevaxisZentra.Persistence.GoldenDemoP1;

public sealed record GoldenDemoWorkspaceSnapshot(
    GoldenDemoProductionEvent Event,
    GoldenDemoCycleBaseline? Baseline,
    GoldenDemoCycleObservation CurrentObservation,
    GoldenDemoCycleObservation? PostInterventionObservation,
    CycleLossDecompositionResult CycleLoss,
    IReadOnlyList<RootContributorResult> Contributors,
    ProductionRecoveryResult Recovery,
    IReadOnlyList<WhatIfScenarioResult> WhatIfScenarios,
    GoldenDemoRecoveryPlan? Plan,
    IReadOnlyList<GoldenDemoRecoveryPlanAction> PlanActions,
    IReadOnlyList<GoldenDemoAgentFinding> AgentFindings,
    GoldenDemoVerificationRun? Verification,
    BeforeAfterComparisonResult? BeforeAfter,
    MachineIntelligenceMetrics Metrics,
    BusinessImpactSummary BusinessImpact);

public sealed record MachineIntelligenceMetrics(
    int ShiftTargetGoodParts,
    int ActualGoodParts,
    int ProjectedEndOfShiftGoodParts,
    decimal TargetAttainmentPercent,
    int RejectQuantity,
    decimal RejectRatePercent,
    decimal ThroughputGoodPartsPerHour,
    string MachineState,
    decimal CurrentCycleSeconds,
    decimal? ReferenceCycleSeconds,
    decimal AvailabilityPercent,
    int MicroStopCount,
    decimal MicroStopDurationSeconds,
    string? MoldReference,
    decimal? KwhPerGoodPart,
    decimal? ReferenceKwhPerGoodPart,
    decimal? EnergyIntensityDeviationPercent,
    IReadOnlyDictionary<string, decimal?> ProcessParameters,
    IReadOnlyDictionary<string, string> UnavailableMetrics);

public sealed record BusinessImpactSummary(
    int OperationalGoodPartsPerShift,
    decimal CapacityImpactPercent,
    decimal? EnergyIntensityImpactPercent,
    decimal? DowntimeMinutesAvoided,
    decimal? VerifiedBusinessImpactTry,
    decimal? EstimatedBusinessImpactTry);

public sealed record ApproveRecoveryPlanRequest(string? Comment);
public sealed record ExecuteRecoveryPlanRequest(string IdempotencyKey);
public sealed record CompleteInterventionRequest(string IdempotencyKey);
public sealed record VerifyOutcomeRequest(string IdempotencyKey);
