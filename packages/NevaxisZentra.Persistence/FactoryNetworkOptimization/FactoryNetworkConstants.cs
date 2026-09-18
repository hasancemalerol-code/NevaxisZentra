namespace NevaxisZentra.Persistence.FactoryNetworkOptimization;

public static class FactoryNetworkLifecycleStatuses
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

public static class NetworkAllocationStatuses
{
    public const string Draft = "draft";
    public const string Simulating = "simulating";
    public const string Ready = "ready";
    public const string ApprovalPending = "approval-pending";
    public const string Approved = "approved";
    public const string Executing = "executing";
    public const string Published = "published";
    public const string Verified = "verified";
    public const string Cancelled = "cancelled";
}

public static class NetworkOptionActions
{
    public const string KeepCurrentFacility = "keep-current-facility";
    public const string MoveFullOrder = "move-full-order";
    public const string MoveRemainingOperations = "move-remaining-operations";
    public const string SplitOrder = "split-order";
    public const string MoveFutureBatch = "move-future-batch";
    public const string TemporaryCapacityShift = "temporary-capacity-shift";
}

public static class NetworkDecisionTypes
{
    public const string CrossPlantReallocation = "cross-plant-reallocation";
    public const string OrderSplit = "order-split";
    public const string NetworkCapacityRecovery = "network-capacity-recovery";
    public const string FacilityFailover = "facility-failover";
}

public static class NetworkAutonomyModes
{
    public const string Observe = "observe";
    public const string Draft = "draft";
    public const string Limited = "limited";
    public const string Full = "full";
}

public static class FactoryNetworkFailureCodes
{
    public const string NotFound = "factory-network.not-found";
    public const string ValidationFailed = "factory-network.validation-failed";
    public const string Forbidden = "factory-network.forbidden";
    public const string FacilityScopeDenied = "factory-network.facility-scope-denied";
    public const string ConcurrencyConflict = "factory-network.concurrency-conflict";
    public const string Infeasible = "factory-network.infeasible";
    public const string StaleData = "factory-network.stale-data";
    public const string Conflict = "factory-network.conflict";
    public const string ApprovalRequired = "factory-network.approval-required";
    public const string DuplicateExecution = "factory-network.duplicate-execution";
    public const string QuantityViolation = "factory-network.quantity-violation";
    public const string PolicyViolation = "factory-network.policy-violation";
}

public sealed class FactoryNetworkException : Exception
{
    public string FailureCode { get; }

    public FactoryNetworkException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
