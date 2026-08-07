namespace CrevaFactOS.Persistence.Audit;

public static class AuditActions
{
    public const string Authenticate = "authenticate";
    public const string AccessTenant = "access.tenant";
    public const string Authorize = "authorize";
    public const string WriteAudit = "write.audit";
    public const string ValidateConfiguration = "validate.configuration";
    public const string AppendAiDecision = "ai.decision.append";
    public const string ProductionPlanDraftCreate = "production.plan.draft.create";
    public const string ProductionPlanDraftUpdate = "production.plan.draft.update";
    public const string ProductionPlanSubmitForReview = "production.plan.workflow.submit-for-review";
    public const string ProductionPlanRequestRevision = "production.plan.workflow.request-revision";
    public const string ProductionPlanApprove = "production.plan.workflow.approve";
    public const string ProductionPlanReject = "production.plan.workflow.reject";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Authenticate,
        AccessTenant,
        Authorize,
        WriteAudit,
        ValidateConfiguration,
        AppendAiDecision,
        ProductionPlanDraftCreate,
        ProductionPlanDraftUpdate,
        ProductionPlanSubmitForReview,
        ProductionPlanRequestRevision,
        ProductionPlanApprove,
        ProductionPlanReject,
    };
}
