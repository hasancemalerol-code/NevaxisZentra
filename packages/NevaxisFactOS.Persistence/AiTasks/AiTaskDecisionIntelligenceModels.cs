namespace NevaxisFactOS.Persistence.AiTasks;

public sealed record AiTaskDecisionInputSnapshot(
    Guid TaskId,
    string TaskReference,
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string Status,
    string DetectedIssue,
    string Reasoning,
    string? SourceModule,
    string? SourceEntityId,
    string? MachineId,
    string? ProductionOrderId,
    string? MoldId,
    string? MaterialId,
    string? SalesOrderId,
    string SlaState,
    int EscalationLevel,
    string EscalationState,
    int EvidenceCount,
    int MinimumEvidenceRequired,
    bool ClosureApprovalRequired,
    bool FourEyesRequired,
    string ClosureState,
    string? ResolutionSummary,
    Guid? AssignedTeamChannelId,
    Guid? AssignedMembershipId,
    Guid? AutomationRuleId,
    string? AutomationMetadataJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record AiTaskDecisionRelatedCandidateSnapshot(
    Guid TaskId,
    string TaskReference,
    string Title,
    string Status,
    string Priority,
    string? SourceModule,
    string? SourceEntityId,
    string? MachineId,
    string? ProductionOrderId,
    DateTimeOffset CreatedAtUtc);

public sealed record AiTaskDecisionTriageResult(
    string? SuggestedCategory,
    string? SuggestedTaskType,
    string? SuggestedPriority,
    string? SuggestedSeverity,
    string? SuggestedDepartment,
    Guid? SuggestedTeamChannelId,
    string? BusinessImpact,
    string? UrgencyReason,
    string OperationalImpactBand,
    string UrgencyBand,
    string SlaBreachRiskBand,
    string? EstimatedRiskReason,
    IReadOnlyList<string> ContributingFactors,
    string? RecommendedIntervention,
    string Explanation,
    string ConfidenceBand,
    bool InsufficientData,
    IReadOnlyList<AiTaskDecisionNextActionResult> NextActions,
    IReadOnlyList<AiTaskDecisionEvidenceRecommendationResult> EvidenceRecommendations,
    IReadOnlyList<AiTaskDecisionRelatedCandidateResult> RelatedCandidates,
    AiTaskDecisionPolicyComparisonResult? PolicyComparison);

public sealed record AiTaskDecisionNextActionResult(
    string ActionType,
    string Title,
    string Rationale,
    string ConfidenceBand,
    string UrgencyBand,
    string? RelatedEntityType,
    string? RelatedEntityId,
    string PayloadJson);

public sealed record AiTaskDecisionEvidenceRecommendationResult(
    string EvidenceType,
    string Title,
    string Rationale,
    bool PolicyMandatory,
    string ConfidenceBand);

public sealed record AiTaskDecisionRelatedCandidateResult(
    Guid CandidateTaskId,
    string RelationshipType,
    string ConfidenceBand,
    string Explanation);

public sealed record AiTaskDecisionPolicyComparisonResult(
    string PolicySummary,
    IReadOnlyList<string> PolicyActions,
    string AiSummary,
    IReadOnlyList<string> AiDifferences);

public sealed record AiTaskDecisionClosureReviewResult(
    string ClosureReadiness,
    IReadOnlyList<string> Concerns,
    IReadOnlyList<string> Recommendations,
    string Explanation,
    string ConfidenceBand,
    bool InsufficientData);

public sealed record AiTaskDecisionProviderRequest(
    AiTaskDecisionInputSnapshot Task,
    IReadOnlyList<AiTaskDecisionRelatedCandidateSnapshot> RelatedTaskCandidates,
    string AnalysisType,
    string PromptTemplateVersion,
    string? TriggerReason);

public sealed record AiTaskDecisionProviderResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureReason,
    AiTaskDecisionTriageResult? Triage,
    AiTaskDecisionClosureReviewResult? ClosureReview,
    string RawResponseJson,
    string ProviderName,
    string ModelName);

public sealed record AiTaskDecisionAnalysisReadModel(
    Guid Id,
    Guid TaskId,
    string AnalysisType,
    string ProviderName,
    string ModelName,
    string PromptTemplateVersion,
    string ConfidenceBand,
    AiTaskDecisionTriageResult? Triage,
    AiTaskDecisionClosureReviewResult? ClosureReview,
    bool Superseded,
    Guid? SupersededByAnalysisId,
    DateTimeOffset CreatedAtUtc,
    string CreatedBy,
    IReadOnlyList<AiTaskDecisionRecommendationReadModel> Recommendations);

public sealed record AiTaskDecisionRecommendationReadModel(
    Guid Id,
    Guid AnalysisId,
    string ActionType,
    string Title,
    string Rationale,
    string ConfidenceBand,
    string UrgencyBand,
    string? RelatedEntityType,
    string? RelatedEntityId,
    string PayloadJson,
    string Status,
    string? FeedbackDecision,
    string? FeedbackReason,
    DateTimeOffset? FeedbackAtUtc,
    string? AppliedResultJson,
    DateTimeOffset CreatedAtUtc);

public sealed record AiTaskDecisionRelationshipReadModel(
    Guid Id,
    Guid SourceTaskId,
    Guid TargetTaskId,
    string TargetTaskTitle,
    string TargetTaskStatus,
    string TargetTaskPriority,
    string? TargetSourceModule,
    string RelationshipType,
    string Status,
    string ConfidenceBand,
    string? Explanation,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record AiTaskDecisionWorkspaceReadModel(
    AiTaskDecisionAnalysisReadModel? LatestAnalysis,
    IReadOnlyList<AiTaskDecisionAnalysisReadModel> History,
    IReadOnlyList<AiTaskDecisionRelationshipReadModel> Relationships,
    IReadOnlyList<AiTaskDecisionRelatedCandidateResult> SuggestedRelated,
    bool ProviderAvailable,
    string? ProviderStatusMessage);

public sealed record AnalyzeAiTaskDecisionCommand(
    Guid TaskId,
    string AnalysisType,
    string IdempotencyKey,
    string? TriggerReason);

public sealed record AcceptAiTaskRecommendationCommand(
    Guid TaskId,
    Guid RecommendationId,
    uint ExpectedTaskVersion,
    string? Reason,
    string? EditedPayloadJson);

public sealed record RejectAiTaskRecommendationCommand(
    Guid TaskId,
    Guid RecommendationId,
    string Reason);

public sealed record ConfirmAiTaskRelationshipCommand(
    Guid TaskId,
    Guid TargetTaskId,
    string RelationshipType,
    string Status,
    string? Explanation);

public sealed record AiTaskDecisionWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? TaskVersion { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public AiTaskDecisionAnalysisReadModel? Analysis { get; init; }

    public static AiTaskDecisionWriteResult Success(
        Guid entityId,
        uint? taskVersion = null,
        bool idempotentReplay = false,
        AiTaskDecisionAnalysisReadModel? analysis = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            TaskVersion = taskVersion,
            IdempotentReplay = idempotentReplay,
            Analysis = analysis,
        };

    public static AiTaskDecisionWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
