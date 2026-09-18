namespace NevaxisZentra.Persistence.FactoryRecoveryIntelligence;

public sealed record FactoryRecoveryPattern(
    string OutcomeType,
    string EventCategory,
    string? MachineCode,
    string? OrderReference,
    string? MaterialCode,
    string? MoldCode,
    string Severity,
    string PatternKey,
    string StrategyFamily,
    IReadOnlyList<string> WorkerCodes,
    IReadOnlyList<string> BlockerProfile);

public sealed record FactoryRecoverySimilarCaseMatch(
    Guid LearningRecordId,
    Guid SourceCaseId,
    int SimilarityPercent,
    IReadOnlyList<string> SimilarityReasons,
    string OrderReference,
    string MachineCode,
    string ProblemSummary,
    string AppliedStrategy,
    string OutcomeSummary,
    int? RecoveryMinutes,
    bool? DeliveryProtected,
    DateTimeOffset VerifiedAtUtc);

public sealed record FactoryRecoveryStrategyPerformance(
    string StrategyKey,
    int VerifiedUses,
    int VerifiedSuccesses,
    int? MedianRecoveryMinutes,
    int? DeliveryProtectedCount,
    int? DeliveryRelevantCases,
    string SampleConfidence,
    IReadOnlyList<string> CommonBlockers,
    int FailureModeCount);

public sealed record FactoryRecoveryPlanHistoricalEvidence(
    string PlanKey,
    int SimilarVerifiedCaseCount,
    int VerifiedSuccesses,
    int? MedianRecoveryMinutes,
    string SampleConfidence,
    IReadOnlyList<string> KnownRisks,
    IReadOnlyList<string> KnownFailureModes);

public sealed record FactoryRecoveryIntelligenceSnapshot(
    int SimilarVerifiedCaseCount,
    IReadOnlyList<FactoryRecoverySimilarCaseMatch> SimilarCases,
    IReadOnlyList<FactoryRecoveryStrategyPerformance> StrategyPerformance,
    IReadOnlyList<string> KnownFailureModes,
    int? ExpectedRecoveryMinutesMedian,
    string EvidenceStrength,
    IReadOnlyList<FactoryRecoveryPlanHistoricalEvidence> PlanEvidence);

public sealed record FactoryRecoveryHistoricalCaseDetail(
    Guid LearningRecordId,
    Guid SourceCaseId,
    string OutcomeType,
    string EventCategory,
    string OrderReference,
    string MachineCode,
    string MaterialCode,
    string MoldCode,
    string Severity,
    string ProblemSummary,
    string CauseSummary,
    string AppliedStrategy,
    string? AlternativeStrategy,
    IReadOnlyList<string> ParticipatingWorkers,
    IReadOnlyList<string> ActionsExecuted,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> FailureModes,
    bool DeliveryProtected,
    int? RecoveredQuantity,
    int? RecoveryMinutes,
    int? TimeToFirstActionMinutes,
    string EvidenceStrength,
    string LearningDisposition,
    DateTimeOffset VerifiedAtUtc);

public sealed record FactoryRecoveryWorkerEffectiveness(
    string WorkerCode,
    string WorkerName,
    int VerifiedRecoveries,
    int? MedianRecoveryMinutes,
    IReadOnlyList<string> CommonWorkTypes,
    string SampleConfidence);

public sealed record FactoryRecoveryLearningSearchFilters(
    string? OutcomeType,
    string? MachineCode,
    string? OrderReference,
    string? MaterialCode,
    string? EventCategory,
    string? StrategyKey,
    string? WorkerCode,
    DateTimeOffset? VerifiedAfterUtc,
    int Limit = 20);
