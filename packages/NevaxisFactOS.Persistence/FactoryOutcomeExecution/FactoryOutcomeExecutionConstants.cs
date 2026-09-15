namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public static class FactoryOutcomeExecutionConstants
{
    public const string SourceModule = "factory-outcome-execution";
    public const string ScenarioOrderRecovery2 = "order-recovery-2";
    public const string OrderReference = "ORD-8742";
    public const string MachineCode = "INJ-004";
    public const int RequiredQuantity = 1200;
    public const int InitialProjectedQuantity = 980;
    public const double CycleTimeReferenceSec = 27.5;
    public const double CycleTimeActualSec = 32.1;
    public const int MicroStopCount = 14;
    public const double MaterialCoverageHours = 5.8;
}

public static class FactoryOutcomeExecutionActionStatuses
{
    public const string Pending = "pending";
    public const string Executing = "executing";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Blocked = "blocked";
    public const string WaitingApproval = "waiting-approval";
}

public static class FactoryOutcomeExecutionPlanKeys
{
    public const string Primary = "primary";
    public const string Alternative = "alternative";
}
