namespace NevaxisFactOS.Persistence.MixedProduction;

public static class MixedProductionPermissionKeys
{
    public const string Read = "mixed-production.read";
    public const string Manage = "mixed-production.manage";
    public const string Operate = "mixed-production.operate";
}

public static class MixedProductionResourceTypes
{
    public const string Machine = "machine";
    public const string Human = "human";
    public const string Robot = "robot";
    public const string Tool = "tool";
    public const string Conveyor = "conveyor";
    public const string Buffer = "buffer";
    public const string QualityStation = "quality_station";
}

public static class MixedProductionStationKinds
{
    public const string Process = "process";
    public const string Buffer = "buffer";
    public const string Quality = "quality";
    public const string Packaging = "packaging";
}

public static class MixedProductionEventTypes
{
    public const string Run = "RUN";
    public const string Idle = "IDLE";
    public const string Stop = "STOP";
    public const string Fault = "FAULT";
    public const string Starved = "STARVED";
    public const string Blocked = "BLOCKED";
    public const string CycleStart = "CYCLE_START";
    public const string CycleEnd = "CYCLE_END";
    public const string Good = "GOOD";
    public const string Reject = "REJECT";
    public const string Rework = "REWORK";
    public const string MaterialWait = "MATERIAL_WAIT";
    public const string OperatorWait = "OPERATOR_WAIT";
    public const string OperatorStart = "OPERATOR_START";
    public const string OperatorComplete = "OPERATOR_COMPLETE";
    public const string BufferChange = "BUFFER_CHANGE";
    public const string QualityResult = "QUALITY_RESULT";
}

public static class MixedProductionLineStates
{
    public const string Running = "RUNNING";
    public const string Starved = "STARVED";
    public const string Blocked = "BLOCKED";
    public const string Stopped = "STOPPED";
    public const string Faulted = "FAULTED";
    public const string MixedDegraded = "MIXED_DEGRADED";
}

public static class MixedProductionFixtureCodes
{
    public const string LineCode = "MIX-LINE-01";
    public const string AreaCode = "MIX-AREA-01";
    public const string ProductCode = "MIX-PROD-01";
    public const string RoutingRevision = "A";
}

public static class MixedProductionFailureCodes
{
    public const string NotFound = "mixed-production.not-found";
    public const string ValidationFailed = "mixed-production.validation-failed";
    public const string DuplicateEvent = "mixed-production.duplicate-event";
    public const string Conflict = "mixed-production.conflict";
}
