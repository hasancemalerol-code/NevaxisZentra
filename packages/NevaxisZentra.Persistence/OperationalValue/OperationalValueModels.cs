namespace NevaxisZentra.Persistence.OperationalValue;

public sealed record OperationalValueBaselineSnapshot(
    decimal Value,
    string Unit,
    string Source,
    string Window,
    string Method);

public sealed record OperationalValueActualSnapshot(
    decimal Value,
    string Unit,
    string Source);

public sealed record OperationalValueCalculationRequest(
    Guid? DigitalWorkCaseId,
    Guid? OptimizationRunId,
    Guid? ScenarioRunId,
    Guid? VerifiedImprovementId,
    string VerificationOutcome,
    string ValueCategory,
    OperationalValueBaselineSnapshot Baseline,
    OperationalValueActualSnapshot Actual,
    decimal ObservationWindowHours,
    string AttributionClass,
    string WorkflowType,
    string? LineCode,
    string? ScopeEntityType,
    string? ScopeEntityId,
    int HumanTouchesBefore,
    int HumanTouchesAfter,
    string? OverlapGroupKey,
    decimal? IntelligenceCost);

public sealed record OperationalValueCalculationResult(
    bool Eligible,
    string? FailureState,
    string Status,
    decimal DeltaValue,
    string Unit,
    decimal ConfidenceScore,
    string EconomicConversionStatus,
    decimal? EconomicValue,
    string? Currency,
    decimal? IntelligenceCost,
    decimal? NetValue,
    decimal? RoiPercent,
    decimal? PaybackDays,
    string FormulaType,
    string FormulaVersion,
    string InputVersion,
    string EvidenceSummary);

public sealed record TenantEconomicInputSnapshot(
    Guid ConfigurationId,
    string VersionCode,
    string Currency,
    decimal? UnitContributionValue,
    decimal? ScrapUnitCost,
    decimal? DowntimeCostPerHour,
    decimal? LaborCostPerHour,
    decimal? ChangeoverCostPerMinute,
    decimal? LateDeliveryPenalty,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertTenantEconomicInputRequest(
    string VersionCode,
    string Currency,
    decimal? UnitContributionValue,
    decimal? ScrapUnitCost,
    decimal? DowntimeCostPerHour,
    decimal? LaborCostPerHour,
    decimal? ChangeoverCostPerMinute,
    decimal? LateDeliveryPenalty);

public sealed record VerifiedOperationalValueSummary(
    Guid ValueRecordId,
    Guid? DigitalWorkCaseId,
    string ValueCategory,
    string Status,
    string AttributionClass,
    decimal DeltaValue,
    string Unit,
    decimal? EconomicValue,
    string? Currency,
    decimal? NetValue,
    decimal ConfidenceScore,
    string? LineCode,
    string WorkflowType,
    DateTimeOffset CreatedAtUtc);

public sealed record OperationalValueCaseDetail(
    Guid ValueRecordId,
    Guid? DigitalWorkCaseId,
    string ProblemSummary,
    OperationalValueBaselineSnapshot Baseline,
    OperationalValueActualSnapshot Actual,
    string VerificationOutcome,
    decimal DeltaValue,
    string Unit,
    string Status,
    string AttributionClass,
    decimal ConfidenceScore,
    string EconomicConversionStatus,
    decimal? EconomicValue,
    string? Currency,
    decimal? IntelligenceCost,
    decimal? NetValue,
    decimal? RoiPercent,
    decimal? PaybackDays,
    string FormulaType,
    string FormulaVersion,
    IReadOnlyList<string> EvidenceRefs,
    DateTimeOffset CreatedAtUtc);

public sealed record OperationalValueTrendPoint(
    string PeriodLabel,
    int VerifiedOutcomeCount,
    decimal GoodOutputGainUnits,
    decimal DowntimeAvoidedMinutes,
    decimal? VerifiedEconomicValue,
    decimal IntelligenceCost,
    decimal? CostPerVerifiedOutcome);

public sealed record OperationalValueBreakdownItem(
    string DimensionType,
    string DimensionKey,
    string Label,
    int VerifiedCount,
    decimal OperationalDeltaTotal,
    string Unit,
    decimal? EconomicValueTotal,
    decimal IntelligenceCostTotal);

public sealed record OperationalValueControlCenterSummary(
    int VerifiedOutcomes,
    decimal GoodOutputGainUnits,
    decimal DowntimeAvoidedMinutes,
    int LateOrdersProtected,
    int HumanTouchesReduced,
    decimal IntelligenceCost,
    decimal? VerifiedEconomicValue,
    decimal? CostPerVerifiedOutcome,
    decimal? NetValue,
    decimal? RoiPercent,
    string RoiAvailability,
    decimal AutonomousCompletionRate,
    decimal AverageHumanTouchesPerCase,
    decimal AverageTimeToVerifiedOutcomeHours);

public sealed record OperationalValueControlCenter(
    OperationalValueControlCenterSummary Summary,
    IReadOnlyList<OperationalValueTrendPoint> Trend,
    IReadOnlyList<OperationalValueBreakdownItem> Breakdown,
    IReadOnlyList<VerifiedOperationalValueSummary> RecentValues,
    TenantEconomicInputSnapshot? EconomicInputs,
    DateTimeOffset GeneratedAtUtc);

public sealed record OperationalValueCustomerReportPreview(
    string PeriodLabel,
    int VerifiedInterventions,
    decimal GoodOutputGainUnits,
    decimal DowntimeAvoidedMinutes,
    int LateOrdersProtected,
    decimal? VerifiedEconomicValue,
    string? Currency,
    decimal IntelligenceCost,
    decimal? NetValue,
    decimal? RoiPercent,
    string MethodologySummary,
    IReadOnlyList<string> EvidenceReferences);

public sealed record OperationalValueGoldenValueResult(
    Guid ValueRecordId,
    decimal GoodOutputGainUnits,
    decimal ThroughputGainPerHour,
    decimal IntelligenceCost,
    decimal? CostPerVerifiedOutcome,
    decimal? EconomicValue,
    decimal? NetValue,
    string RoiAvailability,
    string Message);

public sealed record OperationalValueGoldenDoubleCountResult(
    bool DoubleCountBlocked,
    int RecordsCreated,
    string Message);

public sealed record OperationalValueGoldenUnverifiedResult(
    bool TrustedValueCreated,
    string Message);

public sealed record OperationalValueGoldenInvalidationResult(
    Guid ValueRecordId,
    string PreviousStatus,
    string NewStatus,
    string Message);

public sealed record OperationalValueKpiSnapshot(
    decimal AutonomousCompletionRate,
    decimal AverageHumanTouchesPerCase,
    decimal AverageTimeToVerifiedOutcomeHours,
    decimal OperationalValuePerVerifiedCase);
