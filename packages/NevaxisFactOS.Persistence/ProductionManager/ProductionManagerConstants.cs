namespace NevaxisFactOS.Persistence.ProductionManager;

public static class ProductionManagerAutonomyModes
{
    public const string Observe = "observe";
    public const string Draft = "draft";
    public const string Limited = "limited";
    public const string Full = "full";
}

public static class ProductionManagerConfidenceBands
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
    public const string Insufficient = "insufficient-data";
}

public static class ProductionManagerDataClassifications
{
    public const string Observed = "observed";
    public const string Predicted = "predicted";
    public const string Simulated = "simulated";
    public const string Estimated = "estimated";
    public const string Verified = "verified";
}

public static class ProductionManagerDecisionTypes
{
    public const string OrderDelay = "order-delay";
    public const string MachineRisk = "machine-risk";
    public const string MaterialDepletion = "material-depletion";
    public const string QualityEscalation = "quality-escalation";
    public const string MoldRisk = "mold-risk";
    public const string CapacityOverload = "capacity-overload";
}

public static class ProductionManagerDecisionStatuses
{
    public const string Proposed = "proposed";
    public const string PendingApproval = "pending-approval";
    public const string Approved = "approved";
    public const string Executing = "executing";
    public const string Verified = "verified";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";
}

public static class ProductionManagerFailureCodes
{
    public const string NotFound = "production-manager.not-found";
    public const string ValidationFailed = "production-manager.validation-failed";
    public const string Forbidden = "production-manager.forbidden";
    public const string ConcurrencyConflict = "production-manager.concurrency-conflict";
    public const string StaleData = "production-manager.stale-data";
    public const string ConflictUnresolved = "production-manager.conflict-unresolved";
    public const string LowConfidence = "production-manager.low-confidence";
    public const string AutonomyDenied = "production-manager.autonomy-denied";
    public const string AlreadyExecuted = "production-manager.already-executed";
    public const string ConstraintViolation = "production-manager.constraint-violation";
}

public sealed class ProductionManagerException : Exception
{
    public string FailureCode { get; }

    public ProductionManagerException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
