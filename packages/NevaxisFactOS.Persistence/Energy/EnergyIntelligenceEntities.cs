namespace NevaxisFactOS.Persistence.Energy;

public sealed class EnergyBaselineProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string BaselineReference { get; set; } = string.Empty;
    public string ComparisonDimension { get; set; } = string.Empty;
    public Guid? MeasurementPointId { get; set; }
    public Guid? MachineAssetId { get; set; }
    public string? ShiftReference { get; set; }
    public string? ProductionContextReference { get; set; }
    public int ReportingWindowDurationSeconds { get; set; }
    public int SampleCount { get; set; }
    public decimal BaselineConsumedKwh { get; set; }
    public decimal? BaselineMaximumDemandKw { get; set; }
    public decimal? BaselineKwhPerKg { get; set; }
    public decimal? BaselineKwhPer1000Parts { get; set; }
    public decimal? BaselineKwhPer1000GoodParts { get; set; }
    public decimal MedianCenter { get; set; }
    public decimal MadDispersion { get; set; }
    public decimal NormalizedDispersion { get; set; }
    public decimal RelativeTolerance { get; set; }
    public decimal UpperThresholdConsumedKwh { get; set; }
    public decimal? UpperThresholdDemandKw { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = EnergyEvidenceStates.InsufficientData;
    public string SourceWindowReferencesJson { get; set; } = "[]";
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ValidToUtc { get; set; }
    public string RuleSetVersion { get; set; } = EnergyIntelligenceConstants.RuleSetVersion;
    public string Provenance { get; set; } = string.Empty;
    public string? EvaluateIdempotencyKey { get; set; }
    public string? EvaluateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class EnergyTarget
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string TargetReference { get; set; } = string.Empty;
    public string TargetScope { get; set; } = string.Empty;
    public string ScopeReference { get; set; } = string.Empty;
    public string TargetMetric { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Tolerance { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset EffectiveToUtc { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public string Status { get; set; } = EnergyTargetStatuses.Approved;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class EnergyIntelligenceAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssessmentReference { get; set; } = string.Empty;
    public string ComparisonDimension { get; set; } = string.Empty;
    public Guid? MeasurementPointId { get; set; }
    public Guid? ConsumptionWindowId { get; set; }
    public Guid? BaselineProfileId { get; set; }
    public Guid? TargetId { get; set; }
    public DateTimeOffset EvaluationWindowStartUtc { get; set; }
    public DateTimeOffset EvaluationWindowEndUtc { get; set; }
    public decimal? ActualConsumedKwh { get; set; }
    public decimal? ActualMaximumDemandKw { get; set; }
    public decimal? ActualKwhPer1000GoodParts { get; set; }
    public decimal? VarianceConsumedKwh { get; set; }
    public decimal? VariancePercent { get; set; }
    public decimal? PotentialReducibleKwh { get; set; }
    public decimal? EstimatedCostImpact { get; set; }
    public string? CostCurrencyCode { get; set; }
    public string? OpportunityLabel { get; set; }
    public string Outcome { get; set; } = EnergyIntelligenceOutcomes.InsufficientData;
    public string? LeadingDriverCode { get; set; }
    public Guid? LeadingDriverId { get; set; }
    public string RuleSetVersion { get; set; } = EnergyIntelligenceConstants.RuleSetVersion;
    public string CalculationMethod { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = EnergyEvidenceStates.InsufficientData;
    public string FreshnessState { get; set; } = EnergyFreshnessStates.Unknown;
    public string? EvaluateIdempotencyKey { get; set; }
    public string? EvaluateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<EnergyIntelligenceDriver> Drivers { get; set; } = [];
}

public sealed class EnergyIntelligenceDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string LabelTr { get; set; } = string.Empty;
    public string Severity { get; set; } = EnergyIntelligenceDriverSeverities.Info;
    public string Category { get; set; } = string.Empty;
    public string? ActualValue { get; set; }
    public string? ActualUnit { get; set; }
    public string? ComparisonValue { get; set; }
    public string? ComparisonUnit { get; set; }
    public decimal? NormalizedVariance { get; set; }
    public string StructuredEvidenceJson { get; set; } = "{}";
    public string EvidenceSource { get; set; } = string.Empty;
    public string? FreshnessState { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = EnergyIntelligenceConstants.RuleSetVersion;
    public bool IsLeadingDriver { get; set; }

    public EnergyIntelligenceAssessment? Assessment { get; set; }
}
