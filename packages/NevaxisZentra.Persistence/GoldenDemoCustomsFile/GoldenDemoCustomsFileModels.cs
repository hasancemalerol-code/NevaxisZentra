namespace NevaxisZentra.Persistence.GoldenDemoCustomsFile;

public sealed class GoldenDemoCustomsFileException : Exception
{
    public GoldenDemoCustomsFileException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed record GoldenDemoCustomsFileProductLine(
    string ProductCode,
    string Description,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    string? GtipCode,
    string OriginCountry);

public sealed record GoldenDemoCustomsFileTradeRequirements(
    bool RequiresAtr,
    bool RequiresEur1,
    bool RequiresCertificateOfOrigin);

public sealed record GoldenDemoCustomsFileDocumentContent(
    string OrderReference,
    string ShipmentReference,
    int PackageCount,
    decimal NetWeightKg,
    decimal GrossWeightKg,
    IReadOnlyList<GoldenDemoCustomsFileProductLine> Lines,
    string Currency,
    string Incoterm,
    string DestinationCountry,
    string CustomerName);

public sealed record GoldenDemoCustomsFileRequirementResult(
    string RequirementCode,
    string Label,
    string SourceRule,
    bool Required,
    string Status,
    string Classification,
    bool Blocking,
    string Explanation,
    string? Evidence);

public sealed record GoldenDemoCustomsFileConsistencyCheck(
    string CheckCode,
    string Label,
    string Status,
    string Explanation,
    string Classification);

public sealed record GoldenDemoCustomsFileBlockerModel(
    Guid Id,
    string BlockerCode,
    string Status,
    string Reason,
    string EvidenceJson,
    Guid? RelatedDocumentId,
    DateTimeOffset DetectedAtUtc,
    DateTimeOffset? ResolvedAtUtc);

public sealed record GoldenDemoCustomsFileCorrectiveWorkModel(
    Guid Id,
    string Title,
    string Status,
    string ResponsibleRole,
    string CompletionEvidenceJson,
    Guid? RelatedDocumentId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record GoldenDemoCustomsFileDocumentModel(
    Guid Id,
    string DocumentType,
    int VersionNumber,
    bool IsCurrent,
    string Status,
    GoldenDemoCustomsFileDocumentContent Content,
    DateTimeOffset CreatedAtUtc);

public sealed record GoldenDemoCustomsFileActivityModel(
    string StageCode,
    string StageLabel,
    string Actor,
    string Action,
    string Status,
    string EvidenceReference,
    DateTimeOffset OccurredAtUtc);

public sealed record GoldenDemoCustomsFileApprovalModel(
    bool IsApprovalRequired,
    bool IsApproved,
    string ApprovingRole,
    string? ApprovedBySubjectId,
    DateTimeOffset? ApprovedAtUtc,
    string AuditReference);

public sealed record GoldenDemoCustomsFilePackageModel(
    Guid Id,
    int PackageVersion,
    IReadOnlyList<string> DocumentManifest,
    IReadOnlyList<Guid> DocumentReferenceIds,
    DateTimeOffset AssembledAtUtc,
    string ArtifactLabel);

public sealed record GoldenDemoCustomsFileVerificationModel(
    string Result,
    string ResultLabel,
    IReadOnlyList<GoldenDemoCustomsFileRequirementResult> Checks,
    string Summary,
    DateTimeOffset? VerifiedAtUtc);

public sealed record GoldenDemoCustomsFileHeroModel(
    bool ShowSuccessHero,
    string Headline,
    string Subheadline,
    string VerificationResult,
    int DocumentsReadyCount,
    int DocumentsTotalCount,
    int BlockingIssueCount,
    string ConsistencyStatus,
    string ApprovalStatus,
    string BrokerHandoffStatus,
    Guid? PackageId,
    int? PackageVersion);

public sealed record GoldenDemoCustomsFileWorkspaceSnapshot(
    Guid OutcomeId,
    string WorkerCode,
    string WorkerLabelEn,
    string WorkerLabelTr,
    string AssignedOutcomeText,
    string Status,
    string StatusLabel,
    string OrderNumber,
    string CustomerName,
    string DestinationCountry,
    string DeliveryAddress,
    string Currency,
    string Incoterm,
    int PackageCount,
    decimal ShipmentNetWeightKg,
    decimal ShipmentGrossWeightKg,
    string ShipmentReference,
    IReadOnlyList<GoldenDemoCustomsFileProductLine> ProductLines,
    GoldenDemoCustomsFileTradeRequirements TradeRequirements,
    IReadOnlyList<GoldenDemoCustomsFileDocumentModel> Documents,
    IReadOnlyList<GoldenDemoCustomsFileRequirementResult> Requirements,
    IReadOnlyList<GoldenDemoCustomsFileConsistencyCheck> ConsistencyChecks,
    IReadOnlyList<GoldenDemoCustomsFileBlockerModel> Blockers,
    IReadOnlyList<GoldenDemoCustomsFileCorrectiveWorkModel> CorrectiveWorks,
    IReadOnlyList<GoldenDemoCustomsFileActivityModel> ActivityTrace,
    GoldenDemoCustomsFileApprovalModel Approval,
    GoldenDemoCustomsFilePackageModel? Package,
    GoldenDemoCustomsFileVerificationModel Verification,
    GoldenDemoCustomsFileHeroModel Hero);

public sealed record AssignCustomsOutcomeRequest(string? Comment);
public sealed record CompleteCustomsCorrectiveWorkRequest(string IdempotencyKey);
public sealed record ApproveCustomsPackageRequest(string? Comment);
public sealed record VerifyCustomsOutcomeRequest(string IdempotencyKey);
