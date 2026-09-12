namespace NevaxisFactOS.Persistence.Energy;

public sealed class EnergyMeasurementPoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string PointCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string MeasurementScope { get; set; } = EnergyMeasurementScopes.Machine;
    public Guid? MachineAssetId { get; set; }
    public string? EdgeAssetReference { get; set; }
    public string? EquipmentCategory { get; set; }
    public string MeasurementUnit { get; set; } = "kWh";
    public string Status { get; set; } = EnergyMeasurementPointStatuses.Active;
    public string SourceProvenance { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class EnergyReading
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MeasurementPointId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public decimal? CumulativeEnergyKwh { get; set; }
    public decimal? IntervalEnergyKwh { get; set; }
    public decimal? ActivePowerKw { get; set; }
    public decimal? MaximumDemandKw { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public string FreshnessState { get; set; } = EnergyFreshnessStates.Unknown;
    public string EvidenceState { get; set; } = EnergyEvidenceStates.Measured;
    public string? IdempotencyKey { get; set; }
    public string? PayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public EnergyMeasurementPoint? MeasurementPoint { get; set; }
}

public sealed class EnergyConsumptionWindow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MeasurementPointId { get; set; }
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public decimal ConsumedKwh { get; set; }
    public decimal? AveragePowerKw { get; set; }
    public decimal? MaximumDemandKw { get; set; }
    public string CalculationMethod { get; set; } = EnergyCalculationMethods.Unavailable;
    public string EvidenceStatus { get; set; } = EnergyEvidenceStates.InsufficientData;
    public int ReadingCount { get; set; }
    public string FreshnessState { get; set; } = EnergyFreshnessStates.Unknown;
    public string SourceReferencesJson { get; set; } = "[]";
    public string RuleSetVersion { get; set; } = EnergyConstants.RuleSetVersion;
    public decimal? CostAmount { get; set; }
    public string? CostCurrencyCode { get; set; }
    public Guid? TariffSnapshotId { get; set; }
    public string? MissingDataExplanation { get; set; }
    public string? EvaluateIdempotencyKey { get; set; }
    public string? EvaluateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CalculatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<EnergyAllocation> Allocations { get; set; } = [];
}

public sealed class EnergyAllocation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConsumptionWindowId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string? TargetReference { get; set; }
    public decimal AllocatedKwh { get; set; }
    public string AllocationMethod { get; set; } = string.Empty;
    public string EvidenceSource { get; set; } = string.Empty;
    public string ConfidenceStatus { get; set; } = EnergyEvidenceStates.Calculated;
    public string RuleSetVersion { get; set; } = EnergyConstants.RuleSetVersion;
    public DateTimeOffset CalculatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public EnergyConsumptionWindow? ConsumptionWindow { get; set; }
}

public sealed class EnergyTariffSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ValidToUtc { get; set; }
    public decimal EnergyUnitPrice { get; set; }
    public decimal? DemandUnitPrice { get; set; }
    public string Provenance { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class EnergyAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
