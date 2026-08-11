namespace CrevaFactOS.Persistence.Mold;

public sealed record MoldOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class MoldFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string ValidationFailed = "validation-failed";
    public const string DuplicateCode = "duplicate-code";
    public const string IdempotencyConflict = "idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "idempotency-legacy-unverifiable";
    public const string SourceUnavailable = "source-unavailable";
}

public sealed class MoldWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }

    public static MoldWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static MoldWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record MoldListFilters(
    string? Search,
    string? LifecycleState,
    string? OwnershipType,
    string? LocationType,
    string? SourceType,
    int Page,
    int PageSize);

public sealed record MoldSummaryReadModel(
    Guid Id,
    string Code,
    string Name,
    string LifecycleState,
    string OwnershipType,
    string LocationType,
    string? LocationLabel,
    Guid FacilityId,
    int? CavityCount,
    long? CurrentShotCount,
    DateTimeOffset? CounterRecordedAtUtc,
    string SourceType,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MoldDetailReadModel(
    Guid Id,
    Guid FacilityId,
    string Code,
    string Name,
    string? Description,
    string? SerialNumber,
    string? Manufacturer,
    int? ManufactureYear,
    DateOnly? ManufactureDate,
    DateOnly? CommissioningDate,
    string SourceType,
    string? ExternalSourceReference,
    string OwnershipType,
    string? OwnerReference,
    string LocationType,
    string? LocationLabel,
    DateTimeOffset? LocationChangedAtUtc,
    string LifecycleState,
    string? MoldType,
    int? CavityCount,
    long? NominalShotLife,
    long? CurrentShotCount,
    DateTimeOffset? CounterRecordedAtUtc,
    string? CounterSource,
    string? CounterSourceReference,
    string? CounterNote,
    string? PrimaryProductReference,
    string? MaterialFamilyReference,
    string? RunnerType,
    bool? HotRunnerPresent,
    decimal? MoldWeightKg,
    decimal? DimensionLengthMm,
    decimal? DimensionWidthMm,
    decimal? DimensionHeightMm,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MoldLifecycleEventReadModel(
    Guid Id,
    string PreviousState,
    string NewState,
    string Reason,
    string ActorDisplayName,
    DateTimeOffset OccurredAtUtc,
    string? SourceReference,
    uint Version);

public sealed record MoldWorkspaceSummaryReadModel(
    int TotalCount,
    int DraftCount,
    int ActiveCount,
    int MaintenanceCount,
    int TrialCount,
    int QuarantinedCount,
    int RetiredCount,
    int ScrappedCount);

public sealed record CreateMoldCommand(
    string Code,
    string Name,
    string? Description,
    string? SerialNumber,
    string? Manufacturer,
    int? ManufactureYear,
    DateOnly? ManufactureDate,
    DateOnly? CommissioningDate,
    string SourceType,
    string? ExternalSourceReference,
    string OwnershipType,
    string? OwnerReference,
    string LocationType,
    string? LocationLabel,
    string? MoldType,
    int? CavityCount,
    long? NominalShotLife,
    long? CurrentShotCount,
    string? PrimaryProductReference,
    string? MaterialFamilyReference,
    string? RunnerType,
    bool? HotRunnerPresent,
    decimal? MoldWeightKg,
    decimal? DimensionLengthMm,
    decimal? DimensionWidthMm,
    decimal? DimensionHeightMm,
    string? Notes,
    string? IdempotencyKey);

public sealed record UpdateMoldCommand(
    Guid MoldId,
    uint ExpectedVersion,
    string Name,
    string? Description,
    string? SerialNumber,
    string? Manufacturer,
    int? ManufactureYear,
    DateOnly? ManufactureDate,
    DateOnly? CommissioningDate,
    string? ExternalSourceReference,
    string OwnershipType,
    string? OwnerReference,
    string LocationType,
    string? LocationLabel,
    string? MoldType,
    int? CavityCount,
    long? NominalShotLife,
    string? PrimaryProductReference,
    string? MaterialFamilyReference,
    string? RunnerType,
    bool? HotRunnerPresent,
    decimal? MoldWeightKg,
    decimal? DimensionLengthMm,
    decimal? DimensionWidthMm,
    decimal? DimensionHeightMm,
    string? Notes);

public sealed record RecordMoldCounterSnapshotCommand(
    Guid MoldId,
    uint ExpectedVersion,
    long ShotCount,
    string SourceType,
    string? SourceReference,
    string? Note,
    string IdempotencyKey);

public sealed record TransitionMoldLifecycleCommand(
    Guid MoldId,
    uint ExpectedVersion,
    string TargetState,
    string Reason,
    string? SourceReference,
    string IdempotencyKey);
