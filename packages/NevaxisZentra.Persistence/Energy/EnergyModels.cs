namespace NevaxisZentra.Persistence.Energy;

public sealed record EnergyOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record EnergyWorkspaceReadModel(
    int ActiveMeasurementPointCount,
    int RecentWindowCount,
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    bool HasDevelopmentPilotData);

public sealed record EnergyMeasurementPointReadModel(
    Guid Id,
    string PointCode,
    string DisplayName,
    string MeasurementScope,
    Guid? MachineAssetId,
    string? MachineAssetLabel,
    string? EdgeAssetReference,
    string? EquipmentCategory,
    string MeasurementUnit,
    string Status,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record EnergyMeasurementPointListReadModel(
    IReadOnlyList<EnergyMeasurementPointReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EnergyMeasurementPointListFilters(
    string? MeasurementScope,
    string? EquipmentCategory,
    Guid? MachineAssetId,
    bool? IsActive,
    int Page,
    int PageSize);

public sealed record EnergyConsumptionWindowReadModel(
    Guid Id,
    Guid MeasurementPointId,
    string MeasurementPointCode,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    decimal ConsumedKwh,
    decimal? AveragePowerKw,
    decimal? MaximumDemandKw,
    string CalculationMethod,
    string EvidenceStatus,
    int ReadingCount,
    string FreshnessState,
    string RuleSetVersion,
    decimal? CostAmount,
    string? CostCurrencyCode,
    string? MissingDataExplanation,
    DateTimeOffset CalculatedAtUtc,
    bool IsPilotData);

public sealed record EnergyConsumptionListReadModel(
    IReadOnlyList<EnergyConsumptionWindowReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EnergyConsumptionListFilters(
    string? MeasurementPointReference,
    DateTimeOffset? WindowStartUtc,
    DateTimeOffset? WindowEndUtc,
    string? EvidenceStatus,
    int Page,
    int PageSize);

public sealed record EnergyAllocationReadModel(
    Guid Id,
    Guid ConsumptionWindowId,
    string TargetType,
    string? TargetReference,
    decimal AllocatedKwh,
    string AllocationMethod,
    string EvidenceSource,
    string ConfidenceStatus,
    string RuleSetVersion,
    DateTimeOffset CalculatedAtUtc);

public sealed record EnergyAllocationListReadModel(
    IReadOnlyList<EnergyAllocationReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EnergyAllocationListFilters(
    Guid? ConsumptionWindowId,
    string? TargetType,
    string? TargetReference,
    int Page,
    int PageSize);

public sealed record IngestEnergyReadingCommand(
    string MeasurementPointReference,
    DateTimeOffset OccurredAtUtc,
    decimal? CumulativeEnergyKwh,
    decimal? IntervalEnergyKwh,
    decimal? ActivePowerKw,
    decimal? MaximumDemandKw,
    string SourceType,
    string? SourceReference,
    string? FreshnessState,
    string? EvidenceState,
    string? IdempotencyKey);

public sealed record EvaluateEnergyConsumptionCommand(
    string MeasurementPointReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    Guid? TariffSnapshotId,
    IReadOnlyList<EnergyAllocationInput>? Allocations,
    string? IdempotencyKey);

public sealed record EnergyEvaluateResultReadModel(
    Guid ConsumptionWindowId,
    decimal ConsumedKwh,
    decimal? AveragePowerKw,
    decimal? MaximumDemandKw,
    string CalculationMethod,
    string EvidenceStatus,
    int ReadingCount,
    decimal? CostAmount,
    string? CostCurrencyCode,
    string? MissingDataExplanation,
    IReadOnlyList<EnergyAllocationReadModel> Allocations,
    IReadOnlyList<EnergyIntensityMetricReadModel> IntensityMetrics,
    string RuleSetVersion,
    bool IdempotentReplay);

public sealed record EnergyWriteResult(
    bool Succeeded,
    Guid? EntityId,
    uint? Version,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason)
{
    public static EnergyWriteResult Success(Guid entityId, uint? version = null, bool idempotentReplay = false) =>
        new(true, entityId, version, idempotentReplay, null, null);

    public static EnergyWriteResult Failure(string code, string? reason) =>
        new(false, null, null, false, code, reason);
}
