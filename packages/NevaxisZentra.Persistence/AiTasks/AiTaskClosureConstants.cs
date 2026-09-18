namespace NevaxisZentra.Persistence.AiTasks;

public static class AiTaskClosureStates
{
    public const string None = "none";
    public const string ResolutionRequested = "resolution-requested";
    public const string AwaitingClosureApproval = "awaiting-closure-approval";
    public const string ClosureRejected = "closure-rejected";
    public const string Closed = "closed";
    public const string Reopened = "reopened";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        None, ResolutionRequested, AwaitingClosureApproval, ClosureRejected, Closed, Reopened,
    };
}

public static class AiTaskEvidenceTypes
{
    public const string Photo = "photo";
    public const string Document = "document";
    public const string Measurement = "measurement";
    public const string MachineReading = "machine-reading";
    public const string QualityRecord = "quality-record";
    public const string MaintenanceRecord = "maintenance-record";
    public const string ProductionRecord = "production-record";
    public const string ExternalReference = "external-reference";
    public const string Note = "note";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Photo, Document, Measurement, MachineReading, QualityRecord, MaintenanceRecord,
        ProductionRecord, ExternalReference, Note,
    };
}

public static class AiTaskResolutionCodes
{
    public const string Resolved = "resolved";
    public const string Mitigated = "mitigated";
    public const string AcceptedRisk = "accepted-risk";
    public const string FalsePositive = "false-positive";
    public const string DeferredFollowUp = "deferred-follow-up";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Resolved, Mitigated, AcceptedRisk, FalsePositive, DeferredFollowUp,
    };
}
