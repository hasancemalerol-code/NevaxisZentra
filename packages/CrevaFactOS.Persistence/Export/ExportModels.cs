namespace CrevaFactOS.Persistence.Export;

public sealed record ExportOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record EnsureExportCaseForOrderCommand(
    Guid SourceOrderId,
    string SourceOrderReference,
    string CustomerName,
    bool IsExport,
    string DestinationCountryCode,
    string? OriginCountryCode,
    string? IncotermCode);

public sealed record UpdateExportCaseCommand(
    Guid ExportCaseId,
    uint ExpectedVersion,
    string? DestinationCountryCode,
    string? OriginCountryCode,
    string? IncotermCode,
    ExportCaseStatus? Status);

public sealed record RegisterExportDocumentCommand(
    Guid ExportCaseId,
    string DocumentType,
    string DisplayName,
    string? FileReference,
    string? DocumentNumber,
    DateTimeOffset? IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Source,
    ExportDocumentStatus InitialStatus = ExportDocumentStatus.Draft);

public sealed record UpdateExportDocumentCommand(
    Guid ExportCaseId,
    Guid DocumentId,
    uint ExpectedVersion,
    string? DisplayName,
    string? FileReference,
    string? DocumentNumber,
    DateTimeOffset? IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    ExportDocumentStatus? DocumentStatus);

public sealed record VerifyExportDocumentCommand(
    Guid ExportCaseId,
    Guid DocumentId,
    uint ExpectedVersion);

public sealed record RejectExportDocumentCommand(
    Guid ExportCaseId,
    Guid DocumentId,
    uint ExpectedVersion,
    string Reason);

public static class ExportFailureCodes
{
    public const string InvalidScope = "invalid-scope";
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
}

public sealed record ExportCaseListReadModel(
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

public sealed record ExportCaseDetailReadModel(
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

public sealed record ExportDocumentReadModel(
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

public sealed record ExportCaseActivityReadModel(
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

public sealed record ExportRequirementEvaluation(
    string Key,
    string Label,
    bool IsBlocking,
    bool IsSatisfied,
    string Status);

public sealed record ExportReadinessCalculationResult(
    ExportReadinessStatus ReadinessStatus,
    int ReadinessPercent,
    int BlockingIssueCount,
    int MissingRequirementCount,
    ExportCaseStatus SuggestedCaseStatus,
    IReadOnlyList<ExportRequirementEvaluation> Requirements);

public sealed record ExportReadinessReadModel(
    Guid ExportCaseId,
    string ReadinessStatus,
    int ReadinessPercent,
    int BlockingIssueCount,
    int MissingRequirementCount,
    string SuggestedCaseStatus,
    IReadOnlyList<ExportRequirementEvaluation> Requirements);

public sealed record ExportCaseWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ExportCaseId { get; init; }
    public string? CaseNumber { get; init; }
    public string? Status { get; init; }
    public string? ReadinessStatus { get; init; }
    public int? ReadinessPercent { get; init; }
    public uint? Version { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public bool AlreadyExisted { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCaseWriteResult Success(
        Guid exportCaseId,
        string caseNumber,
        string status,
        string readinessStatus,
        int readinessPercent,
        uint version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool alreadyExisted = false) =>
        new()
        {
            Succeeded = true,
            ExportCaseId = exportCaseId,
            CaseNumber = caseNumber,
            Status = status,
            ReadinessStatus = readinessStatus,
            ReadinessPercent = readinessPercent,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            AlreadyExisted = alreadyExisted,
        };

    public static ExportCaseWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportDocumentWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? DocumentId { get; init; }
    public uint? DocumentVersion { get; init; }
    public uint? CaseVersion { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportDocumentWriteResult Success(Guid documentId, uint documentVersion, uint caseVersion) =>
        new()
        {
            Succeeded = true,
            DocumentId = documentId,
            DocumentVersion = documentVersion,
            CaseVersion = caseVersion,
        };

    public static ExportDocumentWriteResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCaseListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ExportCaseListReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCaseListResult Success(IReadOnlyList<ExportCaseListReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportCaseListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportCaseDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportCaseDetailReadModel? Case { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportCaseDetailResult Success(ExportCaseDetailReadModel model) =>
        new() { Succeeded = true, Case = model };

    public static ExportCaseDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportCaseDetailResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportDocumentListResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public IReadOnlyList<ExportDocumentReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportDocumentListResult Success(IReadOnlyList<ExportDocumentReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportDocumentListResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportDocumentListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportActivityListResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public IReadOnlyList<ExportCaseActivityReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportActivityListResult Success(IReadOnlyList<ExportCaseActivityReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ExportActivityListResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportActivityListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ExportReadinessResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ExportReadinessReadModel? Readiness { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ExportReadinessResult Success(ExportReadinessReadModel model) =>
        new() { Succeeded = true, Readiness = model };

    public static ExportReadinessResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ExportFailureCodes.NotFound };

    public static ExportReadinessResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}
