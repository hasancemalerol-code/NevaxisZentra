namespace CrevaFactOS.Persistence.OperationalRisk;

public static class OperationalRiskConstants
{
    public const string RuleSetVersion = "OPERATIONAL-RISK-1.0";
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development operational-risk data — simulated, non-production, not real customer evidence or guaranteed loss.";
}

public static class OperationalRiskFailureCodes
{
    public const string ValidationFailed = "operational-risk.validation-failed";
    public const string NotFound = "operational-risk.not-found";
    public const string InvalidScope = "operational-risk.invalid-scope";
    public const string ScopeReferenceRequired = "operational-risk.scope-reference-required";
    public const string ConcurrencyConflict = "operational-risk.concurrency-conflict";
    public const string IdempotencyConflict = "operational-risk.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "operational-risk.idempotency-legacy-unverifiable";
    public const string InvalidTransition = "operational-risk.invalid-transition";
    public const string TerminalState = "operational-risk.terminal-state";
    public const string FacilityScopeDenied = "operational-risk.facility-scope-denied";
}

public static class OperationalRiskCategories
{
    public const string MaterialAvailability = "material-availability";
    public const string Supplier = "supplier";
    public const string OrderDelivery = "order-delivery";
    public const string CostDeviation = "cost-deviation";
    public const string ProductionContinuity = "production-continuity";
    public const string Quality = "quality";
    public const string Maintenance = "maintenance";
    public const string Energy = "energy";
    public const string IotConnectivity = "iot-connectivity";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MaterialAvailability, Supplier, OrderDelivery, CostDeviation, ProductionContinuity,
        Quality, Maintenance, Energy, IotConnectivity, Other,
    };
}

public static class OperationalRiskScopeTypes
{
    public const string Facility = "facility";
    public const string Material = "material";
    public const string Supplier = "supplier";
    public const string PurchaseOrder = "purchase-order";
    public const string CustomerOrder = "customer-order";
    public const string ProductionOrder = "production-order";
    public const string Machine = "machine";
    public const string Mold = "mold";
    public const string Shipment = "shipment";
    public const string EnergyMeasurementPoint = "energy-measurement-point";
    public const string IotEndpoint = "iot-endpoint";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Facility, Material, Supplier, PurchaseOrder, CustomerOrder, ProductionOrder,
        Machine, Mold, Shipment, EnergyMeasurementPoint, IotEndpoint, Other,
    };

    public static bool RequiresScopeReference(string scopeType) =>
        !string.Equals(scopeType, Facility, StringComparison.Ordinal);
}

public static class OperationalRiskEvidenceStates
{
    public const string Measured = "measured";
    public const string Calculated = "calculated";
    public const string Estimated = "estimated";
    public const string Simulated = "simulated";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Measured, Calculated, Estimated, Simulated, InsufficientData, SourceConflict,
    };
}

public static class OperationalRiskAssessmentOutcomes
{
    public const string Scored = "scored";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class OperationalRiskSeverityBands
{
    public const string Low = "low";
    public const string Moderate = "moderate";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class OperationalRiskLifecycleStatuses
{
    public const string Detected = "detected";
    public const string UnderReview = "under-review";
    public const string Accepted = "accepted";
    public const string MitigationPlanned = "mitigation-planned";
    public const string Monitoring = "monitoring";
    public const string Resolved = "resolved";
    public const string Dismissed = "dismissed";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Detected, UnderReview, Accepted, MitigationPlanned, Monitoring, Resolved, Dismissed,
    };

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.Ordinal)
    {
        Resolved, Dismissed,
    };
}

public static class OperationalRiskAuditEventTypes
{
    public const string CaseCreated = "operational-risk.case.created";
    public const string CaseAssessmentUpdated = "operational-risk.case.assessment-updated";
    public const string EvidenceAppended = "operational-risk.evidence.appended";
    public const string CaseTransitioned = "operational-risk.case.transitioned";
}
