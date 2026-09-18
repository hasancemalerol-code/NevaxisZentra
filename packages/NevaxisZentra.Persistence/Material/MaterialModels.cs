namespace NevaxisZentra.Persistence.Material;

public sealed record MaterialOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class MaterialFailureCodes
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

public sealed class MaterialWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }

    public static MaterialWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static MaterialWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record MaterialListFilters(
    string? Search,
    string? LifecycleStatus,
    string? MaterialCategory,
    string? PolymerFamily,
    bool? IsActive,
    int Page,
    int PageSize);

public sealed record MaterialTechnicalSpecificationReadModel(
    decimal? DensityGPerCm3,
    decimal? MeltFlowRate,
    string? MeltFlowUnit,
    decimal? MeltFlowTestTemperatureC,
    decimal? MeltFlowTestLoadKg,
    decimal? MoistureLimitPercent,
    bool? DryingRequired,
    decimal? DryingTemperatureMinC,
    decimal? DryingTemperatureMaxC,
    decimal? DryingTimeMinHours,
    decimal? DryingTimeMaxHours,
    decimal? MeltTemperatureMinC,
    decimal? MeltTemperatureMaxC,
    decimal? MoldTemperatureMinC,
    decimal? MoldTemperatureMaxC,
    decimal? StorageTemperatureMinC,
    decimal? StorageTemperatureMaxC,
    decimal? MaximumRegrindPercent,
    string? TechnicalNotes,
    string? SafetyDataSheetReference,
    string? TechnicalDataSheetReference,
    string SpecificationSource,
    string? SpecificationSourceReference,
    DateTimeOffset? SpecificationRecordedAtUtc,
    bool HasTechnicalData,
    uint Version);

public sealed record MaterialSummaryReadModel(
    Guid Id,
    string Code,
    string Name,
    string MaterialCategory,
    string PolymerFamily,
    string? Manufacturer,
    string? TradeNameOrGrade,
    string? ColorName,
    string LifecycleStatus,
    bool IsActive,
    bool HasTechnicalData,
    Guid FacilityId,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MaterialDetailReadModel(
    Guid Id,
    Guid FacilityId,
    string Code,
    string Name,
    string? Description,
    string MaterialCategory,
    string PolymerFamily,
    string? Manufacturer,
    string? TradeNameOrGrade,
    string? ColorName,
    string? ColorCode,
    string PhysicalForm,
    string LifecycleStatus,
    string UnitOfMeasure,
    bool IsActive,
    string SourceType,
    string? SourceSystem,
    string? SourceReference,
    string? ExternalMaterialCode,
    DateTimeOffset? DataRecordedAtUtc,
    MaterialTechnicalSpecificationReadModel? TechnicalSpecification,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MaterialLifecycleEventReadModel(
    Guid Id,
    string PreviousStatus,
    string NewStatus,
    string Reason,
    string ActorDisplayName,
    DateTimeOffset OccurredAtUtc,
    string? SourceReference,
    uint Version);

public sealed record MaterialWorkspaceSummaryReadModel(
    int TotalCount,
    int ActiveCount,
    int BlockedCount,
    int ObsoleteCount,
    int MissingTechnicalDataCount);

public sealed record CreateMaterialCommand(
    string Code,
    string Name,
    string? Description,
    string MaterialCategory,
    string PolymerFamily,
    string? Manufacturer,
    string? TradeNameOrGrade,
    string? ColorName,
    string? ColorCode,
    string PhysicalForm,
    string UnitOfMeasure,
    bool IsActive,
    string SourceType,
    string? SourceSystem,
    string? SourceReference,
    string? ExternalMaterialCode,
    DateTimeOffset? DataRecordedAtUtc,
    string? IdempotencyKey);

public sealed record UpdateMaterialCommand(
    Guid MaterialId,
    uint ExpectedVersion,
    string Name,
    string? Description,
    string MaterialCategory,
    string PolymerFamily,
    string? Manufacturer,
    string? TradeNameOrGrade,
    string? ColorName,
    string? ColorCode,
    string PhysicalForm,
    string UnitOfMeasure,
    bool IsActive,
    string? SourceSystem,
    string? SourceReference,
    string? ExternalMaterialCode,
    DateTimeOffset? DataRecordedAtUtc);

public sealed record UpdateMaterialTechnicalSpecificationCommand(
    Guid MaterialId,
    uint ExpectedVersion,
    decimal? DensityGPerCm3,
    decimal? MeltFlowRate,
    string? MeltFlowUnit,
    decimal? MeltFlowTestTemperatureC,
    decimal? MeltFlowTestLoadKg,
    decimal? MoistureLimitPercent,
    bool? DryingRequired,
    decimal? DryingTemperatureMinC,
    decimal? DryingTemperatureMaxC,
    decimal? DryingTimeMinHours,
    decimal? DryingTimeMaxHours,
    decimal? MeltTemperatureMinC,
    decimal? MeltTemperatureMaxC,
    decimal? MoldTemperatureMinC,
    decimal? MoldTemperatureMaxC,
    decimal? StorageTemperatureMinC,
    decimal? StorageTemperatureMaxC,
    decimal? MaximumRegrindPercent,
    string? TechnicalNotes,
    string? SafetyDataSheetReference,
    string? TechnicalDataSheetReference,
    string SpecificationSource,
    string? SpecificationSourceReference,
    DateTimeOffset? SpecificationRecordedAtUtc);

public sealed record TransitionMaterialLifecycleCommand(
    Guid MaterialId,
    uint ExpectedVersion,
    string TargetStatus,
    string Reason,
    string? SourceReference,
    string IdempotencyKey);
