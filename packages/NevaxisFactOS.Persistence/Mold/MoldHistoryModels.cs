namespace NevaxisFactOS.Persistence.Mold;

public static class MoldHistoryFailureCodes
{
    public const string HistoryIdempotencyConflict = "history-idempotency-conflict";
    public const string DuplicateRevisionNumber = "duplicate-revision-number";
    public const string DuplicateTrialNumber = "duplicate-trial-number";
    public const string InvalidMaintenanceState = "invalid-maintenance-state";
}

public sealed record MoldHistoryListFilters(int Page = 1, int PageSize = 25);

public sealed record MoldMaintenanceReadModel(
    Guid Id,
    Guid MoldId,
    string MaintenanceType,
    string Status,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Summary,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName,
    string? ExternalMaintenanceRequestReference,
    string? ExternalWorkOrderReference,
    string SourceType,
    string? SourceReference,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MoldRevisionReadModel(
    Guid Id,
    Guid MoldId,
    string RevisionNumber,
    string RevisionType,
    DateOnly RevisionDate,
    string Summary,
    string? Reason,
    string? TechnicalChange,
    int? PreviousCavityCount,
    int? NewCavityCount,
    string? PerformedByDisplayName,
    string? ExternalReference,
    string SourceType,
    string? SourceReference,
    DateTimeOffset CreatedAtUtc);

public sealed record MoldTrialReadModel(
    Guid Id,
    Guid MoldId,
    string TrialNumber,
    DateOnly TrialDate,
    string Result,
    string? MachineReference,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MaterialReference,
    decimal? CycleTimeSeconds,
    int? ShotCount,
    int? GoodPartCount,
    int? ScrapPartCount,
    string? Findings,
    string? Decision,
    string? PerformedByDisplayName,
    string SourceType,
    string? SourceReference,
    DateTimeOffset CreatedAtUtc);

public sealed record MoldTimelineEntryReadModel(
    Guid Id,
    string EventKind,
    DateTimeOffset EventDateUtc,
    DateTimeOffset CreatedAtUtc,
    string Title,
    string Summary,
    string? StatusOrResult,
    string? Reference,
    string? ActorDisplayName);

public sealed record CreateMoldMaintenanceCommand(
    Guid MoldId,
    string MaintenanceType,
    string Status,
    DateTimeOffset OpenedAtUtc,
    string Summary,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName,
    string? ExternalMaintenanceRequestReference,
    string? ExternalWorkOrderReference,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);

public sealed record UpdateMoldMaintenanceCommand(
    Guid MoldId,
    Guid MaintenanceId,
    uint ExpectedVersion,
    string MaintenanceType,
    string Status,
    DateTimeOffset OpenedAtUtc,
    string Summary,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName,
    string? ExternalMaintenanceRequestReference,
    string? ExternalWorkOrderReference,
    string SourceType,
    string? SourceReference);

public sealed record CompleteMoldMaintenanceCommand(
    Guid MoldId,
    Guid MaintenanceId,
    uint ExpectedVersion,
    DateTimeOffset CompletedAtUtc,
    string? Findings,
    string? ActionsTaken,
    string? PerformedByDisplayName);

public sealed record CancelMoldMaintenanceCommand(
    Guid MoldId,
    Guid MaintenanceId,
    uint ExpectedVersion,
    string? Findings);

public sealed record AppendMoldRevisionCommand(
    Guid MoldId,
    string RevisionNumber,
    string RevisionType,
    DateOnly RevisionDate,
    string Summary,
    string? Reason,
    string? TechnicalChange,
    int? PreviousCavityCount,
    int? NewCavityCount,
    string? PerformedByDisplayName,
    string? ExternalReference,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);

public sealed record AppendMoldTrialCommand(
    Guid MoldId,
    string TrialNumber,
    DateOnly TrialDate,
    string Result,
    string? MachineReference,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MaterialReference,
    decimal? CycleTimeSeconds,
    int? ShotCount,
    int? GoodPartCount,
    int? ScrapPartCount,
    string? Findings,
    string? Decision,
    string? PerformedByDisplayName,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);
