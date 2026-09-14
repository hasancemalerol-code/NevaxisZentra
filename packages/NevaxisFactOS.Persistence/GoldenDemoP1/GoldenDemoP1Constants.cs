namespace NevaxisFactOS.Persistence.GoldenDemoP1;

public static class GoldenDemoP1EventStatuses
{
    public const string Active = "active";
    public const string InterventionInProgress = "intervention-in-progress";
    public const string ValidationRun = "validation-run";
    public const string Verified = "verified";
    public const string Closed = "closed";
}

public static class GoldenDemoReferenceSources
{
    public const string StableHistoricalWindow = "stable-historical-window";
    public const string EngineeringStandard = "engineering-standard";
    public const string ValidatedBestWindow = "validated-best-window";
    public const string Insufficient = "insufficient-baseline";
}

public static class GoldenDemoCyclePhaseCodes
{
    public const string Injection = "injection";
    public const string Hold = "hold";
    public const string Cooling = "cooling";
    public const string ScrewRecovery = "screw-recovery";
    public const string MoldMovement = "mold-movement";
    public const string OtherProcess = "other-process";
}

public static class GoldenDemoContributorConfidence
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
}

public static class GoldenDemoContributorKinds
{
    public const string ProbableContributor = "probable-contributor";
    public const string LikelyContributor = "likely-contributor";
    public const string AssociatedPattern = "associated-pattern";
    public const string SuspectedCause = "suspected-cause";
}

public static class GoldenDemoPlanActionStatuses
{
    public const string Planned = "planned";
    public const string InProgress = "in-progress";
    public const string Completed = "completed";
    public const string Blocked = "blocked";
}

public static class GoldenDemoWhatIfScenarioCodes
{
    public const string ContinueCurrent = "continue-current";
    public const string LaterIntervention = "later-intervention";
    public const string ControlledInterventionNow = "controlled-intervention-now";
}

public static class GoldenDemoVerificationResults
{
    public const string Verified = "verified";
    public const string PartiallyVerified = "partially-verified";
    public const string NotVerified = "not-verified";
    public const string InsufficientData = "insufficient-data";
}

public static class GoldenDemoAgentCodes
{
    public const string ProductionPerformance = "production-performance";
    public const string MachinePerformance = "machine-performance";
    public const string ProcessOptimization = "process-optimization";
    public const string MoldHealth = "mold-health";
    public const string Maintenance = "maintenance";
    public const string Energy = "energy";
    public const string Orchestrator = "orchestrator";
}

public static class GoldenDemoImpactClassifications
{
    public const string Measured = "measured";
    public const string Calculated = "calculated";
    public const string Estimated = "estimated";
    public const string Verified = "verified";
}

public static class GoldenDemoP1FailureCodes
{
    public const string InsufficientBaseline = "golden-demo-p1.insufficient-baseline";
    public const string EventNotFound = "golden-demo-p1.event-not-found";
    public const string InvalidState = "golden-demo-p1.invalid-state";
    public const string Unauthorized = "golden-demo-p1.unauthorized";
}

public sealed class GoldenDemoP1Exception : Exception
{
    public string FailureCode { get; }

    public GoldenDemoP1Exception(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
