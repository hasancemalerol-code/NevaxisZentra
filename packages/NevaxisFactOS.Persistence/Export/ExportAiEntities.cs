using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportAiContextSnapshot : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? ExportCaseId { get; set; }
    public Guid? CountryResearchCaseId { get; set; }
    public int SnapshotVersion { get; set; }
    public string ContextHash { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
}

public sealed class ExportAiAnalysisRun : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? ExportCaseId { get; set; }
    public Guid? CountryResearchCaseId { get; set; }
    public Guid ContextSnapshotId { get; set; }
    public ExportAiAnalysisRunType RunType { get; set; }
    public ExportAiAnalysisRunStatus Status { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string RequestedBySubjectId { get; set; } = string.Empty;
    public int InputUsageUnits { get; set; }
    public int OutputUsageUnits { get; set; }
    public decimal AiCreditsConsumed { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class ExportAiFinding : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AnalysisRunId { get; set; }
    public ExportAiFindingCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public ExportAiFindingSeverity Severity { get; set; }
    public decimal Confidence { get; set; }
    public ExportAiSourceType SourceType { get; set; }
    public string SourceReferencesJson { get; set; } = "[]";
    public Guid? RelatedRequirementId { get; set; }
    public Guid? RelatedDocumentId { get; set; }
    public Guid? RelatedShipmentId { get; set; }
    public bool Actionable { get; set; }
    public bool HumanReviewRequired { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ExportAiRecommendation : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AnalysisRunId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ExportAiRecommendationPriority Priority { get; set; }
    public decimal Confidence { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public ExportAiProposedActionType? ProposedActionType { get; set; }
    public string? ProposedActionPayloadJson { get; set; }
    public ExportAiRecommendationStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewedBySubjectId { get; set; }
    public ExportAiReviewDecision? ReviewDecision { get; set; }
    public string? ReviewNote { get; set; }
    public uint Version { get; set; }
}
