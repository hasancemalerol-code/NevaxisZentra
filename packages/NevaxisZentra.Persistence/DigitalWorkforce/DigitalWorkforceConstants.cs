namespace NevaxisZentra.Persistence.DigitalWorkforce;

public static class DigitalWorkforceWorkerCodes
{
    public const string ProductionRecovery = "DW-FACT-001";
    public const string MaintenanceResolution = "DW-FACT-002";
    public const string MaterialShortage = "DW-FACT-003";
    public const string QualityNcr = "DW-FACT-004";
    public const string SupplierRecovery = "DW-FACT-005";
    public const string MoldLifecycle = "DW-FACT-006";
    public const string FactoryCoordinator = "DW-FACT-007";
    public const string ThroughputOptimizer = "DW-FACT-008";
    public const string CustomsFileDigitalWorker = "DW-EXP-001";
}

public static class DigitalWorkforceAutonomyModes
{
    public const string Observe = "observe";
    public const string Draft = "draft";
    public const string Limited = "limited";
    public const string Full = "full";
}

public static class DigitalWorkforceWorkerStatuses
{
    public const string Running = "running";
    public const string Paused = "paused";
    public const string Killed = "killed";
    public const string HumanControlled = "human-controlled";
    public const string Idle = "idle";
}

public static class DigitalWorkCaseStatuses
{
    public const string Detected = "detected";
    public const string Analyzing = "analyzing";
    public const string Deciding = "deciding";
    public const string Planning = "planning";
    public const string AwaitingApproval = "awaiting-approval";
    public const string Executing = "executing";
    public const string Verifying = "verifying";
    public const string Escalated = "escalated";
    public const string Closed = "closed";
}

public static class DigitalWorkCaseStages
{
    public const string Detect = "detect";
    public const string Analyze = "analyze";
    public const string Decide = "decide";
    public const string Plan = "plan";
    public const string Approve = "approve";
    public const string Act = "act";
    public const string Verify = "verify";
    public const string Close = "close";
}

public static class DigitalWorkforceSourceClassifications
{
    public const string Real = "real";
    public const string Demo = "demo";
    public const string Simulated = "simulated";
}

public static class DigitalWorkApprovalPolicies
{
    public const string AlwaysAsk = "always-ask";
    public const string AskAboveThreshold = "ask-above-threshold";
    public const string AutoAllowed = "auto-allowed";
    public const string NeverAllowed = "never-allowed";
}

public static class DigitalWorkApprovalStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Modified = "modified";
    public const string Expired = "expired";
}

public static class DigitalWorkVerificationResults
{
    public const string Passed = "passed";
    public const string Failed = "failed";
    public const string Pending = "pending";
}

public static class DigitalWorkforceBudgetStates
{
    public const string Normal = "normal";
    public const string NearLimit = "near-limit";
    public const string LimitReached = "limit-reached";
}

public static class DigitalWorkforceParticipantRoles
{
    public const string Primary = "primary";
    public const string Supporting = "supporting";
    public const string Coordinator = "coordinator";
}

public static class DigitalWorkforceHandoffStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Completed = "completed";
    public const string Rejected = "rejected";
}

public static class DigitalWorkExecutionStatuses
{
    public const string Queued = "queued";
    public const string Claimed = "claimed";
    public const string Running = "running";
    public const string WaitingApproval = "waiting-approval";
    public const string RetryWait = "retry-wait";
    public const string Paused = "paused";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Verified = "verified";
}

public static class DigitalWorkActionClassifications
{
    public const string Read = "read";
    public const string Analyze = "analyze";
    public const string Prepare = "prepare";
    public const string WriteInternal = "write-internal";
    public const string WriteExternal = "write-external";
    public const string ChangeOperation = "change-operation";
    public const string Financial = "financial";
    public const string SafetyCritical = "safety-critical";
}

public static class DigitalWorkExecutionErrorClassifications
{
    public const string Transient = "transient";
    public const string Permanent = "permanent";
    public const string Policy = "policy";
    public const string Approval = "approval";
    public const string Configuration = "configuration";
    public const string ExternalUnknown = "external-unknown";
}

public static class DigitalWorkExternalActionStatuses
{
    public const string Requested = "requested";
    public const string Sent = "sent";
    public const string Acknowledged = "acknowledged";
    public const string Failed = "failed";
    public const string Unknown = "unknown";
}

public static class DigitalWorkerHealthStates
{
    public const string Healthy = "healthy";
    public const string Busy = "busy";
    public const string WaitingApproval = "waiting-approval";
    public const string Blocked = "blocked";
    public const string Degraded = "degraded";
    public const string Paused = "paused";
    public const string Offline = "offline";
}

public static class DigitalWorkforceFailureCodes
{
    public const string NotFound = "digital-workforce.not-found";
    public const string ValidationFailed = "digital-workforce.validation-failed";
    public const string Forbidden = "digital-workforce.forbidden";
    public const string PolicyDenied = "digital-workforce.policy-denied";
    public const string AutonomyDenied = "digital-workforce.autonomy-denied";
    public const string KillSwitchActive = "digital-workforce.kill-switch-active";
    public const string Paused = "digital-workforce.paused";
    public const string InvalidTransition = "digital-workforce.invalid-transition";
    public const string VerificationRequired = "digital-workforce.verification-required";
    public const string ApprovalRequired = "digital-workforce.approval-required";
    public const string BudgetExceeded = "digital-workforce.budget-exceeded";
    public const string ScopeViolation = "digital-workforce.scope-violation";
    public const string ConcurrencyConflict = "digital-workforce.concurrency-conflict";
    public const string ExecutionLeaseLost = "digital-workforce.execution-lease-lost";
    public const string ExecutionStaleApproval = "digital-workforce.execution-stale-approval";
    public const string ExecutionBudgetExceeded = "digital-workforce.execution-budget-exceeded";
    public const string ExecutionConflict = "digital-workforce.execution-conflict";
    public const string ExecutionDuplicate = "digital-workforce.execution-duplicate";
}
