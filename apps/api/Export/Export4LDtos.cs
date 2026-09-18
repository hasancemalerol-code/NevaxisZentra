namespace NevaxisZentra.Api.Export;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record AnalyzeExportCaseRequest(string RunType);

public sealed record ReviewExportAiRecommendationRequest(
    uint ExpectedVersion,
    string? ReviewNote);

// ─── Response DTOs ────────────────────────────────────────────────────────────

public sealed record ExportAiSourceReferenceDto(
    string SourceType,
    string SourceId,
    string Label,
    string? Detail);

public sealed record ExportAiAnalysisRunDto(
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

public sealed record ExportAiFindingDto(
    Guid Id,
    Guid AnalysisRunId,
    string Category,
    string Title,
    string Summary,
    string Severity,
    decimal Confidence,
    string SourceType,
    ExportAiSourceReferenceDto[] SourceReferences,
    Guid? RelatedRequirementId,
    Guid? RelatedDocumentId,
    Guid? RelatedShipmentId,
    bool Actionable,
    bool HumanReviewRequired,
    DateTimeOffset CreatedAtUtc);

public sealed record ExportAiRecommendationDto(
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

public sealed record ExportAiContextCaseRefDto(
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

public sealed record ExportAiContextDocumentRefDto(
    Guid Id,
    string DocumentType,
    string Status,
    string? FileName);

public sealed record ExportAiContextRequirementRefDto(
    Guid Id,
    string RequirementCode,
    string Title,
    string Status,
    bool Blocking,
    string? RequiredDocumentType,
    string SourceReference);

public sealed record ExportAiContextComplianceRefDto(
    Guid? EvaluationId,
    int? EvaluationNumber,
    string Result,
    int? RulePackVersion,
    string? RulePackName,
    ExportAiContextRequirementRefDto[] Requirements);

public sealed record ExportAiContextResearchRefDto(
    Guid? ResearchCaseId,
    string? ResearchNumber,
    string Status,
    int FindingCount,
    int UnverifiedFindingCount);

public sealed record ExportAiContextShipmentRefDto(
    Guid Id,
    string ShipmentNumber,
    string Status,
    DateTimeOffset? PlannedDepartureUtc,
    DateTimeOffset? PackingDeadlineUtc);

public sealed record ExportAiContextBrokerRefDto(
    Guid WorkspaceId,
    string Status,
    int OpenCorrectionCount);

public sealed record ExportAiContextKnowledgeRefDto(
    Guid Id,
    string Title,
    string Authority,
    string VerificationStatus);

public sealed record ExportCrossModuleRiskDto(
    string Type,
    string Severity,
    string SourceModule,
    string SourceRecordReference,
    string Description,
    DateTimeOffset DetectedAtUtc,
    bool Blocking);

public sealed record ExportAiContextSnapshotDto(
    Guid? ExportCaseId,
    Guid? CountryResearchCaseId,
    ExportAiContextCaseRefDto? ExportCase,
    string? SourceOrderReference,
    string? CustomerName,
    ExportAiContextDocumentRefDto[] Documents,
    string PackingStatus,
    ExportAiContextShipmentRefDto[] Shipments,
    ExportAiContextBrokerRefDto? Broker,
    ExportAiContextComplianceRefDto? Compliance,
    ExportAiContextResearchRefDto? Research,
    ExportAiContextKnowledgeRefDto[] KnowledgeEntries,
    ExportCrossModuleRiskDto[] CrossModuleRisks,
    DateTimeOffset CapturedAtUtc);

public sealed record ExportAiRunDetailDto(
    ExportAiAnalysisRunDto Run,
    ExportAiFindingDto[] Findings,
    ExportAiRecommendationDto[] Recommendations,
    ExportAiContextSnapshotDto? ContextSnapshot);

public sealed record ExportAiAnalysisWriteSuccessDto(
    Guid AnalysisRunId,
    uint RunVersion);

public sealed record ExportAiRecommendationReviewSuccessDto(
    uint RecommendationVersion,
    uint CaseVersion);

public sealed record AiUsageSummaryDto(
    decimal CreditsConsumedThisPeriod,
    decimal? MonthlyQuota,
    decimal UsagePercent,
    string WarningLevel);
