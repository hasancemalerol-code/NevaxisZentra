namespace NevaxisZentra.Api.Export;

// ─── Rule pack request DTOs ───────────────────────────────────────────────────

public sealed record CreateCountryRulePackRequest(
    string CountryCode,
    string Name,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceTitle,
    string? SourceUrl,
    string SourcePublisher,
    DateOnly? SourcePublicationDate,
    string? SourceVersion);

public sealed record AddCountryComplianceRuleRequest(
    uint ExpectedRulePackVersion,
    string RuleCode,
    string Title,
    string Description,
    string RuleType,
    string Severity,
    string RequirementMode,
    bool Blocking,
    string? RequiredDocumentType,
    string? ProductScope,
    string? ProductGroup,
    string? HsCodePattern,
    string SourceReference);

public sealed record VerifyCountryRulePackRequest(uint ExpectedVersion);

public sealed record SupersedeCountryRulePackRequest(
    uint ExpectedVersion,
    Guid NewRulePackId);

// ─── Compliance case request DTOs ─────────────────────────────────────────────

public sealed record EvaluateExportComplianceRequest(
    uint ExpectedCaseVersion,
    Guid? ShipmentId);

public sealed record RevalidateExportComplianceRequest(uint ExpectedCaseVersion);

public sealed record SatisfyComplianceRequirementRequest(
    uint ExpectedRequirementVersion,
    string ResolutionType,
    string ResolutionNote);

// ─── Country research request DTOs ────────────────────────────────────────────

public sealed record CreateCountryResearchCaseRequest(
    string CountryCode,
    string? ProductReference,
    string? ProductDescription,
    string? ProductGroup,
    string? HsCode,
    string? OriginCountryCode,
    string? PurposeNotes);

public sealed record AddCountryResearchFindingRequest(
    uint ExpectedResearchVersion,
    string Category,
    string Title,
    string Summary,
    string SourcePublisher,
    string SourceTitle,
    string? SourceUrl,
    DateOnly? SourcePublicationDate,
    int Confidence,
    bool BlockingPotential);

public sealed record ReviewCountryResearchCaseRequest(uint ExpectedVersion);

public sealed record CompleteCountryResearchCaseRequest(uint ExpectedVersion);

public sealed record LinkResearchToExportCaseRequest(
    uint ExpectedResearchVersion,
    Guid ExportCaseId,
    uint ExpectedCaseVersion);

// ─── Country knowledge request DTOs ───────────────────────────────────────────

public sealed record CreateCountryKnowledgeEntryRequest(
    string CountryCode,
    string Category,
    string Title,
    string Summary,
    string SourcePublisher,
    string SourceTitle,
    string? SourceUrl,
    string Authority,
    string? ProductScope,
    string? ProductGroup,
    string? HsCodePattern,
    Guid? CreatedFromResearchCaseId);

public sealed record VerifyCountryKnowledgeEntryRequest(uint ExpectedVersion);

public sealed record SupersedeCountryKnowledgeEntryRequest(
    uint ExpectedVersion,
    Guid NewEntryId);

// ─── Response DTOs ────────────────────────────────────────────────────────────

