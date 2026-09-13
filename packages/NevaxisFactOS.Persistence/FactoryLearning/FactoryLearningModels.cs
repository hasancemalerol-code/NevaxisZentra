namespace NevaxisFactOS.Persistence.FactoryLearning;

public sealed record FactoryLearningWorkspaceSnapshot(
    IReadOnlyList<FactoryLoss> Losses,
    IReadOnlyList<FactoryLossPattern> Patterns,
    IReadOnlyList<ImprovementOpportunity> Opportunities,
    IReadOnlyList<FactoryExperiment> ActiveExperiments,
    IReadOnlyList<VerifiedImprovement> VerifiedImprovements,
    IReadOnlyList<FactoryLearningMemory> LearningMemories,
    IReadOnlyList<LossParetoRow> ParetoRows,
    IReadOnlyList<FactoryLearningInsight> Insights,
    FactoryLossCommandSummary CommandSummary,
    DateTimeOffset DataFreshnessUtc);

public sealed record LossParetoRow(
    string Category,
    string Label,
    decimal TotalMinutes,
    decimal ContributionPercent,
    int OccurrenceCount,
    string DataClassification);

public sealed record FactoryLearningInsight(
    string InsightType,
    string Title,
    string Summary,
    decimal ConfidenceScore,
    string EvidenceSummary,
    Guid? LinkedMemoryId);

public sealed record FactoryLossCommandSummary(
    decimal TodayLossMinutes,
    decimal SevenDayLossMinutes,
    decimal ThirtyDayLossMinutes,
    int RecurringLossCount,
    int ActiveExperimentCount,
    int VerifiedImprovementCount,
    int RegressingImprovementCount);

public sealed record CreateExperimentRequest(
    Guid OpportunityId,
    string ExperimentType,
    string Title,
    string Hypothesis,
    string ScopeJson,
    string BaselineJson,
    string TargetJson,
    string GuardrailsJson,
    string StopConditionsJson);

public sealed record ApproveExperimentRequest(long ConcurrencyToken, bool Approved, string? Notes);

public sealed record StartExperimentRequest(long ConcurrencyToken);

public sealed record StopExperimentRequest(long ConcurrencyToken, string Reason);

public sealed record CompleteExperimentRequest(
    long ConcurrencyToken,
    IReadOnlyList<ExperimentMeasurementInput> Measurements);

public sealed record ExperimentMeasurementInput(
    string Phase,
    string MetricKey,
    decimal MetricValue,
    string Unit,
    string DataClassification,
    string EvidenceJson);

public sealed record VerifyExperimentRequest(long ConcurrencyToken, string Outcome);

public sealed record StandardizeImprovementRequest(long ConcurrencyToken, string ScopeJson, string OwnerSubjectId);

public sealed record SimulateImprovementRequest(Guid OpportunityId, string ScenarioType, string InputJson);

public sealed record SimulationImprovementResult(
    string ScenarioType,
    string OutputJson,
    string ComparisonJson,
    string DataClassification);

public sealed record RootCauseDraftRequest(
    Guid? PatternId,
    Guid? LossId,
    string Method,
    IReadOnlyList<string> WhyChain,
    string CandidateCause,
    string ConfidenceLevel,
    string EvidenceJson);
