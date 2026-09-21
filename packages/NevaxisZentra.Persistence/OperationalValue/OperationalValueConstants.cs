namespace NevaxisZentra.Persistence.OperationalValue;

public static class OperationalValuePermissionKeys
{
    public const string Read = "operational-value.read";
    public const string ManageEconomicInputs = "operational-value.manage-economic-inputs";
    public const string Export = "operational-value.export";
}

public static class OperationalValueCategories
{
    public const string GoodOutputGain = "GOOD_OUTPUT_GAIN";
    public const string ThroughputGain = "THROUGHPUT_GAIN";
    public const string CapacityRecovered = "CAPACITY_RECOVERED";
    public const string DowntimeAvoided = "DOWNTIME_AVOIDED";
    public const string LateOrderRiskReduced = "LATE_ORDER_RISK_REDUCED";
    public const string OtifProtected = "OTIF_PROTECTED";
    public const string ScrapReduced = "SCRAP_REDUCED";
    public const string ReworkReduced = "REWORK_REDUCED";
    public const string ChangeoverReduced = "CHANGEOVER_REDUCED";
    public const string WipReduced = "WIP_REDUCED";
    public const string MaterialWaitReduced = "MATERIAL_WAIT_REDUCED";
    public const string LaborTouchReduced = "LABOR_TOUCH_REDUCED";
    public const string HumanTouchesReduced = "HUMAN_TOUCHES_REDUCED";
    public const string EnergyReduction = "ENERGY_REDUCTION";
    public const string IntelligenceCostReduced = "INTELLIGENCE_COST_REDUCED";
}

public static class OperationalValueStatuses
{
    public const string Predicted = "PREDICTED";
    public const string Observing = "OBSERVING";
    public const string VerifiedOperational = "VERIFIED_OPERATIONAL";
    public const string VerifiedEconomic = "VERIFIED_ECONOMIC";
    public const string Invalidated = "INVALIDATED";
    public const string Rejected = "REJECTED";
}

public static class OperationalValueAttributionClasses
{
    public const string Direct = "DIRECT";
    public const string StronglyAttributed = "STRONGLY_ATTRIBUTED";
    public const string PartiallyAttributed = "PARTIALLY_ATTRIBUTED";
    public const string CorrelatedOnly = "CORRELATED_ONLY";
    public const string Unattributed = "UNATTRIBUTED";
}

public static class OperationalValueBaselineMethods
{
    public const string BeforeInterventionWindow = "BEFORE_INTERVENTION_WINDOW";
    public const string PublishedPlan = "PUBLISHED_PLAN";
    public const string HistoricalComparable = "HISTORICAL_COMPARABLE";
    public const string ScenarioBaseline = "SCENARIO_BASELINE";
    public const string VerifiedTarget = "VERIFIED_TARGET";
}

public static class OperationalValueEconomicConversionStatuses
{
    public const string NotConfigured = "NOT_CONFIGURED";
    public const string OperationalOnly = "OPERATIONAL_ONLY";
    public const string Converted = "CONVERTED";
    public const string InsufficientInputs = "INSUFFICIENT_INPUTS";
}

public static class OperationalValueFormulaTypes
{
    public const string ThroughputGoodUnits = "throughput-good-units-v1";
    public const string DowntimeMinutes = "downtime-minutes-v1";
    public const string ScrapUnits = "scrap-units-v1";
    public const string ChangeoverMinutes = "changeover-minutes-v1";
    public const string HumanTouches = "human-touches-v1";
    public const string RoiNetValue = "roi-net-value-v1";
}

public static class OperationalValueFailureStates
{
    public const string UnverifiedOutcome = "UNVERIFIED_OUTCOME";
    public const string InvalidBaseline = "INVALID_BASELINE";
    public const string InvalidActual = "INVALID_ACTUAL";
    public const string DoubleCountBlocked = "DOUBLE_COUNT_BLOCKED";
    public const string MissingFinancialInput = "MISSING_FINANCIAL_INPUT";
    public const string RoiUnavailable = "ROI_UNAVAILABLE";
    public const string PaybackUnavailable = "PAYBACK_UNAVAILABLE";
}

public static class OperationalValueEngineVersions
{
    public const string Controller = "operational-value-controller-v1";
    public const string Engine = "operational-value-engine-v1";
    public const string DoubleCountGuard = "operational-value-double-count-v1";
}

public static class OperationalValueOverlapGroups
{
    public const string ThroughputCapacity = "throughput-capacity";
    public const string ChangeoverThroughput = "changeover-throughput";
}
