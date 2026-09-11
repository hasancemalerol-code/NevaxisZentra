namespace CrevaFactOS.Persistence.ProductionManager;

public sealed record ProductionManagerWorkspaceSnapshot(
    FactoryStateSnapshot State,
    IReadOnlyList<FactoryPrediction> Predictions,
    IReadOnlyList<FactoryDecision> PendingDecisions,
    IReadOnlyList<FactoryDecision> RecentDecisions,
    IReadOnlyList<BottleneckSignal> Bottlenecks,
    ProductionManagerPolicy Policy,
    ProductionManagerExecutiveSummary Executive);

public sealed record ProductionManagerExecutiveSummary(
    string FactoryStatusLabel,
    int ActiveRiskCount,
    int PredictedIssueCount,
    int PendingDecisionCount,
    int AutonomousActionCount,
    int VerifiedTodayCount);

public sealed record BottleneckSignal(
    string Domain,
    string Summary,
    string Severity,
    IReadOnlyList<string> AffectedOrders);

public sealed record DecisionOpportunity(
    string DecisionType,
    string Title,
    string ProblemSummary,
    string CorrelationKey,
    decimal ConfidenceScore,
    string ConfidenceBand);

public sealed record DecisionOptionDraft(
    string OptionCode,
    string Title,
    string Description,
    string ExpectedOutcomeJson,
    IReadOnlyList<string> Risks,
    bool RequiresApproval,
    bool IsRejected,
    string? RejectionReason);

public sealed record SimulationRequest(
    string ScenarioType,
    string? MachineId,
    string? OrderId,
    string? MoldId,
    string? MaterialId,
    decimal? DelayHours,
    decimal? CapacityAdjustmentPercent);

public sealed record SimulationResult(
    string ScenarioType,
    string OutputJson,
    string ComparisonJson,
    IReadOnlyList<ScenarioComparisonRow> ComparisonRows);

public sealed record ScenarioComparisonRow(
    string OptionCode,
    string Label,
    decimal OnTimeProbability,
    decimal ProductionCompletionPercent,
    decimal CapacityUtilizationPercent,
    decimal DowntimeHours,
    string MaterialRisk,
    string QualityRisk,
    bool RequiresApproval,
    decimal EstimatedRecoveryHours);

public sealed record ExecuteDecisionRequest(
    Guid? SelectedOptionId,
    string? IdempotencyKey,
    long ConcurrencyToken);

public sealed record VerifyDecisionOutcomeRequest(
    string ActualOutcomeJson,
    long ConcurrencyToken);

public sealed record ApproveDecisionRequest(
    bool Approved,
    string? Notes,
    long ConcurrencyToken);
