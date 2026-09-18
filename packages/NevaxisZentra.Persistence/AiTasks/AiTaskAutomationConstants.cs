namespace NevaxisZentra.Persistence.AiTasks;

public static class AiTaskAutomationTriggers
{
    public const string TaskCreated = "task-created";
    public const string TaskSourceLinked = "task-source-linked";
    public const string TaskPriorityChanged = "task-priority-changed";
    public const string TaskEscalated = "task-escalated";
    public const string TaskReopened = "task-reopened";
    public const string TaskClosureRequested = "task-closure-requested";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        TaskCreated, TaskSourceLinked, TaskPriorityChanged, TaskEscalated, TaskReopened, TaskClosureRequested,
    };
}

public static class AiTaskAutomationConditionFields
{
    public const string SourceModule = "sourceModule";
    public const string TaskCategory = "taskCategory";
    public const string TaskType = "taskType";
    public const string Facility = "facility";
    public const string Department = "department";
    public const string Priority = "priority";
    public const string Severity = "severity";
    public const string EscalationLevel = "escalationLevel";
    public const string RequiresEvidence = "requiresEvidence";
    public const string ApprovalRequired = "approvalRequired";
    public const string SourceEntityType = "sourceEntityType";
    public const string OriginType = "originType";
    public const string CurrentAssignmentState = "currentAssignmentState";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SourceModule, TaskCategory, TaskType, Facility, Department, Priority, Severity, EscalationLevel,
        RequiresEvidence, ApprovalRequired, SourceEntityType, OriginType, CurrentAssignmentState,
    };
}

public static class AiTaskAutomationConditionOperators
{
    public const string Equal = "equals";
    public const string NotEqual = "not-equals";
    public const string In = "in";
    public const string NotIn = "not-in";
    public const string Gte = "gte";
    public const string Lte = "lte";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Equal, NotEqual, In, NotIn, Gte, Lte,
    };
}

public static class AiTaskAutomationActionTypes
{
    public const string AssignToMembership = "assignToMembership";
    public const string AssignToTeam = "assignToTeam";
    public const string RouteToQueue = "routeToQueue";
    public const string SetPriority = "setPriority";
    public const string ApplySlaPolicy = "applySlaPolicy";
    public const string ApplyEscalationPolicy = "applyEscalationPolicy";
    public const string RequireEvidence = "requireEvidence";
    public const string RequireClosureApproval = "requireClosureApproval";
    public const string EnableFourEyes = "enableFourEyes";
    public const string NotifyTeamsChannel = "notifyTeamsChannel";
    public const string AddTaskTag = "addTaskTag";
    public const string SetDueDatePolicy = "setDueDatePolicy";
    public const string ApplyClosurePolicy = "applyClosurePolicy";
    public const string StartPlaybook = "startPlaybook";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AssignToMembership, AssignToTeam, RouteToQueue, SetPriority, ApplySlaPolicy, ApplyEscalationPolicy,
        RequireEvidence, RequireClosureApproval, EnableFourEyes, NotifyTeamsChannel, AddTaskTag, SetDueDatePolicy,
        ApplyClosurePolicy, StartPlaybook,
    };
}

public static class AiTaskAutomationOverrideScopes
{
    public const string Assignment = "assignment";
    public const string Priority = "priority";
    public const string DueDate = "dueDate";
    public const string SlaPolicy = "slaPolicy";
    public const string EscalationPolicy = "escalationPolicy";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Assignment, Priority, DueDate, SlaPolicy, EscalationPolicy,
    };
}

public static class AiTaskAutomationAssignmentStates
{
    public const string Unassigned = "unassigned";
    public const string Membership = "membership";
    public const string Team = "team";
}
