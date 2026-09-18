namespace NevaxisZentra.Persistence.AiTasks;

public static class AiTaskConstants
{
    public const string RuleSetVersion = "AI-TASK-CENTER-1.0";
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development AI task data — simulated, non-production, not real operational decisions.";
}

public static class AiTaskFailureCodes
{
    public const string ValidationFailed = "ai-task.validation-failed";
    public const string NotFound = "ai-task.not-found";
    public const string ConcurrencyConflict = "ai-task.concurrency-conflict";
    public const string IdempotencyConflict = "ai-task.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "ai-task.idempotency-legacy-unverifiable";
    public const string InvalidTransition = "ai-task.invalid-transition";
    public const string FinalStatus = "ai-task.final-status";
    public const string FacilityScopeDenied = "ai-task.facility-scope-denied";
    public const string Unavailable = "ai-task.unavailable";
    public const string ClosureValidationFailed = "ai-task.closure-validation-failed";
    public const string FourEyesViolation = "ai-task.four-eyes-violation";
    public const string ClosureStateConflict = "ai-task.closure-state-conflict";
}

public static class AiTaskStatuses
{
    public const string Draft = "draft";
    public const string AwaitingReview = "awaiting-review";
    public const string AwaitingApproval = "awaiting-approval";
    public const string Approved = "approved";
    public const string InProgress = "in-progress";
    public const string Completed = "completed";
    public const string Rejected = "rejected";
    public const string Deferred = "deferred";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft, AwaitingReview, AwaitingApproval, Approved, InProgress, Completed,
        Rejected, Deferred, Failed, Cancelled,
    };

    public static readonly IReadOnlySet<string> Final = new HashSet<string>(StringComparer.Ordinal)
    {
        Completed, Rejected, Failed, Cancelled,
    };
}

public static class AiTaskCategories
{
    public const string ProductionPlanning = "production-planning";
    public const string LiveProduction = "live-production";
    public const string Maintenance = "maintenance";
    public const string Quality = "quality";
    public const string MaterialRisk = "material-risk";
    public const string SupplyRisk = "supply-risk";
    public const string CommercialIntelligence = "commercial-intelligence";
    public const string Energy = "energy";
    public const string Capacity = "capacity";
    public const string DeliveryRisk = "delivery-risk";
    public const string CostDeviation = "cost-deviation";
    public const string ErpIntegration = "erp-integration";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ProductionPlanning, LiveProduction, Maintenance, Quality, MaterialRisk, SupplyRisk,
        CommercialIntelligence, Energy, Capacity, DeliveryRisk, CostDeviation, ErpIntegration,
    };
}

public static class AiTaskPriorities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Low, Medium, High, Critical,
    };
}

public static class AiTaskRiskLevels
{
    public const string Information = "information";
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Information, Low, Medium, High, Critical,
    };
}

public static class AiTaskApprovalStates
{
    public const string NotRequired = "not-required";
    public const string Required = "required";
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        NotRequired, Required, Pending, Approved, Rejected,
    };
}

public static class AiTaskActionTypes
{
    public const string Review = "review";
    public const string CreateDraft = "create-draft";
    public const string UpdatePlan = "update-plan";
    public const string AssignUser = "assign-user";
    public const string CreateMaintenanceRequest = "create-maintenance-request";
    public const string CreateQualityRecord = "create-quality-record";
    public const string CreatePurchaseRequest = "create-purchase-request";
    public const string CreateTransferRequest = "create-transfer-request";
    public const string SendNotification = "send-notification";
    public const string SyncToErp = "sync-to-erp";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Review, CreateDraft, UpdatePlan, AssignUser, CreateMaintenanceRequest, CreateQualityRecord,
        CreatePurchaseRequest, CreateTransferRequest, SendNotification, SyncToErp,
    };

    private static readonly HashSet<string> HighImpactActions = new(StringComparer.Ordinal)
    {
        UpdatePlan, CreatePurchaseRequest, CreateTransferRequest, SyncToErp,
    };

    public static bool DoesActionRequireApproval(string actionType) =>
        HighImpactActions.Contains(actionType.Trim());
}

public static class AiTaskAuditEventTypes
{
    public const string Created = "created";
    public const string AnalysisUpdated = "analysis-updated";
    public const string Routed = "routed";
    public const string Assigned = "assigned";
    public const string Reassigned = "reassigned";
    public const string Acknowledged = "acknowledged";
    public const string PriorityChanged = "priority-changed";
    public const string DueDateChanged = "due-date-changed";
    public const string SlaWarning = "sla-warning";
    public const string Escalated = "escalated";
    public const string ApprovalRequested = "approval-requested";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Deferred = "deferred";
    public const string ExecutionStarted = "execution-started";
    public const string ExecutionCompleted = "execution-completed";
    public const string ExecutionFailed = "execution-failed";
    public const string Cancelled = "cancelled";
    public const string StatusChanged = "status-changed";
    public const string TeamsNotificationEmitted = "teams-notification-emitted";
    public const string EvidenceSubmitted = "evidence-submitted";
    public const string ResolutionUpdated = "resolution-updated";
    public const string ClosureRequested = "closure-requested";
    public const string ClosureApproved = "closure-approved";
    public const string ClosureRejected = "closure-rejected";
    public const string TaskReopened = "task-reopened";
    public const string AutomationApplied = "automation-applied";
    public const string AutomationOverrideRecorded = "automation-override-recorded";
    public const string AiAnalyzed = "ai-analyzed";
    public const string AiRecommendationAccepted = "ai-recommendation-accepted";
    public const string AiRecommendationRejected = "ai-recommendation-rejected";
    public const string AiRelationshipConfirmed = "ai-relationship-confirmed";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Created, AnalysisUpdated, Routed, Assigned, Reassigned, Acknowledged, PriorityChanged,
        DueDateChanged, SlaWarning, Escalated, ApprovalRequested, Approved, Rejected, Deferred,
        ExecutionStarted, ExecutionCompleted, ExecutionFailed, Cancelled, StatusChanged,
        TeamsNotificationEmitted, EvidenceSubmitted, ResolutionUpdated, ClosureRequested,
        ClosureApproved, ClosureRejected, TaskReopened, AutomationApplied, AutomationOverrideRecorded,
        AiAnalyzed, AiRecommendationAccepted, AiRecommendationRejected, AiRelationshipConfirmed,
    };
}

public static class AiTaskActorTypes
{
    public const string Ai = "ai";
    public const string User = "user";
    public const string System = "system";
}
