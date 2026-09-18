namespace NevaxisZentra.Persistence.FactoryLearning;

public static class FactoryLossCategories
{
    public const string Downtime = "downtime";
    public const string MicroStop = "micro-stop";
    public const string SpeedLoss = "speed-loss";
    public const string Scrap = "scrap";
    public const string Rework = "rework";
    public const string QualityHold = "quality-hold";
    public const string Changeover = "changeover";
    public const string MaterialShortage = "material-shortage";
    public const string SupplierDelay = "supplier-delay";
    public const string MoldUnavailability = "mold-unavailability";
    public const string MaintenanceDelay = "maintenance-delay";
    public const string CapacityLoss = "capacity-loss";
    public const string PlanningLoss = "planning-loss";
    public const string WaitingApproval = "waiting-approval";
    public const string ShiftHandoverLoss = "shift-handover-loss";
    public const string RepeatedIncident = "repeated-incident";
    public const string Other = "other";
}

public static class FactoryLearningDataClassifications
{
    public const string Measured = "measured";
    public const string Derived = "derived";
    public const string Estimated = "estimated";
    public const string Unknown = "unknown";
    public const string Projected = "projected";
}

public static class RootCauseConfidenceLevels
{
    public const string Confirmed = "confirmed";
    public const string StrongEvidence = "strong-evidence";
    public const string Likely = "likely";
    public const string Possible = "possible";
    public const string Unproven = "unproven";
}

public static class ImprovementBacklogStatuses
{
    public const string New = "new";
    public const string Analyzing = "analyzing";
    public const string Proposed = "proposed";
    public const string ApprovalPending = "approval-pending";
    public const string Experimenting = "experimenting";
    public const string Verifying = "verifying";
    public const string Verified = "verified";
    public const string Standardized = "standardized";
    public const string Rejected = "rejected";
    public const string Inconclusive = "inconclusive";
    public const string Regressing = "regressing";
}

public static class FactoryExperimentTypes
{
    public const string Process = "process";
    public const string Planning = "planning";
    public const string Maintenance = "maintenance";
    public const string Quality = "quality";
    public const string Material = "material";
    public const string Mold = "mold";
    public const string Shift = "shift";
    public const string Supplier = "supplier";
}

public static class FactoryExperimentStatuses
{
    public const string Draft = "draft";
    public const string ApprovalPending = "approval-pending";
    public const string Approved = "approved";
    public const string Running = "running";
    public const string Stopped = "stopped";
    public const string Completed = "completed";
    public const string Verified = "verified";
    public const string Failed = "failed";
    public const string Inconclusive = "inconclusive";
}

public static class FactoryExperimentOutcomes
{
    public const string Success = "success";
    public const string PartialSuccess = "partial-success";
    public const string NoEffect = "no-effect";
    public const string NegativeEffect = "negative-effect";
    public const string Inconclusive = "inconclusive";
}

public static class LearningMemoryQuality
{
    public const string Verified = "verified";
    public const string Partial = "partial";
    public const string Inconclusive = "inconclusive";
    public const string Failed = "failed";
}

public static class FactoryLearningFailureCodes
{
    public const string NotFound = "factory-learning.not-found";
    public const string ValidationFailed = "factory-learning.validation-failed";
    public const string Forbidden = "factory-learning.forbidden";
    public const string ConcurrencyConflict = "factory-learning.concurrency-conflict";
    public const string GuardrailBreach = "factory-learning.guardrail-breach";
    public const string ApprovalRequired = "factory-learning.approval-required";
    public const string InsufficientEvidence = "factory-learning.insufficient-evidence";
    public const string ConflictingEvidence = "factory-learning.conflicting-evidence";
}

public sealed class FactoryLearningException : Exception
{
    public string FailureCode { get; }

    public FactoryLearningException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
