namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public interface IThroughputOptimizerTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class ThroughputOptimizerRunRecord : IThroughputOptimizerTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public Guid DigitalWorkCaseId { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    public string CorrelationKey { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = ThroughputOptimizerPhases.Observe;
    public string ObservationJson { get; set; } = "{}";
    public string DiagnosisJson { get; set; } = "{}";
    public string SimulationJson { get; set; } = "[]";
    public string SelectionJson { get; set; } = "{}";
    public string ActionPlanJson { get; set; } = "{}";
    public string ModelIntelligencePath { get; set; } = string.Empty;
    public string EngineVersion { get; set; } = ThroughputOptimizerEngineVersions.Observe;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class VerifiedImprovementRecord : IThroughputOptimizerTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DigitalWorkCaseId { get; set; }
    public Guid ThroughputOptimizerRunId { get; set; }
    public string VerificationOutcome { get; set; } = string.Empty;
    public string BaselineJson { get; set; } = "{}";
    public decimal TargetImprovementPerHour { get; set; }
    public string InterventionSummary { get; set; } = string.Empty;
    public string ActualJson { get; set; } = "{}";
    public bool QualityGuardPassed { get; set; }
    public bool SafetyGuardPassed { get; set; }
    public DateTimeOffset? ObservationStartUtc { get; set; }
    public DateTimeOffset? ObservationEndUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string EngineVersion { get; set; } = ThroughputOptimizerEngineVersions.VerifiedImprovement;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class VerifiedTrainingDatasetCandidateRecord : IThroughputOptimizerTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DigitalWorkCaseId { get; set; }
    public Guid VerifiedImprovementId { get; set; }
    public string EligibilityStatus { get; set; } = VerifiedDatasetEligibilityStatuses.PendingReview;
    public string IneligibilityReason { get; set; } = string.Empty;
    public string RecordJson { get; set; } = "{}";
    public string TenantPolicyJson { get; set; } = "{}";
    public string VerificationLevel { get; set; } = string.Empty;
    public string DataQualityScore { get; set; } = string.Empty;
    public bool AnonymizationEligible { get; set; }
    public bool ExportEligible { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
