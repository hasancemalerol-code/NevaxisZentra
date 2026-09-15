namespace NevaxisFactOS.Persistence.FactoryOrchestration;

public static class FactoryOrchestrationDomains
{
    public const string Production = "production";
    public const string Planning = "planning";
    public const string Maintenance = "maintenance";
    public const string Quality = "quality";
    public const string Material = "material";
    public const string Supplier = "supplier";
    public const string Mold = "mold";
    public const string Shift = "shift";
    public const string Iot = "iot";
}

public static class FactoryCaseBlockerCodes
{
    public const string None = "none";
    public const string WaitingMaintenance = "waiting-maintenance";
    public const string WaitingMaterial = "waiting-material";
    public const string WaitingApproval = "waiting-approval";
    public const string WaitingQuality = "waiting-quality";
    public const string WaitingSupplier = "waiting-supplier";
    public const string WaitingMachineVerification = "waiting-machine-verification";
    public const string WaitingHuman = "waiting-human";
    public const string WaitingExternalSystem = "waiting-external-system";
    public const string WaitingData = "waiting-data";
    public const string Deadlock = "deadlock";
    public const string Conflict = "conflict";
}

public static class FactoryDependencyEdgeTypes
{
    public const string Blocks = "blocks";
    public const string DependsOn = "depends-on";
    public const string Triggers = "triggers";
    public const string Requires = "requires";
    public const string Verifies = "verifies";
    public const string Resolves = "resolves";
    public const string HandoffTo = "handoff-to";
}

public static class FactoryDependencyNodeTypes
{
    public const string Event = "event";
    public const string DomainIssue = "domain-issue";
    public const string DigitalWorker = "digital-worker";
    public const string Task = "task";
    public const string Approval = "approval";
    public const string ExternalAction = "external-action";
    public const string Verification = "verification";
    public const string Outcome = "outcome";
}

public static class FactoryOrchestrationFailureCodes
{
    public const string NotFound = "factory-orchestration.not-found";
    public const string ValidationFailed = "factory-orchestration.validation-failed";
    public const string Forbidden = "factory-orchestration.forbidden";
    public const string ConcurrencyConflict = "factory-orchestration.concurrency-conflict";
    public const string DeadlockDetected = "factory-orchestration.deadlock-detected";
    public const string OutcomeNotVerified = "factory-orchestration.outcome-not-verified";
    public const string DuplicateEvent = "factory-orchestration.duplicate-event";
    public const string EventNotQualified = "factory-orchestration.event-not-qualified";
    public const string ConflictUnresolved = "factory-orchestration.conflict-unresolved";
}

public sealed class FactoryOrchestrationException : Exception
{
    public string FailureCode { get; }

    public FactoryOrchestrationException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
