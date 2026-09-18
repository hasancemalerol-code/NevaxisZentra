namespace NevaxisZentra.Persistence.AiTasks;

public static class AiTaskPlaybookCategories
{
    public const string MachineIncident = "machine-incident";
    public const string QualityNonconformance = "quality-nonconformance";
    public const string MaterialShortage = "material-shortage";
    public const string CriticalOrderDelay = "critical-order-delay";
    public const string EnergyAnomaly = "energy-anomaly";
    public const string ShiftHandoverCritical = "shift-handover-critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MachineIncident, QualityNonconformance, MaterialShortage, CriticalOrderDelay, EnergyAnomaly, ShiftHandoverCritical,
    };
}

public static class AiTaskPlaybookStepTypes
{
    public const string Task = "task";
    public const string Approval = "approval";
    public const string Verification = "verification";
    public const string DecisionGate = "decision-gate";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Task, Approval, Verification, DecisionGate,
    };
}

public static class AiTaskPlaybookDependencyTypes
{
    public const string RequiredPredecessor = "required-predecessor";
    public const string OptionalPredecessor = "optional-predecessor";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        RequiredPredecessor, OptionalPredecessor,
    };
}

public static class AiTaskPlaybookRunStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Blocked = "blocked";
    public const string AwaitingApproval = "awaiting-approval";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending, Running, Blocked, AwaitingApproval, Completed, Cancelled, Failed,
    };

    public static readonly IReadOnlySet<string> Active = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending, Running, Blocked, AwaitingApproval,
    };
}

public static class AiTaskPlaybookRunStepStatuses
{
    public const string Pending = "pending";
    public const string Ready = "ready";
    public const string InProgress = "in-progress";
    public const string AwaitingApproval = "awaiting-approval";
    public const string Blocked = "blocked";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
    public const string Skipped = "skipped";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending, Ready, InProgress, AwaitingApproval, Blocked, Completed, Cancelled, Failed, Skipped,
    };
}

public static class AiTaskPlaybookDependencyStates
{
    public const string Ready = "ready";
    public const string Waiting = "waiting";
    public const string Blocked = "blocked";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Ready, Waiting, Blocked,
    };
}

public static class AiTaskPlaybookTriggerSources
{
    public const string Manual = "manual";
    public const string Automation = "automation";
    public const string AiRecommendation = "ai-recommendation";
    public const string ShiftHandover = "shift-handover";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Manual, Automation, AiRecommendation, ShiftHandover,
    };
}

public static class AiTaskPlaybookConditionFields
{
    public const string SparePartRequired = "sparePartRequired";
    public const string QualityImpact = "qualityImpact";
    public const string DowntimeMinutes = "downtimeMinutes";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SparePartRequired, QualityImpact, DowntimeMinutes,
    };
}

public static class AiTaskPlaybookConditionOperators
{
    public const string Equal = "equals";
    public const string Gte = "gte";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Equal, Gte,
    };
}

public static class AiTaskPlaybookAuditEventTypes
{
    public const string PlaybookCreated = "playbook-created";
    public const string PlaybookUpdated = "playbook-updated";
    public const string PlaybookEnabled = "playbook-enabled";
    public const string PlaybookDisabled = "playbook-disabled";
    public const string PlaybookRunStarted = "playbook-run-started";
    public const string PlaybookRunCompleted = "playbook-run-completed";
    public const string PlaybookRunCancelled = "playbook-run-cancelled";
    public const string PlaybookRunBlocked = "playbook-run-blocked";
    public const string PlaybookStepBlocked = "playbook-step-blocked";
    public const string PlaybookStepUnblocked = "playbook-step-unblocked";
    public const string PlaybookStepSkipped = "playbook-step-skipped";
}
