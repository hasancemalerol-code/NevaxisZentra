namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public static class ThroughputOptimizerPermissionKeys
{
    public const string Read = "throughput-optimizer.read";
    public const string Manage = "throughput-optimizer.manage";
    public const string Run = "throughput-optimizer.run";
}

public static class ThroughputOptimizerFailureCodes
{
    public const string NotFound = "throughput-optimizer.not-found";
    public const string ValidationFailed = "throughput-optimizer.validation-failed";
    public const string GapNotDetected = "throughput-optimizer.gap-not-detected";
    public const string CaseAlreadyActive = "throughput-optimizer.case-already-active";
    public const string ApprovalRequired = "throughput-optimizer.approval-required";
    public const string ApprovalDenied = "throughput-optimizer.approval-denied";
    public const string ObservationIncomplete = "throughput-optimizer.observation-incomplete";
    public const string VerificationFailed = "throughput-optimizer.verification-failed";
    public const string PolicyDenied = "throughput-optimizer.policy-denied";
}

public static class ThroughputOptimizerPhases
{
    public const string Observe = "OBSERVE";
    public const string Diagnose = "DIAGNOSE";
    public const string Simulate = "SIMULATE";
    public const string Decide = "DECIDE";
    public const string Approval = "APPROVAL";
    public const string Execute = "EXECUTE";
    public const string ObserveResult = "OBSERVE_RESULT";
    public const string Verify = "VERIFY";
    public const string Closed = "CLOSED";
}

public static class ThroughputOptimizerVerificationOutcomes
{
    public const string VerifiedImprovement = "VERIFIED_IMPROVEMENT";
    public const string PartialImprovement = "PARTIAL_IMPROVEMENT";
    public const string NoImprovement = "NO_IMPROVEMENT";
    public const string Regression = "REGRESSION";
    public const string InsufficientObservation = "INSUFFICIENT_OBSERVATION";
    public const string QualityGuardFailed = "QUALITY_GUARD_FAILED";
    public const string SafetyGuardFailed = "SAFETY_GUARD_FAILED";
}

public static class ThroughputOptimizerActionTypes
{
    public const string OperatorTask = "OPERATOR_TASK";
    public const string ResourceReassignment = "RESOURCE_REASSIGNMENT";
    public const string OperationRedistribution = "OPERATION_REDISTRIBUTION";
    public const string ObservationWindow = "OBSERVATION_WINDOW";
}

public static class ThroughputOptimizerCorrelationPrefixes
{
    public const string LineGap = "throughput-gap";
}

public static class ThroughputOptimizerEngineVersions
{
    public const string Observe = "throughput-optimizer-observe-v1";
    public const string VerifiedImprovement = "verified-improvement-v1";
}

public static class VerifiedDatasetEligibilityStatuses
{
    public const string Eligible = "ELIGIBLE";
    public const string Ineligible = "INELIGIBLE";
    public const string PendingReview = "PENDING_REVIEW";
}
