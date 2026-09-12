namespace NevaxisFactOS.Persistence.Maintenance;

public static class MaintenanceAiFailureCodes
{
    public const string NotFound = "not-found";
    public const string InvalidStatus = "invalid-status";
    public const string StaleDraft = "stale-draft";
    public const string InvalidOutput = "invalid-output";
    public const string ProviderFailed = "provider-failed";
    public const string DuplicateDraft = "duplicate-draft";
    public const string AlreadyExecuted = "already-executed";
    public const string AlreadyRejected = "already-rejected";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string ValidationFailed = "validation-failed";
    public const string SourceUnavailable = "source-unavailable";
    public const string DuplicateRequest = "duplicate-request";
    public const string MissingRiskAssessment = "missing-risk-assessment";
}

public sealed record MaintenanceAiEvidenceReferenceInput(
    string EvidenceType,
    string EvidenceReference,
    string Summary);

public sealed record MaintenanceAiRiskDriverInput(string DriverCode, string Summary);

public sealed record MaintenanceAiAnomalySummaryInput(
    string MetricKey,
    string Severity,
    string Summary,
    string EvaluationIdentity);

public sealed record MaintenanceAiOpenRequestSummaryInput(
    Guid RequestId,
    string RequestNumber,
    string Title,
    string Status);

public sealed record MaintenanceAiInputSnapshot(
    Guid TenantId,
    Guid FacilityId,
    string AssetReference,
    string? AssetName,
    string? AssetType,
    string? AssetId,
    string ConditionStatus,
    string ConditionEvaluationReference,
    Guid RiskAssessmentId,
    string RiskBand,
    string DeterministicRecommendationCategory,
    string DataFreshness,
    string DataSufficiency,
    int ActiveAnomalyCount,
    string RuleSetVersion,
    DateTimeOffset AssessmentTimestamp,
    DateTimeOffset? ConditionEvaluatedAtUtc,
    IReadOnlyList<MaintenanceAiRiskDriverInput> RiskDrivers,
    IReadOnlyList<MaintenanceAiEvidenceReferenceInput> EvidenceReferences,
    IReadOnlyList<MaintenanceAiAnomalySummaryInput> ActiveAnomalies,
    int RecentAlarmCount24h,
    int DowntimeEpisodeCount24h,
    decimal DowntimeMinutes24h,
    int RecentFailureCount90d,
    IReadOnlyList<MaintenanceAiOpenRequestSummaryInput> OpenRequests);

public sealed record MaintenanceAiStructuredOutput(
    string Summary,
    string ObservedCondition,
    string SuggestedTitle,
    string SuggestedDescription,
    string SuggestedPriority,
    string SuggestedRequestType,
    string RecommendedReviewType,
    IReadOnlyList<MaintenanceAiEvidenceReferenceInput> EvidenceReferences,
    IReadOnlyList<string> MissingInformation,
    string UncertaintyExplanation,
    string QualitativeConfidence,
    IReadOnlyList<string> SafetyNotes,
    bool ProhibitedActionConfirmation);

public sealed record MaintenanceAiProviderRequest(
    string PromptVersion,
    MaintenanceAiInputSnapshot InputSnapshot,
    string CorrelationIdentity);

public sealed record MaintenanceAiProviderResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureMessage,
    MaintenanceAiStructuredOutput? Output,
    string RawResponseJson,
    string ProviderIdentifier,
    string ModelIdentifier);

public sealed record MaintenanceAiDraftWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? DraftId { get; init; }
    public uint? Version { get; init; }
    public Guid? MaintenanceRequestId { get; init; }
    public string? RequestNumber { get; init; }

    public static MaintenanceAiDraftWriteResult Success(Guid draftId, uint version) =>
        new() { Succeeded = true, DraftId = draftId, Version = version };

    public static MaintenanceAiDraftWriteResult SuccessWithRequest(
        Guid draftId,
        uint version,
        Guid requestId,
        string? requestNumber) =>
        new()
        {
            Succeeded = true,
            DraftId = draftId,
            Version = version,
            MaintenanceRequestId = requestId,
            RequestNumber = requestNumber,
        };

    public static MaintenanceAiDraftWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record MaintenanceAiDraftSummaryReadModel(
    Guid Id,
    string AssetReference,
    string? AssetName,
    string Status,
    string QualitativeConfidence,
    string SuggestedTitle,
    uint Version,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? CreatedMaintenanceRequestId,
    bool IsDevelopmentProvider);