public sealed record CountryRulePackDto(
    Guid Id,
    Guid? FacilityId,
    string CountryCode,
    string Name,
    string Status,
    int VersionNumber,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceTitle,
    string? SourceUrl,
    string SourcePublisher,
    DateTimeOffset? VerifiedAtUtc,
    string? VerifiedBySubjectId,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record CountryComplianceRuleDto(
    Guid Id,
    Guid RulePackId,
    string RuleCode,
    string Title,
    string Description,
    string RuleType,
    string Severity,
    string RequirementMode,
    bool Blocking,
    string? RequiredDocumentType,
    string? ProductScope,
    string? HsCodePattern,
    string SourceReference,
    uint Version);

public sealed record CountryRulePackDetailDto(
    CountryRulePackDto RulePack,
    CountryComplianceRuleDto[] Rules);

public sealed record ExportComplianceRequirementDto(
    Guid Id,
    Guid ExportCaseId,
    Guid EvaluationId,
    string RequirementCode,
    string Title,
    string Description,
    string Status,
    string Severity,
    bool Blocking,
    string? RequiredDocumentType,
    Guid? RelatedDocumentId,
    string SourceReference,
    uint Version);

public sealed record ExportComplianceSummaryDto(
    Guid ExportCaseId,
    string ComplianceStatus,
    int ComplianceReadinessPercent,
    bool RevalidationRequired,
    Guid? CurrentEvaluationId,
    int BlockingCount,
    int MissingCount,
    ExportComplianceRequirementDto[] Requirements,
    CountryRulePackDto? RulePack);

public sealed record ExportComplianceEvaluationDto(
    Guid Id,
    Guid ExportCaseId,
    Guid? ShipmentId,
    int EvaluationNumber,
    string CountryCode,
    string? HsCodeSnapshot,
    Guid? RulePackId,
    int? RulePackVersionNumber,
    string Result,
    int ReadinessPercent,
    bool IsStale,
    bool RevalidationRequired,
    Guid? SupersedesEvaluationId,
    DateTimeOffset EvaluatedAtUtc,
    uint Version);

public sealed record ExportComplianceWriteSuccessDto(
    Guid EvaluationId,
    uint CaseVersion,
    string Result);

public sealed record ExportComplianceRequirementWriteSuccessDto(
    uint RequirementVersion,
    uint CaseVersion);

public sealed record CountryRulePackWriteSuccessDto(
    Guid RulePackId,
    uint Version);

public sealed record CountryComplianceRuleWriteSuccessDto(
    Guid RuleId,
    uint RulePackVersion);

public sealed record CountryResearchCaseDto(
    Guid Id,
    Guid FacilityId,
    string ResearchNumber,
    string CountryCode,
    string? ProductReference,
    string? ProductDescription,
    string? ProductGroup,
    string? HsCode,
    string? OriginCountryCode,
    string Status,
    string? PurposeNotes,
    Guid? LinkedExportCaseId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record CountryResearchFindingDto(
    Guid Id,
    Guid CountryResearchCaseId,
    string Category,
    string Title,
    string Summary,
    string SourcePublisher,
    string SourceTitle,
    string? SourceUrl,
    int Confidence,
    string VerificationStatus,
    uint Version,
    DateTimeOffset CreatedAtUtc);

public sealed record CountryResearchDetailDto(
    CountryResearchCaseDto ResearchCase,
    CountryResearchFindingDto[] Findings);

public sealed record CountryResearchWriteSuccessDto(
    Guid ResearchCaseId,
    uint Version);

public sealed record CountryResearchFindingWriteSuccessDto(
    Guid FindingId,
    uint ResearchVersion);

public sealed record CountryKnowledgeEntryDto(
    Guid Id,
    string CountryCode,
    string Category,
    string Title,
    string Summary,
    string SourcePublisher,
    string SourceTitle,
    string? SourceUrl,
    string Authority,
    string VerificationStatus,
    DateTimeOffset? VerifiedAtUtc,
    bool RevalidationRequired,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record CountryKnowledgeWriteSuccessDto(
    Guid KnowledgeEntryId,
    uint Version);

public sealed record ExportTraceabilityDocumentRefDto(
    Guid Id,
    string DocumentType,
    string Status,
    uint Version);

public sealed record ExportTraceabilityPackingRefDto(
    Guid Id,
    string UnitCode,
    string Status);

public sealed record ExportTraceabilityShipmentRefDto(
    Guid Id,
    string ShipmentReference,
    string Status,
    uint Version);

public sealed record ExportTraceabilityBrokerRefDto(
    Guid WorkspaceId,
    int? PackageVersionNumber,
    string Status);

public sealed record ExportTraceabilityComplianceRefDto(
    Guid? EvaluationId,
    int? EvaluationNumber,
    string Result,
    int? RulePackVersion);

public sealed record ExportTraceabilityResearchRefDto(
    Guid? ResearchCaseId,
    string? ResearchNumber,
    string Status);

public sealed record ExportTraceabilityDto(
    Guid ExportCaseId,
    string CaseNumber,
    Guid SourceOrderId,
    string SourceOrderReference,
    string CustomerName,
    string DestinationCountryCode,
    ExportTraceabilityDocumentRefDto[] Documents,
    ExportTraceabilityPackingRefDto[] PackingUnits,
    ExportTraceabilityShipmentRefDto[] Shipments,
    ExportTraceabilityBrokerRefDto? Broker,
    ExportTraceabilityComplianceRefDto? Compliance,
    ExportTraceabilityResearchRefDto? Research,
    int ActivityCount);
