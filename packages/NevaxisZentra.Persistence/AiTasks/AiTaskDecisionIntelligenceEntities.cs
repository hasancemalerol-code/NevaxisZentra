namespace NevaxisZentra.Persistence.AiTasks;

public sealed class AiTaskAiAnalysis
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid TaskId { get; set; }
    public string AnalysisType { get; set; } = AiTaskDecisionAnalysisTypes.Triage;
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string PromptTemplateVersion { get; set; } = string.Empty;
    public string ConfidenceBand { get; set; } = AiTaskDecisionConfidenceBands.Medium;
    public string ResultJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public bool Superseded { get; set; }
    public Guid? SupersededByAnalysisId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class AiTaskAiRecommendation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid TaskId { get; set; }
    public Guid AnalysisId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string ConfidenceBand { get; set; } = AiTaskDecisionConfidenceBands.Medium;
    public string UrgencyBand { get; set; } = AiTaskDecisionUrgencyBands.Medium;
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = AiTaskDecisionRecommendationStatuses.Pending;
    public string? FeedbackDecision { get; set; }
    public string? FeedbackReason { get; set; }
    public Guid? FeedbackByMembershipId { get; set; }
    public DateTimeOffset? FeedbackAtUtc { get; set; }
    public string? AppliedResultJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AiTaskRelationship
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid SourceTaskId { get; set; }
    public Guid TargetTaskId { get; set; }
    public string RelationshipType { get; set; } = AiTaskDecisionRelationshipTypes.Related;
    public string Status { get; set; } = AiTaskDecisionRelationshipStatuses.Suggested;
    public string ConfidenceBand { get; set; } = AiTaskDecisionConfidenceBands.Medium;
    public string? Explanation { get; set; }
    public Guid? AnalysisId { get; set; }
    public Guid? ConfirmedByMembershipId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