public sealed record MaintenanceAiDraftDetailReadModel(
    Guid Id,
    string AssetReference,
    string? AssetName,
    string Status,
    string QualitativeConfidence,
    string ConditionEvaluationReference,
    Guid RiskAssessmentId,
    string InputFingerprint,
    string RuleSetVersion,
    string ProviderIdentifier,
    string ModelIdentifier,
    string PromptVersion,
    bool IsDevelopmentProvider,
    string DataFreshness,
    string DataSufficiency,
    string RiskBand,
    string DeterministicRecommendationCategory,
    MaintenanceAiStructuredOutput ValidatedOutput,
    MaintenanceAiStructuredOutput AiOriginalOutput,
    string SuggestedTitle,
    string SuggestedDescription,
    string SuggestedPriority,
    string SuggestedRequestType,
    string? HumanEditedTitle,
    string? HumanEditedDescription,
    string? HumanEditedPriority,
    string? HumanEditedRequestType,
    string? HumanEditorSubjectId,
    DateTimeOffset? HumanEditedAtUtc,
    string GeneratedBySubjectId,
    DateTimeOffset GeneratedAtUtc,
    string? ApprovedBySubjectId,
    DateTimeOffset? ApprovedAtUtc,
    string? RejectedBySubjectId,
    DateTimeOffset? RejectedAtUtc,
    string? RejectionReason,
    Guid? CreatedMaintenanceRequestId,
    string? CreatedMaintenanceRequestNumber,
    string? FailureCode,
    string? FailureMessage,
    uint Version,
    IReadOnlyList<MaintenanceAiDraftAuditReadModel> AuditTrail);

public sealed record MaintenanceAiDraftAuditReadModel(
    string Action,
    string ActorSubjectId,
    uint DraftVersion,
    string ResultCode,
    DateTimeOffset OccurredAtUtc);

public sealed record UpdateMaintenanceAiDraftCommand(
    Guid DraftId,
    uint ExpectedVersion,
    string? Title,
    string? Description,
    string? Priority,
    string? RequestType);

public sealed record ApproveMaintenanceAiDraftCommand(
    Guid DraftId,
    uint ExpectedVersion);

public sealed record RejectMaintenanceAiDraftCommand(
    Guid DraftId,
    uint ExpectedVersion,
    string RejectionReason);

public interface IMaintenanceAiProvider
{
    string ProviderName { get; }
    string ModelName { get; }
    bool IsDevelopmentProvider { get; }

    Task<MaintenanceAiProviderResult> GenerateDraftAsync(
        MaintenanceAiProviderRequest request,
        CancellationToken cancellationToken = default);
}

public interface IMaintenanceAiDraftRepository
{
    Task<MaintenanceAiDraftDetailReadModel?> GetCurrentDraftAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceAiDraftSummaryReadModel>> ListDraftsAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<MaintenanceAiDraftDetailReadModel?> GetDraftByIdAsync(
        MaintenanceOperationContext context,
        Guid draftId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceAiDraftWriteResult> GenerateDraftAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default);

    Task<MaintenanceAiDraftWriteResult> UpdateDraftAsync(
        MaintenanceOperationContext context,
        UpdateMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceAiDraftWriteResult> ApproveDraftAsync(
        MaintenanceOperationContext context,
        ApproveMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceAiDraftWriteResult> RejectDraftAsync(
        MaintenanceOperationContext context,
        RejectMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken = default);
}
