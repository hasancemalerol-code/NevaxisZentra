namespace NevaxisFactOS.Persistence.Energy;

public static class EnergyIntelligenceConstants
{
    public const string RuleSetVersion = "ENERGY-2.0";
    public const int MinimumComparableWindows = 6;
    public const decimal MadScaleFactor = 1.4826m;
    public const decimal DefaultRelativeTolerance = 0.10m;
    public const decimal SigmaMultiplier = 3m;
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development energy intelligence — simulated, non-production, not real customer meter evidence or guaranteed savings.";
}

public static class EnergyBaselineDimensions
{
    public const string Facility = "facility";
    public const string Machine = "machine";
    public const string AuxiliaryEquipment = "auxiliary-equipment";
    public const string Shift = "shift";
    public const string ProductionOrder = "production-order";
    public const string ProductionContext = "production-context";
}

public static class EnergyTargetMetrics
{
    public const string ConsumedKwh = "consumed-kwh";
    public const string MaximumDemandKw = "maximum-demand-kw";
    public const string KwhPerKg = "kwh-per-kg";
    public const string KwhPer1000Parts = "kwh-per-1000-parts";
    public const string KwhPer1000GoodParts = "kwh-per-1000-good-parts";
    public const string IdleConsumptionKwh = "idle-consumption-kwh";
}

public static class EnergyTargetStatuses
{
    public const string Approved = "approved";
    public const string Inactive = "inactive";
}

public static class EnergyIntelligenceOutcomes
{
    public const string WithinTarget = "within-target";
    public const string Attention = "attention";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class EnergyIntelligenceDriverReasonCodes
{
    public const string ConsumptionAboveBaseline = "consumption-above-baseline";
    public const string ConsumptionAboveTarget = "consumption-above-target";
    public const string PeakDemandExceeded = "peak-demand-exceeded";
    public const string IdleConsumptionElevated = "idle-consumption-elevated";
    public const string EnergyIntensityElevated = "energy-intensity-elevated";
    public const string AuxiliaryLoadElevated = "auxiliary-load-elevated";
    public const string SourceStale = "source-stale";
    public const string SourceConflict = "source-conflict";
    public const string InsufficientComparableWindows = "insufficient-comparable-windows";
    public const string MissingProductionDenominator = "missing-production-denominator";
    public const string MissingProductionState = "missing-production-state";
    public const string MissingTarget = "missing-target";
    public const string MissingTariff = "missing-tariff";
    public const string WithinTarget = "within-target";
}

public static class EnergyIntelligenceDriverSeverities
{
    public const string Pass = "pass";
    public const string Warn = "warn";
    public const string Fail = "fail";
    public const string Info = "info";
}

public static class EnergyIntelligenceDriverCategories
{
    public const string Consumption = "consumption";
    public const string PeakDemand = "peak-demand";
    public const string Idle = "idle";
    public const string Intensity = "intensity";
    public const string Auxiliary = "auxiliary";
    public const string Evidence = "evidence";
    public const string Target = "target";
    public const string Cost = "cost";
}

public static class EnergyOpportunityLabels
{
    public const string AboveBaseline = "Baseline üzeri tüketim";
    public const string AboveTarget = "Hedef üzeri tüketim";
    public const string PotentialReducible = "Potansiyel azaltılabilir tüketim";
    public const string EstimatedCostImpact = "Tahmini maliyet etkisi";
}

public static class EnergyIntelligenceFailureCodes
{
    public const string BaselineNotFound = "energy.intelligence.baseline-not-found";
    public const string TargetNotFound = "energy.intelligence.target-not-found";
    public const string AssessmentNotFound = "energy.intelligence.assessment-not-found";
    public const string IncompatibleWindows = "energy.intelligence.incompatible-windows";
}

public static class EnergyIntelligenceAuditEventTypes
{
    public const string BaselineEvaluated = "energy.intelligence.baseline.evaluated";
    public const string TargetCreated = "energy.intelligence.target.created";
    public const string TargetUpdated = "energy.intelligence.target.updated";
    public const string AssessmentEvaluated = "energy.intelligence.assessment.evaluated";
}
