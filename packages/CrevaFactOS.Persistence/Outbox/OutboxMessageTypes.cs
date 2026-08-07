namespace CrevaFactOS.Persistence.Outbox;

public static class OutboxMessageTypes
{
    public const string AuditRecorded = "infrastructure.audit.recorded";
    public const string AiDecisionRecorded = "infrastructure.ai-decision.recorded";
    public const string Probe = "infrastructure.outbox.probe";
    public const string ProductionPlanSubmittedForReview = "production-plan-submitted-for-review";
    public const string ProductionPlanRevisionRequested = "production-plan-revision-requested";
    public const string ProductionPlanApproved = "production-plan-approved";
    public const string ProductionPlanRejected = "production-plan-rejected";

    public const int SchemaVersion = 1;

    public static readonly HashSet<string> All = new(
        [
            AuditRecorded,
            AiDecisionRecorded,
            Probe,
            ProductionPlanSubmittedForReview,
            ProductionPlanRevisionRequested,
            ProductionPlanApproved,
            ProductionPlanRejected,
        ],
        StringComparer.Ordinal);
}
