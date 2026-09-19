namespace NevaxisZentra.Persistence.ScenarioRecovery;

public interface IScenarioRecoveryTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class ScenarioRecoveryRunRecord : IScenarioRecoveryTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    public string CorrelationKey { get; set; } = string.Empty;
    public string Category { get; set; } = ScenarioRecoveryCategories.ProductionRecovery;
    public string EntryPoint { get; set; } = ScenarioRecoveryEntryPoints.Direct;
    public string CurrentPhase { get; set; } = ScenarioRecoveryPhases.Draft;
    public string ExecutionState { get; set; } = ScenarioRecoveryExecutionStates.Simulated;
    public string ProblemJson { get; set; } = "{}";
    public string ObjectivesJson { get; set; } = "[]";
    public string SnapshotSummaryJson { get; set; } = "{}";
    public string ComparisonJson { get; set; } = "{}";
    public string SelectedOptionLabel { get; set; } = string.Empty;
    public string RecoveryPlanJson { get; set; } = "{}";
    public string VerificationJson { get; set; } = "{}";
    public string SourceEntityType { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string EngineVersion { get; set; } = ScenarioRecoveryEngineVersions.Orchestration;
    public string SchemaVersion { get; set; } = "1.0.0";
    public long ConcurrencyToken { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ScenarioRecoveryOptionRecord : IScenarioRecoveryTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RunId { get; set; }
    public string OptionLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FeasibilityState { get; set; } = ScenarioRecoveryFeasibilityStates.Feasible;
    public string ActionsJson { get; set; } = "[]";
    public string SimulationJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "[]";
    public string AssumptionsJson { get; set; } = "[]";
    public bool RequiresApproval { get; set; }
    public bool IsDominated { get; set; }
    public decimal RecommendationScore { get; set; }
    public string FeasibilityReasonsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ScenarioRecoveryPredictionAccuracyRecord : IScenarioRecoveryTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RunId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public string MetricCode { get; set; } = "GOOD_THROUGHPUT";
    public decimal PredictedValue { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal? PredictionError { get; set; }
    public string VerificationOutcome { get; set; } = string.Empty;
    public string EngineVersion { get; set; } = ScenarioRecoveryEngineVersions.Orchestration;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
