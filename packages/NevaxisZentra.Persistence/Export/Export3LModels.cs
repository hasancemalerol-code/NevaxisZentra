namespace NevaxisZentra.Persistence.Export;

public static class Export3LFailureCodes
{
    public const string RulePackNotFound = "rule-pack-not-found";
    public const string RuleNotFound = "rule-not-found";
    public const string EvaluationNotFound = "evaluation-not-found";
    public const string RequirementNotFound = "requirement-not-found";
    public const string ResearchCaseNotFound = "research-case-not-found";
    public const string FindingNotFound = "finding-not-found";
    public const string KnowledgeEntryNotFound = "knowledge-entry-not-found";
    public const string InvalidRulePackStatus = "invalid-rule-pack-status";
    public const string InvalidResearchStatus = "invalid-research-status";
    public const string SourceRequired = "source-required";
    public const string AlreadyVerified = "already-verified";
    public const string EvaluationImmutable = "evaluation-immutable";
}

public sealed record CreateCountryRulePackCommand(
    string CountryCode,
    string Name,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceTitle,
    string? SourceUrl,
    string SourcePublisher,
    DateOnly? SourcePublicationDate,
    string? SourceVersion);

public sealed record AddCountryComplianceRuleCommand(
    Guid RulePackId,
    uint ExpectedRulePackVersion,
    string RuleCode,
    string Title,
    string Description,
    ExportComplianceRuleType RuleType,
    ExportComplianceSeverity Severity,
    ExportComplianceRequirementMode RequirementMode,
    bool Blocking,
    string? RequiredDocumentType,
    string? ProductScope,
    string? ProductGroup,
    string? HsCodePattern,
    string SourceReference);

public sealed record VerifyCountryRulePackCommand(Guid RulePackId, uint ExpectedVersion);

public sealed record SupersedeCountryRulePackCommand(
    Guid RulePackId,
    uint ExpectedVersion,
    Guid NewRulePackId);

public sealed record EvaluateExportComplianceCommand(
    Guid ExportCaseId,
    uint ExpectedCaseVersion,
    Guid? ShipmentId);

public sealed record RevalidateExportComplianceCommand(
    Guid ExportCaseId,
    uint ExpectedCaseVersion);

public sealed record SatisfyComplianceRequirementCommand(
    Guid ExportCaseId,
    Guid RequirementId,
    uint ExpectedRequirementVersion,
    string ResolutionType,
    string ResolutionNote);

public sealed record CreateCountryResearchCaseCommand(
    string CountryCode,
    string? ProductReference,
    string? ProductDescription,
    string? ProductGroup,
    string? HsCode,
    string? OriginCountryCode,
    string? PurposeNotes);

public sealed record AddCountryResearchFindingCommand(
    Guid ResearchCaseId,
    uint ExpectedResearchVersion,
    ExportCountryResearchFindingCategory Category,
    string Title,
    string Summary,
    string SourcePublisher,
    string SourceTitle,
    string? SourceUrl,
    DateOnly? SourcePublicationDate,
    int Confidence,
    bool BlockingPotential);

public sealed record ReviewCountryResearchCaseCommand(
    Guid ResearchCaseId,
    uint ExpectedVersion);

public sealed record CompleteCountryResearchCaseCommand(
    Guid ResearchCaseId,
    uint ExpectedVersion);

public sealed record LinkResearchToExportCaseCommand(
    Guid ResearchCaseId,
    uint ExpectedResearchVersion,
    Guid ExportCaseId,
    uint ExpectedCaseVersion);

public sealed record CreateCountryKnowledgeEntryCommand(
    string CountryCode,
    ExportCountryResearchFindingCategory Category,
    string Title,
    string Summary,
    string SourcePublisher,
    string SourceTitle,
    string? SourceUrl,
    ExportCountryKnowledgeAuthority Authority,
    string? ProductScope,
    string? ProductGroup,
    string? HsCodePattern,
    Guid? CreatedFromResearchCaseId);

public sealed record VerifyCountryKnowledgeEntryCommand(
    Guid KnowledgeEntryId,
    uint ExpectedVersion);

public sealed record SupersedeCountryKnowledgeEntryCommand(
    Guid KnowledgeEntryId,
    uint ExpectedVersion,
    Guid NewEntryId);

