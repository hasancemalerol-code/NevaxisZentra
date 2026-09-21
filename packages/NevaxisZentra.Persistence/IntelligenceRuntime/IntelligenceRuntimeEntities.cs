namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public interface IIntelligenceRuntimeTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class IntelligenceRuntimeRouteRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? WorkerId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? OptimizationRunId { get; set; }
    public Guid? ScenarioRunId { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    public Guid? ModelGatewayAuditId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string RequestClassification { get; set; } = string.Empty;
    public string RouteType { get; set; } = string.Empty;
    public string IntelligencePath { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string RoutingReason { get; set; } = string.Empty;
    public string WhyModelCalledOrSkipped { get; set; } = string.Empty;
    public bool ModelInvoked { get; set; }
    public bool UsedFallback { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public string PriceVersion { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public string EngineVersion { get; set; } = IntelligenceRuntimeEngineVersions.Controller;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ProviderPriceCatalogRecord
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public decimal InputPricePer1K { get; set; }
    public decimal OutputPricePer1K { get; set; }
    public decimal? CachedInputPricePer1K { get; set; }
    public string Currency { get; set; } = "USD";
    public string PriceVersion { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TenantIntelligenceLearningPolicyRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string LearningPolicy { get; set; } = IntelligenceTenantLearningPolicies.NoModelTraining;
    public bool AllowExport { get; set; }
    public bool AllowTraining { get; set; }
    public bool AllowShadow { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class NevaxisVerifiedManufacturingDatasetRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "1.0";
    public string ReadinessState { get; set; } = NevaxisDatasetReadinessStates.NotReady;
    public string TenantPolicy { get; set; } = string.Empty;
    public string SanitizationVersion { get; set; } = "1.0";
    public string PolicyVersion { get; set; } = "1.0";
    public int IncludedCaseCount { get; set; }
    public int ExcludedCaseCount { get; set; }
    public string ManifestJson { get; set; } = "{}";
    public string QualityMetricsJson { get; set; } = "{}";
    public string TaskDistributionJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class NevaxisVerifiedManufacturingDatasetEntryRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DatasetVersionId { get; set; }
    public Guid SourceCaseId { get; set; }
    public Guid? VerifiedImprovementId { get; set; }
    public bool IsNegativeLearning { get; set; }
    public string RecordJson { get; set; } = "{}";
    public string ProvenanceJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class NevaxisModelRegistryRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ModelCode { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string BaseModel { get; set; } = string.Empty;
    public string Status { get; set; } = NevaxisModelStatuses.Candidate;
    public string EvaluationStatus { get; set; } = string.Empty;
    public string SafetyStatus { get; set; } = string.Empty;
    public string DeploymentType { get; set; } = "candidate";
    public int ContextLimit { get; set; }
    public string TaskCapabilitiesJson { get; set; } = "[]";
    public Guid? TrainingDatasetVersionId { get; set; }
    public Guid? EvaluationDatasetVersionId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? PromotedAtUtc { get; set; }
    public DateTimeOffset? RetiredAtUtc { get; set; }
}

public sealed class NevaxisModelTrainingJobRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ModelId { get; set; }
    public Guid DatasetVersionId { get; set; }
    public string Status { get; set; } = NevaxisModelTrainingJobStatuses.Created;
    public string Method { get; set; } = "lora-foundation";
    public string ArtifactMetadataJson { get; set; } = "{}";
    public string FailureReason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class NevaxisModelEvaluationRunRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ModelId { get; set; }
    public Guid? EvaluationDatasetVersionId { get; set; }
    public string EvaluationMode { get; set; } = "candidate-vs-production";
    public string Outcome { get; set; } = string.Empty;
    public decimal StructuredOutputValidityRate { get; set; }
    public decimal SafetyPassRate { get; set; }
    public decimal HallucinatedEntityRate { get; set; }
    public decimal UnsupportedClaimRate { get; set; }
    public decimal EstimatedCost { get; set; }
    public long LatencyMs { get; set; }
    public string MetricsJson { get; set; } = "{}";
    public string SafetyBenchmarkJson { get; set; } = "{}";
    public bool IsShadowMode { get; set; }
    public bool ExecutedOperationally { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class VerifiedOutcomeIntelligenceCostRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? OptimizationRunId { get; set; }
    public Guid? ScenarioRunId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public decimal TotalIntelligenceCost { get; set; }
    public int ModelCallCount { get; set; }
    public int DeterministicRouteCount { get; set; }
    public decimal? CostPerVerifiedOutcome { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class IntelligenceRuntimeBudgetPolicyRecord : IIntelligenceRuntimeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ScopeType { get; set; } = "facility";
    public string ScopeId { get; set; } = string.Empty;
    public int MaxModelCalls { get; set; } = 100;
    public int MaxContextTokens { get; set; } = 16000;
    public int MaxOutputTokens { get; set; } = 4000;
    public decimal MaxEstimatedCost { get; set; } = 25m;
    public int MaxRetries { get; set; } = 2;
    public int MaxSimulationCount { get; set; } = 5;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
