namespace NevaxisFactOS.Persistence.AdaptiveProductionPlanning;

public sealed class ProductionPlanVersion
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanId { get; set; }
    public Guid? PreviousVersionId { get; set; }
    public int VersionNumber { get; set; }
    public string PlanningHorizon { get; set; } = PlanningHorizons.Operational;
    public string LifecycleStatus { get; set; } = AdaptivePlanLifecycleStatuses.Draft;
    public string ObjectiveProfileJson { get; set; } = "{}";
    public Guid? ConstraintSnapshotId { get; set; }
    public Guid? DecisionId { get; set; }
    public decimal Score { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset? SupersededAtUtc { get; set; }
    public string ChangeSummaryJson { get; set; } = "{}";
    public string ExpectedImpactJson { get; set; } = "{}";
    public string ActualImpactJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class ProductionPlanOperation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanVersionId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string OperationCode { get; set; } = string.Empty;
    public string? MachineId { get; set; }
    public string? LineId { get; set; }
    public string? MoldId { get; set; }
    public string? ShiftId { get; set; }
    public DateTimeOffset PlannedStartUtc { get; set; }
    public DateTimeOffset PlannedEndUtc { get; set; }
    public decimal SetupMinutes { get; set; }
    public string MaterialReadinessState { get; set; } = "ready";
    public int SequenceRank { get; set; }
    public string Status { get; set; } = "planned";
    public string EvidenceJson { get; set; } = "[]";
}

public sealed class PlanningConstraintSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string PlanningHorizon { get; set; } = PlanningHorizons.Operational;
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string OrdersJson { get; set; } = "[]";
    public string MachinesJson { get; set; } = "[]";
    public string MaterialsJson { get; set; } = "[]";
    public string MoldsJson { get; set; } = "[]";
    public string MaintenanceJson { get; set; } = "[]";
    public string QualityJson { get; set; } = "[]";
    public string ShiftsJson { get; set; } = "[]";
    public string CapacityJson { get; set; } = "{}";
    public string IncidentsJson { get; set; } = "[]";
    public string FreshnessJson { get; set; } = "{}";
    public string HardConstraintsJson { get; set; } = "[]";
    public string SoftConstraintsJson { get; set; } = "[]";
}

public sealed class PlanningScenario
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanId { get; set; }
    public Guid? PlanVersionId { get; set; }
    public Guid ConstraintSnapshotId { get; set; }
    public string ScenarioCode { get; set; } = "A";
    public string Title { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public int Rank { get; set; }
    public string ComparisonJson { get; set; } = "{}";
    public string Status { get; set; } = "proposed";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PlanningScenarioMetric
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ScenarioId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string DataClassification { get; set; } = PlanningDataClassifications.Derived;
}

public sealed class PlanDeviation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanId { get; set; }
    public Guid? PlanVersionId { get; set; }
    public string DeviationType { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium";
    public DateTimeOffset DetectedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
}

public sealed class PlanChange
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanId { get; set; }
    public Guid PlanVersionId { get; set; }
    public Guid? FromVersionId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string InitiatedBySubjectId { get; set; } = string.Empty;
    public string DiffJson { get; set; } = "{}";
    public string ReasonJson { get; set; } = "{}";
    public string ExpectedBenefitJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AdaptivePlanningPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public int FrozenWindowMinutes { get; set; } = 60;
    public int NervousnessCooldownMinutes { get; set; } = 30;
    public decimal MinBenefitThreshold { get; set; } = 0.05m;
    public string AutonomyMode { get; set; } = PlanningAutonomyModes.Draft;
    public string PolicyEnvelopeJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class PlanActivityEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
