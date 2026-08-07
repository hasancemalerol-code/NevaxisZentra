namespace CrevaFactOS.Persistence.Audit;

public static class AuditEventTypes
{
    public const string AuthenticationSucceeded = "authentication.succeeded";
    public const string AuthenticationFailed = "authentication.failed";
    public const string TenantAccessGranted = "tenant.access.granted";
    public const string TenantAccessDenied = "tenant.access.denied";
    public const string AuthorizationGranted = "authorization.granted";
    public const string AuthorizationDenied = "authorization.denied";
    public const string AuditWriteFailed = "audit.write.failed";
    public const string InfrastructureConfigurationRejected = "infrastructure.configuration.rejected";
    public const string AiDecisionRecorded = "ai.decision.recorded";
    public const string ProductionPlanDraftCreated = "production.plan.draft.created";
    public const string ProductionPlanDraftUpdated = "production.plan.draft.updated";
    public const string ProductionPlanSubmittedForReview = "production.plan.submitted.for.review";
    public const string ProductionPlanRevisionRequested = "production.plan.revision.requested";
    public const string ProductionPlanApproved = "production.plan.approved";
    public const string ProductionPlanRejected = "production.plan.rejected";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AuthenticationSucceeded,
        AuthenticationFailed,
        TenantAccessGranted,
        TenantAccessDenied,
        AuthorizationGranted,
        AuthorizationDenied,
        AuditWriteFailed,
        InfrastructureConfigurationRejected,
        AiDecisionRecorded,
        ProductionPlanDraftCreated,
        ProductionPlanDraftUpdated,
        ProductionPlanSubmittedForReview,
        ProductionPlanRevisionRequested,
        ProductionPlanApproved,
        ProductionPlanRejected,
    };
}
