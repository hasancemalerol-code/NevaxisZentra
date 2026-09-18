namespace NevaxisZentra.Persistence.ThroughputIntelligence;

public interface IThroughputIntelligenceTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class LineThroughputTarget : IThroughputIntelligenceTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public decimal TargetGoodUnitsPerHour { get; set; }
    public int? RequiredGoodQuantity { get; set; }
    public decimal? AvailableProductionHours { get; set; }
    public decimal? TargetCycleSeconds { get; set; }
    public string? OrderReference { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OperatorSkillProfile : IThroughputIntelligenceTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SkillLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class OperatorStationQualification : IThroughputIntelligenceTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public Guid StationId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string QualificationStatus { get; set; } = "qualified";
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ThroughputScenarioDefinition : IThroughputIntelligenceTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public string ScenarioCode { get; set; } = string.Empty;
    public string ScenarioType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AnalysisWindow { get; set; } = ThroughputIntelligenceAnalysisWindows.Recent;
    public DateTimeOffset? WindowStartUtc { get; set; }
    public DateTimeOffset? WindowEndUtc { get; set; }
    public string InterventionsJson { get; set; } = "[]";
    public string AssumptionsJson { get; set; } = "[]";
    public string GuardrailsJson { get; set; } = "[]";
    public string RequestedOutputsJson { get; set; } = "[]";
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ThroughputScenarioResult : IThroughputIntelligenceTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ScenarioDefinitionId { get; set; }
    public string Eligibility { get; set; } = ThroughputIntelligenceScenarioEligibility.Eligible;
    public string DataQuality { get; set; } = ThroughputIntelligenceDataQualityStates.Good;
    public string ResultJson { get; set; } = "{}";
    public string EngineVersion { get; set; } = ThroughputIntelligenceEngineVersions.Scenario;
    public int EventsConsidered { get; set; }
    public int StationsEvaluated { get; set; }
    public long AnalysisDurationMs { get; set; }
    public DateTimeOffset ComputedAtUtc { get; set; }
}
