namespace NevaxisZentra.Persistence.Export;

public static class ExportAiPromptVersions
{
    public const string CaseOverviewV1 = "export.case.overview.v1";
    public const string ComplianceExplainV1 = "export.compliance.explain.v1";
    public const string ReadinessAnalysisV1 = "export.readiness.analysis.v1";
    public const string MissingDataV1 = "export.missing-data.analysis.v1";
    public const string CountryResearchV1 = "export.country.research.v1";
    public const string NextActionsV1 = "export.next-actions.v1";
    public const string DocumentGapV1 = "export.document-gap.v1";
    public const string CrossModuleRiskV1 = "export.cross-module.risk.v1";
}

public static class ExportAiProviders
{
    public const string DeterministicDevelopment = "deterministic-development";
    public const string DeterministicDevelopmentModel = "fixture-v1";
}

public sealed record ExportAiSourceReference(
    string SourceType,
    string SourceId,
    string Label,
    string? Detail);

public sealed record ExportAiContextCaseRef(
    Guid ExportCaseId,
    string CaseNumber,
    string DestinationCountryCode,
    string? OriginCountryCode,
    string ReadinessStatus,
    int ReadinessPercent,
    string ComplianceStatus,
    string? HsCode,
    string HsCodeStatus,
    string OriginConfirmationStatus);

public sealed record ExportAiContextDocumentRef(
    Guid Id,
    string DocumentType,
    string Status,
    string? FileName);

public sealed record ExportAiContextComplianceRef(
    Guid? EvaluationId,
    int? EvaluationNumber,
    string Result,
    int? RulePackVersion,
    string? RulePackName,
    IReadOnlyList<ExportAiContextRequirementRef> Requirements);

public sealed record ExportAiContextRequirementRef(
    Guid Id,
    string RequirementCode,
    string Title,
    string Status,
    bool Blocking,
    string? RequiredDocumentType,
    string SourceReference);

public sealed record ExportAiContextResearchRef(
    Guid? ResearchCaseId,
    string? ResearchNumber,
    string Status,
    int FindingCount,
    int UnverifiedFindingCount);

public sealed record ExportAiContextSnapshotModel(
    Guid? ExportCaseId,
    Guid? CountryResearchCaseId,
    ExportAiContextCaseRef? ExportCase,
    string? SourceOrderReference,
    string? CustomerName,
    IReadOnlyList<ExportAiContextDocumentRef> Documents,
    string PackingStatus,
    IReadOnlyList<ExportAiContextShipmentRef> Shipments,
    ExportAiContextBrokerRef? Broker,
    ExportAiContextComplianceRef? Compliance,
    ExportAiContextResearchRef? Research,
    IReadOnlyList<ExportAiContextKnowledgeRef> KnowledgeEntries,
    ExportProductionIntelligenceRef? Production,
    ExportPlanningIntelligenceRef? Planning,
    ExportQualityIntelligenceRef? Quality,
    ExportErpIntelligenceRef? Erp,
    IReadOnlyList<ExportCrossModuleRiskReadModel> CrossModuleRisks,
    DateTimeOffset CapturedAtUtc);

public sealed record ExportAiContextShipmentRef(
    Guid Id,
    string ShipmentNumber,
    string Status,
    DateTimeOffset? PlannedDepartureUtc,
    DateTimeOffset? PackingDeadlineUtc);

public sealed record ExportAiContextBrokerRef(
    Guid WorkspaceId,
    string Status,
    int OpenCorrectionCount);

public sealed record ExportAiContextKnowledgeRef(
    Guid Id,
    string Title,
    string Authority,
    string VerificationStatus);

/// <summary>Read-only production intelligence adapter contract.</summary>
public sealed record ExportProductionIntelligenceRef(
    bool Available,
    Guid? ProductionOrderId,
    string? OrderNumber,
    string? Status,
    decimal? QuantityCompleted,
    decimal? QuantityPlanned,
    DateTimeOffset? ExpectedCompletionUtc,
    bool HasActiveDowntime,
    bool HasExecutionException,
    string? UnavailableReason);

/// <summary>Read-only planning intelligence + logistics constraint contract.</summary>
public sealed record ExportPlanningIntelligenceRef(
    bool Available,
    Guid? ProductionPlanId,
    string? PlanNumber,
    string? Status,
    DateTimeOffset? PlannedCompletionUtc,
    bool ScheduleRiskDetected,
    ExportPlanningLogisticsConstraintRef? LogisticsConstraints,
    string? UnavailableReason);

/// <summary>Future Planning Logistics Constraints integration contract (read-only).</summary>
public sealed record ExportPlanningLogisticsConstraintRef(
    DateTimeOffset? PlannedPickupUtc,
    DateTimeOffset? PlannedDepartureUtc,
    DateTimeOffset? PackingDeadlineUtc,
    DateTimeOffset? QualityDeadlineUtc,
    DateTimeOffset? PlannedProductionCompletionUtc,
    bool MaterialReadinessRisk,
    bool ScheduleRisk);

public sealed record ExportQualityIntelligenceRef(
    bool Available,
    string? ReleaseStatus,
    bool HasUnresolvedHold,
    string? UnavailableReason);

public sealed record ExportErpIntelligenceRef(
    bool Available,
    string? CustomerReference,
    string? ItemReference,
    string? UnavailableReason);

public sealed record ExportCrossModuleRiskReadModel(
    ExportCrossModuleRiskType Type,
    ExportCrossModuleRiskSeverity Severity,
    ExportCrossModuleSourceModule SourceModule,
    string SourceRecordReference,
    string Description,
    DateTimeOffset DetectedAtUtc,
    bool Blocking);

