namespace NevaxisZentra.Persistence.ContinuousOptimization;

public static class ContinuousOptimizationPermissionKeys
{
    public const string Read = "continuous-optimization.read";
    public const string Manage = "continuous-optimization.manage";
    public const string Run = "continuous-optimization.run";
}

public static class ContinuousOptimizationFailureCodes
{
    public const string NotFound = "continuous-optimization.not-found";
    public const string ValidationFailed = "continuous-optimization.validation-failed";
    public const string NoSignificantOpportunity = "continuous-optimization.no-significant-opportunity";
    public const string DuplicateOpportunity = "continuous-optimization.duplicate-opportunity";
    public const string CooldownActive = "continuous-optimization.cooldown-active";
    public const string PolicyBlocked = "continuous-optimization.policy-blocked";
    public const string InsufficientContext = "continuous-optimization.insufficient-context";
    public const string BudgetExceeded = "continuous-optimization.budget-exceeded";
}

public static class ContinuousOptimizationTriggerTypes
{
    public const string ThroughputDeviationChanged = "ThroughputDeviationChanged";
    public const string ConstraintChanged = "ConstraintChanged";
    public const string BufferStateChanged = "BufferStateChanged";
    public const string OrderRiskChanged = "OrderRiskChanged";
    public const string ScheduleDeviationChanged = "ScheduleDeviationChanged";
    public const string QualityDeviationChanged = "QualityDeviationChanged";
    public const string MaintenanceRiskChanged = "MaintenanceRiskChanged";
    public const string InsignificantVariation = "InsignificantVariation";
}

public static class ContinuousOptimizationTypes
{
    public const string Throughput = "THROUGHPUT";
    public const string Constraint = "CONSTRAINT";
    public const string Scheduling = "SCHEDULING";
    public const string Buffer = "BUFFER";
    public const string Quality = "QUALITY";
    public const string Material = "MATERIAL";
    public const string Operator = "OPERATOR";
    public const string Maintenance = "MAINTENANCE";
    public const string Changeover = "CHANGEOVER";
}

public static class ContinuousOptimizationPhases
{
    public const string Detected = "DETECTED";
    public const string Qualifying = "QUALIFYING";
    public const string Analyzing = "ANALYZING";
    public const string Simulating = "SIMULATING";
    public const string NoAction = "NO_ACTION";
    public const string Recommended = "RECOMMENDED";
    public const string ApprovalRequired = "APPROVAL_REQUIRED";
    public const string Approved = "APPROVED";
    public const string Executing = "EXECUTING";
    public const string Observing = "OBSERVING";
    public const string Verifying = "VERIFYING";
    public const string Verified = "VERIFIED";
    public const string NotVerified = "NOT_VERIFIED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
}

public static class ContinuousOptimizationOutcomes
{
    public const string VerifiedGain = "VERIFIED_GAIN";
    public const string NoMeaningfulGain = "NO_MEANINGFUL_GAIN";
    public const string Regression = "REGRESSION";
    public const string Inconclusive = "INCONCLUSIVE";
    public const string ActionNotExecuted = "ACTION_NOT_EXECUTED";
    public const string Expired = "EXPIRED";
    public const string Blocked = "BLOCKED";
    public const string Failed = "FAILED";
    public const string NoSignificantOpportunity = "NO_SIGNIFICANT_OPPORTUNITY";
}

public static class ContinuousOptimizationActionRiskClasses
{
    public const string Low = "LOW";
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
    public const string SafetyCritical = "SAFETY_CRITICAL";
}

public static class ContinuousOptimizationCorrelationPrefixes
{
    public const string ThroughputGap = "co-throughput-gap";
}

public static class ContinuousOptimizationEngineVersions
{
    public const string Controller = "continuous-optimization-controller-v1";
    public const string Qualification = "continuous-optimization-qualification-v1";
}
