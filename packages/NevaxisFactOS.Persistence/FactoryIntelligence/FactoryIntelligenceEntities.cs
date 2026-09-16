namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public sealed class FactoryPredictiveSignal
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string ObservationWindow { get; set; } = "30m";
    public decimal MeasuredValue { get; set; }
    public decimal? BaselineValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public string Trend { get; set; } = "stable";
    public string DataQuality { get; set; } = FactoryIntelligenceDataQualityStates.Good;
    public string SourceReferencesJson { get; set; } = "[]";
    public string EngineVersion { get; set; } = FactoryIntelligenceEngineVersions.RiskEngine;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class FactoryOperationalBaseline
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public decimal ReferenceValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public string Trend { get; set; } = "stable";
    public string DataQuality { get; set; } = FactoryIntelligenceDataQualityStates.Good;
    public string BaselineSource { get; set; } = "recent-stable-window";
    public string EngineVersion { get; set; } = FactoryIntelligenceEngineVersions.BaselineEngine;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class FactoryPredictedRisk
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string RiskType { get; set; } = string.Empty;
    public string RiskState { get; set; } = FactoryIntelligenceRiskStates.Watch;
    public string AffectedOutcome { get; set; } = string.Empty;
    public string ConfidenceBand { get; set; } = FactoryIntelligenceConfidenceBands.Medium;
    public string Horizon { get; set; } = FactoryIntelligencePredictionHorizons.ShortTerm;
    public string Severity { get; set; } = "medium";
    public string? TimeToImpactLabel { get; set; }
    public int? ProjectedGapQuantity { get; set; }
    public string? OrderReference { get; set; }
    public string? MachineCode { get; set; }
    public string? MaterialCode { get; set; }
    public string ContributingSignalsJson { get; set; } = "[]";
    public string ExplanationJson { get; set; } = "[]";
    public string AssumptionsJson { get; set; } = "[]";
    public string DataQuality { get; set; } = FactoryIntelligenceDataQualityStates.Good;
    public string DataClassification { get; set; } = FactoryIntelligenceDataClassifications.Predicted;
    public string CorrelationKey { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? LinkedCaseId { get; set; }
    public string EngineVersion { get; set; } = FactoryIntelligenceEngineVersions.RiskEngine;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FactoryPredictionEvaluation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PredictedRiskId { get; set; }
    public string EvaluationOutcome { get; set; } = FactoryIntelligenceEvaluationOutcomes.Pending;
    public string PredictedOutcome { get; set; } = string.Empty;
    public string? ActualOutcome { get; set; }
    public string Horizon { get; set; } = string.Empty;
    public string ConfidenceBand { get; set; } = string.Empty;
    public bool InterventionOccurred { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
}