public sealed record ExportAiFindingDraft(
    ExportAiFindingCategory Category,
    string Title,
    string Summary,
    ExportAiFindingSeverity Severity,
    decimal Confidence,
    ExportAiSourceType SourceType,
    IReadOnlyList<ExportAiSourceReference> SourceReferences,
    Guid? RelatedRequirementId,
    Guid? RelatedDocumentId,
    Guid? RelatedShipmentId,
    bool Actionable,
    bool HumanReviewRequired);

public sealed record ExportAiRecommendationDraft(
    string Title,
    string Description,
    ExportAiRecommendationPriority Priority,
    decimal Confidence,
    string ReasoningSummary,
    ExportAiProposedActionType? ProposedActionType,
    string? ProposedActionPayloadJson);

public sealed record ExportAiProviderOutput(
    string ReasoningSummary,
    IReadOnlyList<ExportAiFindingDraft> Findings,
    IReadOnlyList<ExportAiRecommendationDraft> Recommendations,
    int InputUsageUnits,
    int OutputUsageUnits,
    decimal AiCreditsConsumed);

public sealed record ExportAiAnalysisRunReadModel(
    Guid Id,
    Guid? ExportCaseId,
    Guid? CountryResearchCaseId,
    Guid ContextSnapshotId,
    string RunType,
    string Status,
    string Provider,
    string Model,
    string PromptVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RequestedBySubjectId,
    int InputUsageUnits,
    int OutputUsageUnits,
    decimal AiCreditsConsumed,
    string? FailureCode,
    string? FailureMessage,
    string ReasoningSummary,
    uint Version);

public sealed record ExportAiFindingReadModel(
    Guid Id,
    Guid AnalysisRunId,
    string Category,
    string Title,
    string Summary,
    string Severity,
    decimal Confidence,
    string SourceType,
    IReadOnlyList<ExportAiSourceReference> SourceReferences,
    Guid? RelatedRequirementId,
    Guid? RelatedDocumentId,
    Guid? RelatedShipmentId,
    bool Actionable,
    bool HumanReviewRequired,
    DateTimeOffset CreatedAtUtc);

public sealed record ExportAiRecommendationReadModel(
    Guid Id,
    Guid AnalysisRunId,
    string Title,
    string Description,
    string Priority,
    decimal Confidence,
    string ReasoningSummary,
    string? ProposedActionType,
    string? ProposedActionPayloadJson,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewedBySubjectId,
    string? ReviewDecision,
    string? ReviewNote,
    uint Version);

public sealed record ExportAiRunDetailReadModel(
    ExportAiAnalysisRunReadModel Run,
    IReadOnlyList<ExportAiFindingReadModel> Findings,
    IReadOnlyList<ExportAiRecommendationReadModel> Recommendations,
    ExportAiContextSnapshotModel? ContextSnapshot);

public sealed record StartExportAiAnalysisCommand(
    Guid? ExportCaseId,
    Guid? CountryResearchCaseId,
    ExportAiAnalysisRunType RunType);

public sealed record ReviewExportAiRecommendationCommand(
    Guid ExportCaseId,
    Guid RecommendationId,
    uint ExpectedVersion,
    ExportAiReviewDecision Decision,
    string? ReviewNote);

public sealed record ExportAiWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? AnalysisRunId { get; init; }
    public uint? RunVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportAiWriteResult Success(Guid runId, uint version) =>
        new() { Succeeded = true, AnalysisRunId = runId, RunVersion = version };

    public static ExportAiWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record ExportAiRecommendationWriteResult
{
    public required bool Succeeded { get; init; }
    public uint? RecommendationVersion { get; init; }
    public uint? CaseVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportAiRecommendationWriteResult Success(uint recVersion, uint caseVersion) =>
        new() { Succeeded = true, RecommendationVersion = recVersion, CaseVersion = caseVersion };

    public static ExportAiRecommendationWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record ExportAiRunListResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public IReadOnlyList<ExportAiAnalysisRunReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportAiRunListResult Success(IReadOnlyList<ExportAiAnalysisRunReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportAiRunListResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportAiRunListResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record ExportAiRunDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportAiRunDetailReadModel? Detail { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportAiRunDetailResult Success(ExportAiRunDetailReadModel detail) =>
        new() { Succeeded = true, Detail = detail };

    public static ExportAiRunDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportAiRunDetailResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record ExportAiRecommendationListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ExportAiRecommendationReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportAiRecommendationListResult Success(IReadOnlyList<ExportAiRecommendationReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportAiRecommendationListResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public static class Export4LFailureCodes
{
    public const string AnalysisRunNotFound = "export.ai.run.not-found";
    public const string RecommendationNotFound = "export.ai.recommendation.not-found";
    public const string InvalidRecommendationStatus = "export.ai.recommendation.invalid-status";
    public const string ProviderUnavailable = "export.ai.provider.unavailable";
    public const string ContextUnavailable = "export.ai.context.unavailable";
}

public static class ExportAiOutboxMessageTypes
{
    public const string ExportAiAnalysisCompleted = "export-ai-analysis-completed";
    public const string ExportAiHighRiskFound = "export-ai-high-risk-found";
    public const string ExportAiRecommendationAccepted = "export-ai-recommendation-accepted";
    public const string ExportAiTaskRequested = "export-ai-task-requested";
}

public static class ExportAiTaskIntegrationContract
{
    public const string SourceModule = "export";
    public const string TaskType = "export-ai-recommendation";
}
