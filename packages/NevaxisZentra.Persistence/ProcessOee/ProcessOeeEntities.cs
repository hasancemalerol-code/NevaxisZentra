namespace NevaxisZentra.Persistence.ProcessOee;

public sealed class ProcessOeeEvaluationWindow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineAssetId { get; set; }
    public string? ProductionOrderReference { get; set; }
    public string? MoldReference { get; set; }
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public int? PlannedProductionSeconds { get; set; }
    public decimal? IdealCycleTimeSeconds { get; set; }
    public int? EffectiveCavityCount { get; set; }
    public string SourceType { get; set; } = ProcessOeeSourceTypes.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
}

public sealed class ProcessOeeAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineAssetId { get; set; }
    public string? ProductionOrderReference { get; set; }
    public string? MoldReference { get; set; }
    public Guid EvaluationWindowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = ProcessOeeConstants.RuleSetVersion;
    public string InputFingerprint { get; set; } = string.Empty;
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public int? PlannedProductionSeconds { get; set; }
    public int? OperatingSeconds { get; set; }
    public int? DowntimeSeconds { get; set; }
    public int? UnknownSeconds { get; set; }
    public int? ObservedCycleCount { get; set; }
    public int? DerivedShotCount { get; set; }
    public int? TotalPieceCount { get; set; }
    public int? GoodPieceCount { get; set; }
    public int? ScrapPieceCount { get; set; }
    public decimal? AvailabilityPercent { get; set; }
    public decimal? PerformanceRawPercent { get; set; }
    public decimal? PerformancePercent { get; set; }
    public decimal? QualityPercent { get; set; }
    public decimal? OeePercent { get; set; }
    public string StructuredDrivers { get; set; } = "[]";
    public string StructuredEvidence { get; set; } = "{}";
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public ProcessOeeEvaluationWindow? EvaluationWindow { get; set; }
    public ICollection<ProcessOeeLossDriver> LossDrivers { get; set; } = [];
    public ProcessOeeStabilityAssessment? StabilityAssessment { get; set; }
    public ICollection<ProcessOeeStabilityDriver> StabilityDrivers { get; set; } = [];
    public ICollection<ProcessOeeDowntimeBreakdownRow> DowntimeBreakdownRows { get; set; } = [];
}

public sealed class ProcessOeeLossDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int OperationalPriority { get; set; }
    public decimal? ObservedValue { get; set; }
    public decimal? ExpectedValue { get; set; }
    public decimal? AbsoluteLossValue { get; set; }
    public int? AffectedDurationSeconds { get; set; }
    public int? AffectedQuantity { get; set; }
    public string SourceReferencesJson { get; set; } = "[]";
    public DateTimeOffset? EvidenceTimestampUtc { get; set; }
    public string FreshnessState { get; set; } = ProcessOeeLossFreshnessStates.Unknown;
    public string ExplanationCode { get; set; } = string.Empty;
    public string DataLimitationsJson { get; set; } = "[]";
    public string RuleSetVersion { get; set; } = ProcessOeeLossConstants.RuleSetVersion;
    public bool IsLeadingLoss { get; set; }
    public decimal? NormalizedImpactPoints { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public ProcessOeeAssessment? Assessment { get; set; }
}

public sealed class ProcessOeeStabilityAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string ResultState { get; set; } = string.Empty;
    public long? MedianCycleMs { get; set; }
    public long? MadCycleMs { get; set; }
    public int SampleCount { get; set; }
    public string DriftDirection { get; set; } = ProcessOeeStabilityDriftDirections.None;
    public int MicroStopCount { get; set; }
    public int MicroStopTotalSeconds { get; set; }
    public int MicroStopMedianSeconds { get; set; }
    public int ClassifiedDowntimeSeconds { get; set; }
    public int UnclassifiedDowntimeSeconds { get; set; }
    public string EvidenceFingerprint { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = ProcessOeeStabilityConstants.RuleSetVersion;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public ProcessOeeAssessment? Assessment { get; set; }
    public ICollection<ProcessOeeStabilityDriver> Drivers { get; set; } = [];
    public ICollection<ProcessOeeDowntimeBreakdownRow> DowntimeBreakdownRows { get; set; } = [];
}

public sealed class ProcessOeeStabilityDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid StabilityAssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ResultState { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int OperationalPriority { get; set; }
    public decimal? ObservedValue { get; set; }
    public decimal? ReferenceValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public DateTimeOffset TimeRangeStartUtc { get; set; }
    public DateTimeOffset TimeRangeEndUtc { get; set; }
    public string SourceReferencesJson { get; set; } = "[]";
    public string FreshnessState { get; set; } = ProcessOeeStabilityFreshnessStates.Unknown;
    public string ExplanationCode { get; set; } = string.Empty;
    public string DataLimitationsJson { get; set; } = "[]";
    public string RuleSetVersion { get; set; } = ProcessOeeStabilityConstants.RuleSetVersion;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public ProcessOeeAssessment? Assessment { get; set; }
    public ProcessOeeStabilityAssessment? StabilityAssessment { get; set; }
}

public sealed class ProcessOeeDowntimeBreakdownRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid StabilityAssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public int OccurrenceCount { get; set; }
    public int SortRank { get; set; }
    public bool IsClassified { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public ProcessOeeAssessment? Assessment { get; set; }
    public ProcessOeeStabilityAssessment? StabilityAssessment { get; set; }
}

public sealed class ProcessOeeShiftComparisonBatch
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? MachineAssetId { get; set; }
    public DateTimeOffset ComparisonPeriodStartUtc { get; set; }
    public DateTimeOffset ComparisonPeriodEndUtc { get; set; }
    public bool IsComparable { get; set; }
    public string? ComparabilityWarningCode { get; set; }
    public string SummaryJson { get; set; } = "{}";
    public string EvidenceFingerprint { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = ProcessOeeStabilityConstants.RuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public ICollection<ProcessOeeShiftComparisonRow> Rows { get; set; } = [];
}

public sealed class ProcessOeeShiftComparisonRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid BatchId { get; set; }
    public Guid AssessmentId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftLabel { get; set; } = string.Empty;
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public int? PlannedProductionSeconds { get; set; }
    public int? OperatingSeconds { get; set; }
    public decimal? AvailabilityPercent { get; set; }
    public decimal? PerformancePercent { get; set; }
    public decimal? QualityPercent { get; set; }
    public decimal? OeePercent { get; set; }
    public string? StabilityResultState { get; set; }
    public int? DowntimeSeconds { get; set; }
    public int MicroStopCount { get; set; }
    public string DataSufficiency { get; set; } = string.Empty;
    public string FreshnessState { get; set; } = ProcessOeeStabilityFreshnessStates.Unknown;
    public bool IsComparable { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public ProcessOeeShiftComparisonBatch? Batch { get; set; }
}
