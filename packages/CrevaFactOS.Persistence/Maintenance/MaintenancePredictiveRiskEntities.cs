namespace CrevaFactOS.Persistence.Maintenance;

public static class MaintenancePredictiveRiskRuleSetVersions
{
    public const string V1 = "MAINT-PRED-RISK-V1";
}

public static class MaintenancePredictiveRiskBands
{
    public const string InsufficientData = "insufficient-data";
    public const string Low = "low";
    public const string Watch = "watch";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class MaintenancePredictiveReviewPriorities
{
    public const string Routine = "routine";
    public const string Elevated = "elevated";
    public const string Planned = "planned";
    public const string Urgent = "urgent";
}

public static class MaintenancePredictiveRecommendationCategories
{
    public const string ReviewDataQuality = "review-data-quality";
    public const string ContinueRoutineMonitoring = "continue-routine-monitoring";
    public const string CloseMonitoring = "close-monitoring";
    public const string PlanTechnicalReview = "plan-technical-review";
    public const string UrgentAuthorizedTechnicalReview = "urgent-authorized-technical-review";
}

public static class MaintenancePredictiveRecommendationStatuses
{
    public const string Current = "current";
    public const string Superseded = "superseded";
}

public static class MaintenancePredictiveRiskCreationSources
{
    public const string DeterministicEvaluator = "deterministic-evaluator";
}

public sealed class MaintenancePredictiveRiskAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public Guid MachineId { get; set; }
    public string RiskBand { get; set; } = MaintenancePredictiveRiskBands.InsufficientData;
    public string ReviewPriority { get; set; } = MaintenancePredictiveReviewPriorities.Routine;
    public string RuleSetVersion { get; set; } = MaintenancePredictiveRiskRuleSetVersions.V1;
    public string? ConditionEvaluationIdentity { get; set; }
    public string DataFreshness { get; set; } = MaintenanceConditionDataFreshnessLevels.Unknown;
    public string DataSufficiency { get; set; } = MaintenanceConditionDataSufficiencyLevels.Insufficient;
    public int ActiveAnomalyCount { get; set; }
    public string MaintenanceHistorySummary { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string CreationSource { get; set; } = MaintenancePredictiveRiskCreationSources.DeterministicEvaluator;
    public DateTimeOffset AssessedAtUtc { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? LinkedAiTaskId { get; set; }
    public string FollowUpStatus { get; set; } = MaintenancePredictiveRiskFollowUpStatuses.Active;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public ICollection<MaintenancePredictiveRiskDriver> Drivers { get; set; } = [];
    public ICollection<MaintenancePredictiveRiskEvidence> EvidenceItems { get; set; } = [];
}

public sealed class MaintenancePredictiveRiskDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string DriverCode { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public MaintenancePredictiveRiskAssessment? Assessment { get; set; }
}

public sealed class MaintenancePredictiveRiskEvidence
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public MaintenancePredictiveRiskAssessment? Assessment { get; set; }
}

public sealed class MaintenancePredictiveRecommendation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public Guid AssessmentId { get; set; }
    public string Category { get; set; } = MaintenancePredictiveRecommendationCategories.ReviewDataQuality;
    public string ReviewPriority { get; set; } = MaintenancePredictiveReviewPriorities.Routine;
    public string Explanation { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = MaintenancePredictiveRiskRuleSetVersions.V1;
    public string Status { get; set; } = MaintenancePredictiveRecommendationStatuses.Current;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public MaintenancePredictiveRiskAssessment? Assessment { get; set; }
}

public sealed class MaintenancePredictiveRiskAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public MaintenancePredictiveRiskAssessment? Assessment { get; set; }
}
