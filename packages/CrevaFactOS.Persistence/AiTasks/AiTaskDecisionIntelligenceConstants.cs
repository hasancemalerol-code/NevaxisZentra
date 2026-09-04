namespace CrevaFactOS.Persistence.AiTasks;

public static class AiTaskDecisionIntelligenceProviders
{
    public const string DeterministicDevelopment = "deterministic-development";
    public const string DeterministicDevelopmentModel = "decision-intelligence-1.0";
    public const string PromptTemplateVersion = "ai-task-decision-intelligence-1";
}

public static class AiTaskDecisionIntelligenceFailureCodes
{
    public const string ProviderUnavailable = "ai-task.ai.provider-unavailable";
    public const string ProviderFailed = "ai-task.ai.provider-failed";
    public const string InsufficientData = "ai-task.ai.insufficient-data";
    public const string ValidationFailed = "ai-task.ai.validation-failed";
}

public static class AiTaskDecisionConfidenceBands
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
    public const string InsufficientData = "insufficient-data";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        High, Medium, Low, InsufficientData,
    };
}

public static class AiTaskDecisionImpactBands
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
    public const string InsufficientData = "insufficient-data";
}

public static class AiTaskDecisionUrgencyBands
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
    public const string InsufficientData = "insufficient-data";
}

public static class AiTaskDecisionSlaRiskBands
{
    public const string Low = "low";
    public const string Watch = "watch";
    public const string High = "high";
    public const string Critical = "critical";
    public const string InsufficientData = "insufficient-data";
}

public static class AiTaskDecisionAnalysisTypes
{
    public const string Triage = "triage";
    public const string ClosureReview = "closure-review";
    public const string RelatedTasks = "related-tasks";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Triage, ClosureReview, RelatedTasks,
    };
}

public static class AiTaskDecisionRecommendationStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Superseded = "superseded";
}

public static class AiTaskDecisionRecommendationActionTypes
{
    public const string AssignTeam = "assign-team";
    public const string SetPriority = "set-priority";
    public const string RequestEvidence = "request-evidence";
    public const string Escalate = "escalate";
    public const string RequestClosureApproval = "request-closure-approval";
    public const string ReviewProductionOrder = "review-production-order";
    public const string StartQualityReview = "start-quality-review";
    public const string NotifyTeam = "notify-team";
    public const string StartPlaybook = "start-playbook";
}

public static class AiTaskDecisionRelationshipTypes
{
    public const string PossibleDuplicate = "possible-duplicate";
    public const string Related = "related";
    public const string SameIncident = "same-incident";
    public const string SameSource = "same-source";
    public const string FollowUp = "follow-up";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        PossibleDuplicate, Related, SameIncident, SameSource, FollowUp,
    };
}

public static class AiTaskDecisionRelationshipStatuses
{
    public const string Suggested = "suggested";
    public const string Confirmed = "confirmed";
    public const string Rejected = "rejected";
    public const string MarkedDuplicate = "marked-duplicate";
}

public static class AiTaskDecisionClosureReadiness
{
    public const string Ready = "ready";
    public const string ReviewNeeded = "review-needed";
    public const string RiskDetected = "risk-detected";
    public const string InsufficientData = "insufficient-data";
}

public static class AiTaskDecisionFeedbackDecisions
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string AcceptedWithEdit = "accepted-with-edit";
}
