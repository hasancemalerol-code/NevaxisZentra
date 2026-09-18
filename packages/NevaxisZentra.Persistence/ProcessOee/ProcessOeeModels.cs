namespace NevaxisZentra.Persistence.ProcessOee;

public sealed record ProcessOeeOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ProcessOeeWindowListFilters(
    Guid? MachineAssetId,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset? WindowStartFromUtc,
    DateTimeOffset? WindowEndToUtc,
    int Page,
    int PageSize);

public sealed record ProcessOeeAssessmentListFilters(
    Guid? MachineAssetId,
    string? ProductionOrderReference,
    string? MoldReference,
    string? Status,
    DateTimeOffset? EvaluatedFromUtc,
    DateTimeOffset? EvaluatedToUtc,
    int Page,
    int PageSize);

public sealed record ProcessOeeWindowSummaryReadModel(
    Guid Id,
    Guid MachineAssetId,
    string? MachineCode,
    string? MachineDisplayName,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    string SourceType,
    string? SourceReference,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record ProcessOeeWindowDetailReadModel(
    Guid Id,
    Guid MachineAssetId,
    string? MachineCode,
    string? MachineDisplayName,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record ProcessOeeAssessmentSummaryReadModel(
    Guid Id,
    Guid MachineAssetId,
    string? MachineCode,
    string? MachineDisplayName,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string Status,
    decimal? AvailabilityPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OeePercent,
    DateTimeOffset EvaluatedAtUtc);

public sealed record ProcessOeeAssessmentDetailReadModel(
    Guid Id,
    Guid EvaluationWindowId,
    Guid MachineAssetId,
    string? MachineCode,
    string? MachineDisplayName,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string Status,
    string RuleSetVersion,
    int? PlannedProductionSeconds,
    int? OperatingSeconds,
    int? DowntimeSeconds,
    int? UnknownSeconds,
    int? ObservedCycleCount,
    int? DerivedShotCount,
    int? TotalPieceCount,
    int? GoodPieceCount,
    int? ScrapPieceCount,
    decimal? AvailabilityPercent,
    decimal? PerformanceRawPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OeePercent,
    IReadOnlyList<ProcessOeeDriverReadModel> Drivers,
    IReadOnlyList<ProcessOeeEvidenceReadModel> Evidence,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Conflicts,
    string? SourceSystem,
    DateTimeOffset? SourceFreshnessUtc,
    bool IsSimulatorDerived,
    string CalculationHonestyDisclaimer,
    string? SimulatorHonestyDisclaimer,
    DateTimeOffset EvaluatedAtUtc);

public sealed record ProcessOeeDriverReadModel(
    string Code,
    string Label,
    string? Value,
    string Severity);

public sealed record ProcessOeeEvidenceReadModel(
    string Field,
    string? Value,
    string? Source,
    DateTimeOffset? RecordedAtUtc,
    bool IsMissing,
    bool IsConflict,
    string? ConflictReason);

public sealed record ProcessOeeWorkspaceSummaryReadModel(
    int TotalWindowCount,
    int AssessmentCount,
    int CalculatedCount,
    int InsufficientDataCount,
    int ReviewRequiredCount,
    int StaleDataCount,
    decimal? LatestOeePercent);

public sealed record CreateProcessOeeWindowCommand(
    Guid MachineAssetId,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string IdempotencyKey);

public sealed record UpdateProcessOeeWindowCommand(
    Guid WindowId,
    uint ExpectedVersion,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc);

public sealed record EvaluateProcessOeeWindowCommand(
    Guid EvaluationWindowId,
    string IdempotencyKey);

public sealed record EvaluateProcessOeeBoundedCommand(
    Guid MachineAssetId,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string IdempotencyKey);

public sealed class ProcessOeeWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProcessOeeWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new() { Succeeded = true, EntityId = entityId, Version = version, IdempotentReplay = idempotentReplay };

    public static ProcessOeeWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record ProcessOeeCalculationResult(
    string Status,
    int? PlannedProductionSeconds,
    int? OperatingSeconds,
    int? DowntimeSeconds,
    int? UnknownSeconds,
    int? ObservedCycleCount,
    int? DerivedShotCount,
    int? TotalPieceCount,
    int? GoodPieceCount,
    int? ScrapPieceCount,
    decimal? AvailabilityPercent,
    decimal? PerformanceRawPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OeePercent,
    IReadOnlyList<ProcessOeeDriverReadModel> Drivers,
    IReadOnlyList<ProcessOeeEvidenceReadModel> Evidence,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Conflicts,
    string? SourceSystem,
    DateTimeOffset? SourceFreshnessUtc,
    bool IsSimulatorDerived,
    string InputFingerprint);

public sealed record ProcessOeeEvidenceBundle(
    int WindowDurationSeconds,
    int? PlannedProductionSeconds,
    int? OperatingSeconds,
    int? DowntimeSeconds,
    int? UnknownSeconds,
    int? ObservedCycleCount,
    int? DerivedShotCount,
    int? TotalPieceCount,
    int? GoodPieceCount,
    int? ScrapPieceCount,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string? ProductionOrderReference,
    string? MoldReference,
    string? SourceSystem,
    DateTimeOffset? SourceFreshnessUtc,
    bool IsSimulatorDerived,
    bool HasEvidenceConflict,
    IReadOnlyList<ProcessOeeEvidenceReadModel> Evidence,
    IReadOnlyList<string> Conflicts);

public sealed record ProcessOeeLossDriverReadModel(
    Guid Id,
    string ReasonCode,
    string Category,
    string Severity,
    int OperationalPriority,
    decimal? ObservedValue,
    decimal? ExpectedValue,
    decimal? AbsoluteLossValue,
    int? AffectedDurationSeconds,
    int? AffectedQuantity,
    IReadOnlyList<string> SourceReferences,
    DateTimeOffset? EvidenceTimestampUtc,
    string FreshnessState,
    string ExplanationCode,
    IReadOnlyList<string> DataLimitations,
    string RuleSetVersion,
    bool IsLeadingLoss,
    decimal? NormalizedImpactPoints);

public sealed record ProcessOeeLossOverviewReadModel(
    int AssessmentCountWithLossDrivers,
    int AvailabilityLossCount,
    int PerformanceLossCount,
    int QualityLossCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    string RuleSetVersion,
    string LeadingLossMethod);

public sealed record ProcessOeeLossDetailReadModel(
    Guid AssessmentId,
    Guid EvaluationWindowId,
    string AssessmentStatus,
    string RuleSetVersion,
    string LeadingLossMethod,
    ProcessOeeLossDriverReadModel? LeadingLoss,
    IReadOnlyList<ProcessOeeLossDriverReadModel> Drivers,
    IReadOnlyList<ProcessOeeEvidenceReadModel> Evidence,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Conflicts,
    string? SourceSystem,
    DateTimeOffset? SourceFreshnessUtc,
    bool IsSimulatorDerived,
    string CalculationHonestyDisclaimer,
    string? SimulatorHonestyDisclaimer);

public sealed record ProcessOeeStabilityDriverReadModel(
    Guid Id,
    string ReasonCode,
    string Category,
    string ResultState,
    string Severity,
    int OperationalPriority,
    decimal? ObservedValue,
    decimal? ReferenceValue,
    string Unit,
    int SampleCount,
    DateTimeOffset TimeRangeStartUtc,
    DateTimeOffset TimeRangeEndUtc,
    IReadOnlyList<string> SourceReferences,
    string FreshnessState,
    string ExplanationCode,
    IReadOnlyList<string> DataLimitations,
    string RuleSetVersion);

public sealed record ProcessOeeDowntimeBreakdownReadModel(
    string ReasonCode,
    int DurationSeconds,
    int OccurrenceCount,
    int SortRank,
    bool IsClassified,
    decimal? DurationPercent);

public sealed record ProcessOeeStabilityOverviewReadModel(
    int AssessmentCountWithStability,
    int StableCount,
    int VariableCount,
    int DriftingCount,
    int ReviewRequiredCount,
    int InsufficientDataCount,
    string RuleSetVersion);

public sealed record ProcessOeeStabilityDetailReadModel(
    Guid AssessmentId,
    Guid EvaluationWindowId,
    string AssessmentStatus,
    string ResultState,
    string RuleSetVersion,
    long? MedianCycleMs,
    long? MadCycleMs,
    int SampleCount,
    string DriftDirection,
    int MicroStopCount,
    int MicroStopTotalSeconds,
    int MicroStopMedianSeconds,
    int ClassifiedDowntimeSeconds,
    int UnclassifiedDowntimeSeconds,
    IReadOnlyList<ProcessOeeStabilityDriverReadModel> Drivers,
    IReadOnlyList<ProcessOeeDowntimeBreakdownReadModel> DowntimeBreakdown,
    IReadOnlyList<ProcessOeeEvidenceReadModel> Evidence,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Conflicts,
    string? SourceSystem,
    DateTimeOffset? SourceFreshnessUtc,
    bool IsSimulatorDerived,
    string CalculationHonestyDisclaimer,
    string? SimulatorHonestyDisclaimer);

public sealed record ProcessOeeDowntimeParetoReadModel(
    Guid AssessmentId,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int TotalDowntimeSeconds,
    int ClassifiedDurationSeconds,
    int UnclassifiedDurationSeconds,
    string RuleSetVersion,
    IReadOnlyList<ProcessOeeDowntimeBreakdownReadModel> Rows);

public sealed record ProcessOeeShiftComparisonSummaryReadModel(
    string? HighestOeeShiftLabel,
    string? LowestAvailabilityShiftLabel,
    string? HighestDowntimeShiftLabel,
    string? HighestVariabilityShiftLabel);

public sealed record ProcessOeeShiftComparisonRowReadModel(
    Guid AssessmentId,
    string ShiftCode,
    string ShiftLabel,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    int? OperatingSeconds,
    decimal? AvailabilityPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OeePercent,
    string? StabilityResultState,
    int? DowntimeSeconds,
    int MicroStopCount,
    string DataSufficiency,
    string FreshnessState,
    bool IsComparable);

public sealed record ProcessOeeShiftComparisonReadModel(
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    bool IsComparable,
    string? ComparabilityWarningCode,
    string RuleSetVersion,
    ProcessOeeShiftComparisonSummaryReadModel? Summary,
    IReadOnlyList<ProcessOeeShiftComparisonRowReadModel> Rows);

public sealed record EvaluateProcessOeeShiftComparisonCommand(
    Guid? MachineAssetId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string IdempotencyKey);
