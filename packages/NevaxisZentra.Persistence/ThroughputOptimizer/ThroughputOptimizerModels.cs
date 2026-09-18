namespace NevaxisZentra.Persistence.ThroughputOptimizer;

public sealed record ThroughputGapObservation(
    Guid LineId,
    string LineCode,
    decimal CurrentGoodPerHour,
    decimal TargetGoodPerHour,
    decimal GapPerHour,
    string? CurrentConstraintStationCode,
    string? CurrentConstraintType,
    string DataQuality,
    bool IsMeaningfulGap);

public sealed record ThroughputDiagnosisResult(
    string CurrentConstraintStationCode,
    string ConstraintType,
    string RootCauseSummary,
    IReadOnlyList<string> ContributingFactors,
    IReadOnlyList<string> EvidenceRefs,
    string IntelligencePath,
    decimal Confidence);

public sealed record ThroughputInterventionCandidate(
    string ScenarioCode,
    string InterventionType,
    string Title,
    decimal PredictedGoodThroughputPerHour,
    decimal DeltaGoodThroughputPerHour,
    decimal QualityImpactScore,
    decimal OperationalRiskScore,
    bool RequiresApproval,
    string Eligibility);

public sealed record ThroughputInterventionSelection(
    ThroughputInterventionCandidate Selected,
    IReadOnlyList<ThroughputInterventionCandidate> Alternatives,
    string SelectionReason);

public sealed record ThroughputOptimizerWorkspace(
    Guid CaseId,
    Guid LineId,
    string LineCode,
    string CurrentPhase,
    string CaseStatus,
    ThroughputGapObservation? Observation,
    ThroughputDiagnosisResult? Diagnosis,
    ThroughputInterventionSelection? Selection,
    VerifiedImprovementSnapshot? Improvement,
    Guid? ContextSnapshotId,
    string? ModelIntelligencePath,
    int RelatedCaseCount,
    bool HasPendingApproval,
    bool IsVerifiedClosed,
    DateTimeOffset UpdatedAtUtc);

public sealed record VerifiedImprovementBaseline(
    decimal GoodThroughputPerHour,
    int GoodCount,
    int RejectCount,
    int ReworkCount,
    decimal Wip,
    string ConstraintStationCode,
    string ConstraintType);

public sealed record VerifiedImprovementActual(
    decimal GoodThroughputPerHour,
    int GoodCount,
    int RejectCount,
    int ReworkCount,
    decimal Wip,
    string ConstraintStationCode,
    string ConstraintType);

public sealed record VerifiedImprovementSnapshot(
    Guid Id,
    Guid CaseId,
    string VerificationOutcome,
    VerifiedImprovementBaseline Baseline,
    decimal TargetImprovementPerHour,
    string InterventionSummary,
    VerifiedImprovementActual? Actual,
    bool QualityGuardPassed,
    bool SafetyGuardPassed,
    DateTimeOffset? ObservationStartUtc,
    DateTimeOffset? ObservationEndUtc,
    DateTimeOffset? VerifiedAtUtc);

public sealed record ThroughputOptimizerGoldenDemoResult(
    Guid CaseId,
    Guid LineId,
    string FinalPhase,
    string VerificationOutcome,
    bool KnowledgeCandidateCreated,
    string Message);

public sealed record ThroughputOptimizerExecutiveCaseItem(
    Guid CaseId,
    Guid LineId,
    string LineCode,
    string CurrentPhase,
    string CaseStatus,
    string? CurrentConstraintStationCode,
    string? CurrentConstraintType,
    string? SelectedInterventionTitle,
    string? SelectedInterventionType,
    bool HasPendingApproval,
    string ObservationState,
    string? VerificationOutcome,
    decimal? VerifiedThroughputGainPerHour,
    bool IsVerifiedClosed,
    DateTimeOffset UpdatedAtUtc);

public sealed record ThroughputOptimizerExecutiveSummary(
    int ActiveCaseCount,
    int PendingApprovalCount,
    int VerifiedImprovementTodayCount,
    IReadOnlyList<ThroughputOptimizerExecutiveCaseItem> ActiveCases,
    IReadOnlyList<ThroughputOptimizerExecutiveCaseItem> VerifiedToday);