public sealed record CountryRulePackReadModel(
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

public sealed record CountryComplianceRuleReadModel(
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

public sealed record ExportComplianceEvaluationReadModel(
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

public sealed record ExportComplianceRequirementReadModel(
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

public sealed record ExportComplianceSummaryReadModel(
    Guid ExportCaseId,
    string ComplianceStatus,
    int ComplianceReadinessPercent,
    bool RevalidationRequired,
    Guid? CurrentEvaluationId,
    int BlockingCount,
    int MissingCount,
    IReadOnlyList<ExportComplianceRequirementReadModel> Requirements,
    CountryRulePackReadModel? RulePack);

public sealed record CountryResearchCaseReadModel(
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

public sealed record CountryResearchFindingReadModel(
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

public sealed record CountryKnowledgeEntryReadModel(
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

public sealed record ExportTraceabilityReadModel(
    Guid ExportCaseId,
    string CaseNumber,
    Guid SourceOrderId,
    string SourceOrderReference,
    string CustomerName,
    string DestinationCountryCode,
    IReadOnlyList<ExportTraceabilityDocumentRef> Documents,
    IReadOnlyList<ExportTraceabilityPackingRef> PackingUnits,
    IReadOnlyList<ExportTraceabilityShipmentRef> Shipments,
    ExportTraceabilityBrokerRef? Broker,
    ExportTraceabilityComplianceRef? Compliance,
    ExportTraceabilityResearchRef? Research,
    int ActivityCount);

public sealed record ExportTraceabilityDocumentRef(Guid Id, string DocumentType, string Status, uint Version);
public sealed record ExportTraceabilityPackingRef(Guid Id, string UnitCode, string Status);
public sealed record ExportTraceabilityShipmentRef(Guid Id, string ShipmentReference, string Status, uint Version);
public sealed record ExportTraceabilityBrokerRef(Guid WorkspaceId, int? PackageVersionNumber, string Status);
public sealed record ExportTraceabilityComplianceRef(Guid? EvaluationId, int? EvaluationNumber, string Result, int? RulePackVersion);
public sealed record ExportTraceabilityResearchRef(Guid? ResearchCaseId, string? ResearchNumber, string Status);

public sealed record ExportComplianceOperationalContext(
    ExportComplianceEvaluationResult ComplianceStatus,
    int ComplianceReadinessPercent,
    bool ComplianceRevalidationRequired,
    bool HasBlockingComplianceRequirement);

// ─── Write results ────────────────────────────────────────────────────────────

public sealed record ExportCountryRulePackWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? RulePackId { get; init; }
    public uint? Version { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryRulePackWriteResult Success(Guid rulePackId, uint version) =>
        new() { Succeeded = true, RulePackId = rulePackId, Version = version };

    public static ExportCountryRulePackWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryComplianceRuleWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? RuleId { get; init; }
    public uint? RulePackVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryComplianceRuleWriteResult Success(Guid ruleId, uint rulePackVersion) =>
        new() { Succeeded = true, RuleId = ruleId, RulePackVersion = rulePackVersion };

    public static ExportCountryComplianceRuleWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportComplianceWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? EvaluationId { get; init; }
    public uint? CaseVersion { get; init; }
    public string? Result { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportComplianceWriteResult Success(Guid evaluationId, uint caseVersion, string result) =>
        new() { Succeeded = true, EvaluationId = evaluationId, CaseVersion = caseVersion, Result = result };

    public static ExportComplianceWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportComplianceRequirementWriteResult
{
    public required bool Succeeded { get; init; }
    public uint? RequirementVersion { get; init; }
    public uint? CaseVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportComplianceRequirementWriteResult Success(uint requirementVersion, uint caseVersion) =>
        new() { Succeeded = true, RequirementVersion = requirementVersion, CaseVersion = caseVersion };

    public static ExportComplianceRequirementWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryResearchWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ResearchCaseId { get; init; }
    public uint? Version { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryResearchWriteResult Success(Guid researchCaseId, uint version) =>
        new() { Succeeded = true, ResearchCaseId = researchCaseId, Version = version };

    public static ExportCountryResearchWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryResearchFindingWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? FindingId { get; init; }
    public uint? ResearchVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryResearchFindingWriteResult Success(Guid findingId, uint researchVersion) =>
        new() { Succeeded = true, FindingId = findingId, ResearchVersion = researchVersion };

    public static ExportCountryResearchFindingWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryKnowledgeWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? KnowledgeEntryId { get; init; }
    public uint? Version { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryKnowledgeWriteResult Success(Guid knowledgeEntryId, uint version) =>
        new() { Succeeded = true, KnowledgeEntryId = knowledgeEntryId, Version = version };

    public static ExportCountryKnowledgeWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

// ─── Query results ────────────────────────────────────────────────────────────

public sealed record ExportCountryRulePackListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<CountryRulePackReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryRulePackListResult Success(IReadOnlyList<CountryRulePackReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportCountryRulePackListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryRulePackDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public CountryRulePackReadModel? RulePack { get; init; }
    public IReadOnlyList<CountryComplianceRuleReadModel>? Rules { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryRulePackDetailResult Success(
        CountryRulePackReadModel rulePack,
        IReadOnlyList<CountryComplianceRuleReadModel> rules) =>
        new() { Succeeded = true, RulePack = rulePack, Rules = rules };

    public static ExportCountryRulePackDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = Export3LFailureCodes.RulePackNotFound };

    public static ExportCountryRulePackDetailResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportComplianceSummaryResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportComplianceSummaryReadModel? Summary { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportComplianceSummaryResult Success(ExportComplianceSummaryReadModel summary) =>
        new() { Succeeded = true, Summary = summary };

    public static ExportComplianceSummaryResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportComplianceSummaryResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportComplianceEvaluationListResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public IReadOnlyList<ExportComplianceEvaluationReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportComplianceEvaluationListResult Success(
        IReadOnlyList<ExportComplianceEvaluationReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportComplianceEvaluationListResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportComplianceEvaluationListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryResearchListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<CountryResearchCaseReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryResearchListResult Success(IReadOnlyList<CountryResearchCaseReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportCountryResearchListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryResearchDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public CountryResearchCaseReadModel? ResearchCase { get; init; }
    public IReadOnlyList<CountryResearchFindingReadModel>? Findings { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryResearchDetailResult Success(
        CountryResearchCaseReadModel researchCase,
        IReadOnlyList<CountryResearchFindingReadModel> findings) =>
        new() { Succeeded = true, ResearchCase = researchCase, Findings = findings };

    public static ExportCountryResearchDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = Export3LFailureCodes.ResearchCaseNotFound };

    public static ExportCountryResearchDetailResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCountryKnowledgeListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<CountryKnowledgeEntryReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCountryKnowledgeListResult Success(IReadOnlyList<CountryKnowledgeEntryReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportCountryKnowledgeListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportTraceabilityResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportTraceabilityReadModel? Traceability { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportTraceabilityResult Success(ExportTraceabilityReadModel model) =>
        new() { Succeeded = true, Traceability = model };

    public static ExportTraceabilityResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportTraceabilityResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}
