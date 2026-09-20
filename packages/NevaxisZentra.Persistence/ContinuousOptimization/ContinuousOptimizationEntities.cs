namespace NevaxisZentra.Persistence.ContinuousOptimization;

public interface IContinuousOptimizationTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class ContinuousOptimizationRunRecord : IContinuousOptimizationTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? LineId { get; set; }
    public string OptimizationType { get; set; } = ContinuousOptimizationTypes.Throughput;
    public string TriggerType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = ContinuousOptimizationPhases.Detected;
    public string Outcome { get; set; } = string.Empty;
    public string FailureState { get; set; } = string.Empty;
    public string CorrelationKey { get; set; } = string.Empty;
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? ThroughputOptimizerRunId { get; set; }
    public Guid? ScenarioRecoveryRunId { get; set; }
    public string QualificationJson { get; set; } = "{}";
    public string OpportunityJson { get; set; } = "{}";
    public string ExplainabilityJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
    public string EngineVersion { get; set; } = ContinuousOptimizationEngineVersions.Controller;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ContinuousOptimizationOpportunityRecord : IContinuousOptimizationTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RunId { get; set; }
    public string OpportunityType { get; set; } = string.Empty;
    public string ScopeEntityType { get; set; } = string.Empty;
    public string ScopeEntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public decimal EstimatedBenefit { get; set; }
    public decimal Confidence { get; set; }
    public string RiskClass { get; set; } = ContinuousOptimizationActionRiskClasses.Medium;
    public string EvidenceJson { get; set; } = "[]";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ContinuousOptimizationPolicyRecord : IContinuousOptimizationTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string OptimizationType { get; set; } = ContinuousOptimizationTypes.Throughput;
    public string ScopeType { get; set; } = "facility";
    public string ScopeId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsPaused { get; set; }
    public string ThresholdsJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
