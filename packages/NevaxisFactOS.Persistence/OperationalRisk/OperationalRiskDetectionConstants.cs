namespace NevaxisFactOS.Persistence.OperationalRisk;

public static class OperationalRiskDetectionConstants
{
    public const string RuleSetVersion = "OPERATIONAL-RISK-2.0";
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development operational-risk detection data — simulated, non-production, not real customer evidence, predicted loss, or guaranteed savings.";
}

public static class OperationalRiskDetectionOutcomes
{
    public const string RiskDetected = "risk-detected";
    public const string WithinThreshold = "within-threshold";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        RiskDetected, WithinThreshold, InsufficientData, SourceConflict,
    };
}

public static class OperationalRiskDetectionDriverReasonCodes
{
    public const string MaterialShortfall = "material-shortfall";
    public const string SupplierDelay = "supplier-delay";
    public const string OrderDeliveryDelay = "order-delivery-delay";
    public const string CostDeviationExceeded = "cost-deviation-exceeded";
    public const string ProductionContinuityRisk = "production-continuity-risk";
    public const string QualityRisk = "quality-risk";
    public const string MaintenanceRisk = "maintenance-risk";
    public const string EnergyRisk = "energy-risk";
    public const string ConnectivityRisk = "connectivity-risk";
    public const string SourceStale = "source-stale";
    public const string MissingThreshold = "missing-threshold";
    public const string MissingDenominator = "missing-denominator";
    public const string InsufficientEvidence = "insufficient-evidence";
    public const string SourceConflict = "source-conflict";
    public const string WithinThreshold = "within-threshold";
}

public static class OperationalRiskDetectionProfileStatuses
{
    public const string Draft = "draft";
    public const string Approved = "approved";
    public const string Inactive = "inactive";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft, Approved, Inactive,
    };
}

public static class OperationalRiskDetectionRunStatuses
{
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public static class OperationalRiskSourceModules
{
    public const string Energy = "energy";
    public const string IotPlc = "iot-plc";
    public const string Maintenance = "maintenance";
    public const string Material = "material";
    public const string Supplier = "supplier";
    public const string Order = "order";
    public const string Quality = "quality";
    public const string Production = "production";
    public const string Cost = "cost";
    public const string DevelopmentSimulator = "development-simulator";
}

public static class OperationalRiskDetectionAuditEventTypes
{
    public const string ProfileCreated = "operational-risk.detection.profile.created";
    public const string ProfileUpdated = "operational-risk.detection.profile.updated";
    public const string RunCompleted = "operational-risk.detection.run.completed";
}
