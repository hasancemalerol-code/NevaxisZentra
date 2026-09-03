namespace CrevaFactOS.Persistence.AiTasks;

public static class AiTaskEscalationLevels
{
    public const int None = 0;
    public const int First = 1;
    public const int Management = 2;
    public const int Critical = 3;

    public static readonly IReadOnlySet<int> All = new HashSet<int> { None, First, Management, Critical };
}

public static class AiTaskEscalationStates
{
    public const string None = "none";
    public const string Active = "active";
    public const string Resolved = "resolved";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        None, Active, Resolved,
    };
}

public static class AiTaskSlaStates
{
    public const string OnTrack = "on-track";
    public const string DueSoon = "due-soon";
    public const string Overdue = "overdue";
    public const string Breached = "breached";
    public const string CompletedWithinSla = "completed-within-sla";
    public const string CompletedAfterSla = "completed-after-sla";
    public const string NotApplicable = "not-applicable";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        OnTrack, DueSoon, Overdue, Breached, CompletedWithinSla, CompletedAfterSla, NotApplicable,
    };
}

public static class AiTaskAssignmentSources
{
    public const string Manual = "manual";
    public const string Routing = "routing";
    public const string Reassignment = "reassignment";
    public const string Escalation = "escalation";
    public const string Automation = "automation";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Manual, Routing, Reassignment, Escalation, Automation,
    };
}

public static class AiTaskRoutingTargetTypes
{
    public const string Membership = "membership";
    public const string TeamChannel = "team-channel";
    public const string UnassignedQueue = "unassigned-queue";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Membership, TeamChannel, UnassignedQueue,
    };
}

public static class AiTaskEscalationBreachTypes
{
    public const string Acknowledgment = "acknowledgment";
    public const string Response = "response";
    public const string Resolution = "resolution";
    public const string CriticalApproachingDue = "critical-approaching-due";
    public const string Manual = "manual";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Acknowledgment, Response, Resolution, CriticalApproachingDue, Manual,
    };
}

public static class AiTaskSlaPolicyKeys
{
    public const string CriticalDefault = "critical-default";
    public const string HighDefault = "high-default";
    public const string MediumDefault = "medium-default";
    public const string LowDefault = "low-default";
}

public static class AiTaskSlaDefaults
{
    public static readonly IReadOnlyDictionary<string, (int AckMinutes, int ResponseMinutes, int ResolutionMinutes)> ByPriority =
        new Dictionary<string, (int, int, int)>(StringComparer.Ordinal)
        {
            [AiTaskPriorities.Critical] = (15, 30, 120),
            [AiTaskPriorities.High] = (30, 60, 240),
            [AiTaskPriorities.Medium] = (60, 240, 1440),
            [AiTaskPriorities.Low] = (240, 480, 2880),
        };

    public const int DueSoonThresholdMinutes = 60;
}
