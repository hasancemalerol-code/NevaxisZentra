namespace NevaxisFactOS.Persistence.ThroughputIntelligence;

public static class ThroughputIntelligencePermissionKeys
{
    public const string Read = "throughput-intelligence.read";
    public const string Manage = "throughput-intelligence.manage";
    public const string Simulate = "throughput-simulation.run";
}

public static class ThroughputIntelligenceFailureCodes
{
    public const string NotFound = "throughput-intelligence.not-found";
    public const string ValidationFailed = "throughput-intelligence.validation-failed";
    public const string InsufficientData = "throughput-intelligence.insufficient-data";
    public const string NotEligible = "throughput-intelligence.not-eligible";
    public const string RequiresApproval = "throughput-intelligence.requires-controlled-approval";
}

public static class ThroughputIntelligenceDataQualityStates
{
    public const string Good = "GOOD";
    public const string Partial = "PARTIAL";
    public const string Stale = "STALE";
    public const string Insufficient = "INSUFFICIENT";
    public const string Conflicting = "CONFLICTING";
}

public static class ThroughputIntelligenceConstraintRoles
{
    public const string Current = "CURRENT";
    public const string Secondary = "SECONDARY";
    public const string Emerging = "EMERGING";
}

public static class ThroughputIntelligenceConstraintTypes
{
    public const string Capacity = "CAPACITY";
    public const string CycleTime = "CYCLE_TIME";
    public const string Starvation = "STARVATION";
    public const string Blocking = "BLOCKING";
    public const string OperatorWait = "OPERATOR_WAIT";
    public const string MaterialWait = "MATERIAL_WAIT";
    public const string QualityYield = "QUALITY_YIELD";
    public const string Microstop = "MICROSTOP";
    public const string Changeover = "CHANGEOVER";
}

public static class ThroughputIntelligenceLossCategories
{
    public const string Stop = "STOP_LOSS";
    public const string Fault = "FAULT_LOSS";
    public const string Starved = "STARVED_LOSS";
    public const string Blocked = "BLOCKED_LOSS";
    public const string Microstop = "MICROSTOP_LOSS";
    public const string CycleVariation = "CYCLE_VARIATION_LOSS";
    public const string Changeover = "CHANGEOVER_LOSS";
    public const string MaterialWait = "MATERIAL_WAIT_LOSS";
    public const string OperatorWait = "OPERATOR_WAIT_LOSS";
    public const string QualityReject = "QUALITY_REJECT_LOSS";
    public const string Rework = "REWORK_LOSS";
}

public static class ThroughputIntelligenceScenarioTypes
{
    public const string AddOperator = "ADD_OPERATOR";
    public const string MoveOperator = "MOVE_OPERATOR";
    public const string ChangeBufferCapacity = "CHANGE_BUFFER_CAPACITY";
    public const string ChangeCycleTime = "CHANGE_CYCLE_TIME";
    public const string RedistributeOperation = "REDISTRIBUTE_OPERATION";
    public const string ReassignResource = "REASSIGN_RESOURCE";
    public const string CombinedIntervention = "COMBINED_INTERVENTION";
}

public static class ThroughputIntelligenceScenarioEligibility
{
    public const string Eligible = "ELIGIBLE";
    public const string NotEligible = "NOT_ELIGIBLE";
    public const string RequiresControlledApproval = "REQUIRES_CONTROLLED_APPROVAL";
}

public static class ThroughputIntelligenceAttributionQuality
{
    public const string Measured = "MEASURED";
    public const string Estimated = "ESTIMATED";
    public const string Partial = "PARTIAL";
    public const string InsufficientData = "INSUFFICIENT_DATA";
}

public static class ThroughputIntelligenceEngineVersions
{
    public const string Measurement = "throughput-measurement-v1";
    public const string Takt = "throughput-takt-v1";
    public const string CycleTime = "throughput-cycle-time-v1";
    public const string Capacity = "throughput-capacity-v1";
    public const string Constraint = "throughput-constraint-v1";
    public const string LossTree = "throughput-loss-tree-v1";
    public const string Scenario = "throughput-scenario-v1";
}

public static class ThroughputIntelligenceAnalysisWindows
{
    public const string Current = "current";
    public const string Recent = "recent";
    public const string Shift = "shift";
    public const string Order = "order";
    public const string Explicit = "explicit";
}

public static class ThroughputIntelligenceFixtureCodes
{
    public const string AssemblyStationCode = "ST-30";
    public const string WeldingStationCode = "ST-40";
    public const string BufferCode = "BUF-20";
    public const string OperatorAssembly = "OP-ASM-01";
    public const string OperatorAssembly2 = "OP-ASM-02";
    public const string OperatorPackaging = "OP-PKG-01";
}
