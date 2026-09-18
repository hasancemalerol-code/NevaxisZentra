namespace NevaxisZentra.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceEngineVersions
{
    public const string RiskEngine = "factory-risk-v1";
    public const string BaselineEngine = "factory-baseline-v1";
    public const string ProjectionEngine = "factory-projection-v1";
    public const string FeatureWindow = "feature-window-v1";
}

public static class FactoryIntelligenceDataClassifications
{
    public const string Observed = "observed";
    public const string Predicted = "predicted";
    public const string Inferred = "inferred";
    public const string Verified = "verified";
}

public static class FactoryIntelligenceDataQualityStates
{
    public const string Good = "good";
    public const string Partial = "partial";
    public const string Stale = "stale";
    public const string Insufficient = "insufficient";
    public const string Conflicting = "conflicting";
}

public static class FactoryIntelligenceConfidenceBands
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
}

public static class FactoryIntelligencePredictionHorizons
{
    public const string Immediate = "immediate";
    public const string ShortTerm = "short";
    public const string Shift = "shift";
    public const string Delivery = "delivery";
}

public static class FactoryIntelligenceRiskStates
{
    public const string Watch = "watch";
    public const string Warning = "warning";
    public const string Critical = "critical";
    public const string Resolved = "resolved";
    public const string Averted = "averted";
}

public static class FactoryIntelligenceRiskTypes
{
    public const string ProductionTargetMiss = "production-target-miss";
    public const string CriticalOrderDelivery = "critical-order-delivery";
    public const string MachineDegradation = "machine-degradation";
    public const string MaterialShortage = "material-shortage";
    public const string QualityDeterioration = "quality-deterioration";
    public const string MoldLifecycle = "mold-lifecycle";
    public const string MaintenanceReliability = "maintenance-reliability";
    public const string CompoundDelivery = "compound-delivery";
}

public static class FactoryIntelligenceSignalTypes
{
    public const string CycleTimeDrift = "cycle-time-drift";
    public const string MicroStopFrequency = "micro-stop-frequency";
    public const string DowntimeFrequency = "downtime-frequency";
    public const string ThroughputTrend = "throughput-trend";
    public const string MaterialCoverageHours = "material-coverage-hours";
    public const string MaterialConsumptionRate = "material-consumption-rate";
    public const string ScrapRejectTrend = "scrap-reject-trend";
    public const string MaintenanceWarning = "maintenance-warning";
    public const string OrderCompletionVelocity = "order-completion-velocity";
    public const string DeliveryProjectionDrift = "delivery-projection-drift";
}

public static class FactoryIntelligenceEvaluationOutcomes
{
    public const string TruePositive = "true-positive";
    public const string FalsePositive = "false-positive";
    public const string RiskAvertedAfterIntervention = "risk-averted-after-intervention";
    public const string MissedRisk = "missed-risk";
    public const string Pending = "pending";
}

public static class FactoryIntelligencePermissionKeys
{
    public const string Read = "factory-intelligence.read";
    public const string Manage = "factory-intelligence.manage";
    public const string Calibration = "factory-intelligence.calibration";
}

public static class FactoryIntelligenceFailureCodes
{
    public const string NotFound = "factory-intelligence.not-found";
    public const string ValidationFailed = "factory-intelligence.validation-failed";
    public const string InsufficientData = "factory-intelligence.insufficient-data";
    public const string Suppressed = "factory-intelligence.suppressed";
}
