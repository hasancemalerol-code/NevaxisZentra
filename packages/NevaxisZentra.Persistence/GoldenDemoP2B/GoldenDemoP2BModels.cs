namespace NevaxisZentra.Persistence.GoldenDemoP2B;

public sealed record DigitalFactoryTeamSnapshot(
    Guid ProductionEventId,
    string MachineCode,
    IReadOnlyList<AgentEvidenceDetail> Agents,
    OrchestratorAssessment Orchestrator,
    IReadOnlyList<GoldenDemoActivityItem> ActivityTimeline,
    ApprovalSheetModel ApprovalSheet,
    IReadOnlyList<PlanActionTraceability> PlanActions,
    IReadOnlyList<InterventionTimelineItem> InterventionTimeline);

public sealed record AgentEvidenceDetail(
    Guid FindingId,
    string AgentCode,
    string WorkerCode,
    string AgentLabel,
    string Summary,
    string EvidenceClassification,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<string> DataInputsReviewed,
    IReadOnlyDictionary<string, string> CalculationInputs,
    IReadOnlyDictionary<string, string> CalculationResults,
    string Conclusion,
    string Confidence,
    string? InsufficientEvidenceState,
    Guid ProductionEventId,
    Guid? MachineAssetId,
    string MachineCode,
    string ResultingAction,
    string? PlanReferenceId,
    string? WorkReferenceId);

public sealed record OrchestratorAssessment(
    string WhatIsHappening,
    string WhySummary,
    int CurrentExpectedShiftGoodParts,
    int RecoveredExpectedShiftGoodParts,
    int ProductionRecoveryGoodParts,
    string WhatShouldHappenNext,
    bool HumanApprovalRequired,
    string ApprovingRole,
    string RequiredControlReason,
    string ExpectedOperationalOutcome,
    IReadOnlyList<string> InputsUsed,
    IReadOnlyList<string> InsufficientSpecialistEvidence);

public sealed record GoldenDemoActivityItem(
    DateTimeOffset TimestampUtc,
    string Actor,
    string Action,
    string Status,
    string SourceReference,
    string EvidenceType,
    string? ResultSummary);

public sealed record ApprovalSheetModel(
    bool IsApprovalRequired,
    bool IsApproved,
    string RecommendedIntervention,
    string WhyRecommended,
    int ExpectedInterruptionMinutes,
    int CurrentProductionExposureGoodParts,
    int CurrentExpectedShiftGoodParts,
    int RecoveredExpectedShiftGoodParts,
    int RecoveryOpportunityGoodParts,
    string OperationalRisk,
    string ApprovingRole,
    IReadOnlyList<string> EvidenceReferences,
    DateTimeOffset RequestedAtUtc,
    string RequestedByActor,
    string? ApprovedBySubjectId,
    DateTimeOffset? ApprovedAtUtc,
    string ApprovalStatus,
    string AuditReference);

public sealed record PlanActionTraceability(
    Guid ActionId,
    Guid PlanId,
    int SortOrder,
    string Title,
    string ResponsibleRole,
    string Status,
    string EvidenceRequired,
    string ValidationCriteria,
    string SourceAgentCode,
    string SourceAgentLabel,
    string RelatedTechnicalFinding,
    DateTimeOffset? CompletedAtUtc,
    string WorkReferenceId);

public sealed record InterventionTimelineItem(
    string StageCode,
    string StageLabel,
    string Status,
    DateTimeOffset? OccurredAtUtc,
    string Actor);
