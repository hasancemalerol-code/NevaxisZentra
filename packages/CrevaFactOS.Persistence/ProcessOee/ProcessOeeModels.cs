namespace CrevaFactOS.Persistence.ProcessOee;

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
