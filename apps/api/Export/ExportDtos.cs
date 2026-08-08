namespace CrevaFactOS.Api.Export;

public static class ExportApiFailureCodes
{
    public const string InvalidRequest = "invalid-request";
    public const string Unauthenticated = "unauthenticated";
    public const string MembershipMissing = "membership-missing";
    public const string FacilityAccessDenied = "facility-access-denied";
    public const string PermissionDenied = "permission-denied";
    public const string NotFound = "not-found";
    public const string DocumentNotFound = "document-not-found";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string ValidationFailed = "validation-failed";
    public const string NotExportOrder = "not-export-order";
    public const string DestinationCountryRequired = "destination-country-required";
    public const string InvalidDestinationCountry = "invalid-destination-country";
    public const string InvalidOriginCountry = "invalid-origin-country";
    public const string InvalidDocumentType = "invalid-document-type";
    public const string InvalidDocumentStatus = "invalid-document-status";
    public const string RejectionReasonRequired = "rejection-reason-required";
    public const string CaseClosed = "case-closed";
    public const string UnexpectedError = "unexpected-error";
}

public sealed record ExportCaseSummaryDto(
    Guid Id,
    Guid FacilityId,
    Guid SourceOrderId,
    string CaseNumber,
    string SourceOrderReference,
    string CustomerName,
    string DestinationCountryCode,
    string? OriginCountryCode,
    string? IncotermCode,
    string Status,
    string ReadinessStatus,
    int ReadinessPercent,
    int BlockingIssueCount,
    int MissingRequirementCount,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record ExportCaseDetailDto(
    Guid Id,
    Guid FacilityId,
    Guid SourceOrderId,
    string CaseNumber,
    string SourceOrderReference,
    string CustomerName,
    string DestinationCountryCode,
    string? OriginCountryCode,
    string? IncotermCode,
    string Status,
    string ReadinessStatus,
    int ReadinessPercent,
    int BlockingIssueCount,
    int MissingRequirementCount,
    string CreatedBySubjectId,
    string LastModifiedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ExportCaseWriteSuccessDto(
    Guid ExportCaseId,
    string CaseNumber,
    string Status,
    string ReadinessStatus,
    int ReadinessPercent,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool AlreadyExisted);

public sealed record EnsureExportCaseFromOrderRequest(
    Guid FacilityId,
    Guid SourceOrderId,
    string SourceOrderReference,
    string CustomerName,
    bool IsExport,
    string DestinationCountryCode,
    string? OriginCountryCode,
    string? IncotermCode);

public sealed record ExportDocumentDto(
    Guid Id,
    Guid ExportCaseId,
    string DocumentType,
    string DocumentStatus,
    string DisplayName,
    string? FileReference,
    string? DocumentNumber,
    DateTimeOffset? IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Source,
    string? RejectionReason,
    DateTimeOffset? VerifiedAtUtc,
    string? VerifiedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record RegisterExportDocumentRequest(
    string DocumentType,
    string DisplayName,
    string? FileReference,
    string? DocumentNumber,
    DateTimeOffset? IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? Source,
    string? InitialStatus);

public sealed record VerifyExportDocumentRequest(uint ExpectedVersion);

public sealed record RejectExportDocumentRequest(uint ExpectedVersion, string Reason);

public sealed record ExportDocumentWriteSuccessDto(
    Guid DocumentId,
    uint DocumentVersion,
    uint CaseVersion);

public sealed record ExportRequirementDto(
    string Key,
    string Label,
    bool IsBlocking,
    bool IsSatisfied,
    string Status);

public sealed record ExportReadinessDto(
    Guid ExportCaseId,
    string ReadinessStatus,
    int ReadinessPercent,
    int BlockingIssueCount,
    int MissingRequirementCount,
    string SuggestedCaseStatus,
    IReadOnlyList<ExportRequirementDto> Requirements);

public sealed record ExportActivityDto(
    Guid Id,
    Guid ExportCaseId,
    string EventType,
    string Title,
    string? Summary,
    DateTimeOffset OccurredAtUtc,
    string SourceType,
    string? ActorSubjectId,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    Guid CorrelationId);
