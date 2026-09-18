namespace NevaxisZentra.Persistence.Maintenance;

public static class MaintenanceAiAgentPromptVersions
{
    public const string V1 = "MAINT-AI-AGENT-V1";
}

public static class MaintenanceAiAgentProviders
{
    public const string DeterministicDevelopment = "deterministic-local-validation";
    public const string DeterministicDevelopmentModel = "maint-ai-dev-v1";
}

public static class MaintenanceAiDraftStatuses
{
    public const string Draft = "draft";
    public const string UnderReview = "under-review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Stale = "stale";
    public const string Executed = "executed";
    public const string ExecutionFailed = "execution-failed";
}

public static class MaintenanceAiQualitativeConfidenceLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
}

public static class MaintenanceAiAuditActions
{
    public const string GenerationRequested = "maintenance.ai.draft.generation-requested";
    public const string ProviderCompleted = "maintenance.ai.draft.provider-completed";
    public const string ProviderFailed = "maintenance.ai.draft.provider-failed";
    public const string OutputValidated = "maintenance.ai.draft.output-validated";
    public const string OutputRejected = "maintenance.ai.draft.output-rejected";
    public const string DraftCreated = "maintenance.ai.draft.created";
    public const string DraftEdited = "maintenance.ai.draft.edited";
    public const string DraftMarkedStale = "maintenance.ai.draft.marked-stale";
    public const string DraftApproved = "maintenance.ai.draft.approved";
    public const string DraftRejected = "maintenance.ai.draft.rejected";
    public const string RequestCreationAttempted = "maintenance.ai.draft.request-creation-attempted";
    public const string RequestCreated = "maintenance.ai.draft.request-created";
    public const string RequestCreationFailed = "maintenance.ai.draft.request-creation-failed";
    public const string DuplicateApprovalPrevented = "maintenance.ai.draft.duplicate-approval-prevented";
}

public sealed class MaintenanceAiActionDraft
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string ConditionEvaluationReference { get; set; } = string.Empty;
    public Guid RiskAssessmentId { get; set; }
    public string InputFingerprint { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = string.Empty;
    public string ProviderIdentifier { get; set; } = string.Empty;
    public string ModelIdentifier { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string InputSnapshotJson { get; set; } = "{}";
    public string? RawProviderResponseJson { get; set; }
    public string ValidatedOutputJson { get; set; } = "{}";
    public string AiOriginalOutputJson { get; set; } = "{}";
    public string QualitativeConfidence { get; set; } = MaintenanceAiQualitativeConfidenceLevels.Low;
    public string Status { get; set; } = MaintenanceAiDraftStatuses.Draft;
    public uint Version { get; set; }
    public string SuggestedTitle { get; set; } = string.Empty;
    public string SuggestedDescription { get; set; } = string.Empty;
    public string SuggestedPriority { get; set; } = string.Empty;
    public string SuggestedRequestType { get; set; } = string.Empty;
    public string? HumanEditedTitle { get; set; }
    public string? HumanEditedDescription { get; set; }
    public string? HumanEditedPriority { get; set; }
    public string? HumanEditedRequestType { get; set; }
    public string? HumanEditorSubjectId { get; set; }
    public DateTimeOffset? HumanEditedAtUtc { get; set; }
    public string GeneratedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public DateTimeOffset? LastReviewedAtUtc { get; set; }
    public string? ApprovedBySubjectId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string? RejectedBySubjectId { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? CreatedMaintenanceRequestId { get; set; }
    public DateTimeOffset? ExecutionResultAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MaintenanceAiDraftAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DraftId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorSubjectId { get; set; } = string.Empty;
    public uint DraftVersion { get; set; }
    public Guid CorrelationId { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }
}
