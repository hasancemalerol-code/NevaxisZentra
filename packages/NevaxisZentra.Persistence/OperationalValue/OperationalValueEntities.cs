namespace NevaxisZentra.Persistence.OperationalValue;

public interface IOperationalValueTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class VerifiedOperationalValueRecord : IOperationalValueTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? OptimizationRunId { get; set; }
    public Guid? ScenarioRunId { get; set; }
    public Guid? VerifiedImprovementId { get; set; }
    public string ValueCategory { get; set; } = string.Empty;
    public string Status { get; set; } = OperationalValueStatuses.Predicted;
    public string AttributionClass { get; set; } = OperationalValueAttributionClasses.Unattributed;
    public decimal BaselineValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal DeltaValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string BaselineSource { get; set; } = string.Empty;
    public string BaselineWindow { get; set; } = string.Empty;
    public string BaselineMethod { get; set; } = string.Empty;
    public decimal ObservationWindowHours { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public string FormulaType { get; set; } = string.Empty;
    public string FormulaVersion { get; set; } = "1.0";
    public string InputVersion { get; set; } = string.Empty;
    public string EconomicConversionStatus { get; set; } = OperationalValueEconomicConversionStatuses.NotConfigured;
    public decimal? EconomicValue { get; set; }
    public string? Currency { get; set; }
    public decimal? IntelligenceCost { get; set; }
    public decimal? NetValue { get; set; }
    public decimal? RoiPercent { get; set; }
    public decimal? PaybackDays { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string? LineCode { get; set; }
    public string? ScopeEntityType { get; set; }
    public string? ScopeEntityId { get; set; }
    public string? OverlapGroupKey { get; set; }
    public int HumanTouchesBefore { get; set; }
    public int HumanTouchesAfter { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public string ProvenanceJson { get; set; } = "{}";
    public bool IsInvalidated { get; set; }
    public string InvalidationReason { get; set; } = string.Empty;
    public DateTimeOffset? InvalidatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class TenantEconomicInputConfigurationRecord : IOperationalValueTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal? UnitContributionValue { get; set; }
    public decimal? ScrapUnitCost { get; set; }
    public decimal? DowntimeCostPerHour { get; set; }
    public decimal? LaborCostPerHour { get; set; }
    public decimal? ChangeoverCostPerMinute { get; set; }
    public decimal? LateDeliveryPenalty { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OperationalValuePeriodSummaryRecord : IOperationalValueTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string PeriodType { get; set; } = "day";
    public DateTimeOffset PeriodStartUtc { get; set; }
    public DateTimeOffset PeriodEndUtc { get; set; }
    public int VerifiedOutcomeCount { get; set; }
    public decimal GoodOutputGainUnits { get; set; }
    public decimal DowntimeAvoidedMinutes { get; set; }
    public int LateOrdersProtected { get; set; }
    public int HumanTouchesReduced { get; set; }
    public decimal IntelligenceCost { get; set; }
    public decimal? VerifiedEconomicValue { get; set; }
    public decimal? NetValue { get; set; }
    public string SummaryJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
