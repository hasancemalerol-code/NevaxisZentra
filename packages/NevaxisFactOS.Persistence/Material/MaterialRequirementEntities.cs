namespace NevaxisFactOS.Persistence.Material;

public enum MaterialRequirementType
{
    ProductionOrder,
    Trial,
    Setup,
    ManualPlanning,
    Other,
}

public enum MaterialConsumptionObservationType
{
    ConsumptionObserved,
    SetupConsumptionObserved,
    TrialConsumptionObserved,
    ScrapObserved,
    RegrindUseObserved,
    CorrectionObserved,
}

public enum MaterialRequirementAssessmentType
{
    Requirement,
    ConsumptionVariance,
    RegrindPolicy,
    AvailabilityRisk,
}

public sealed class MaterialRequirementProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialId { get; set; }
    public string? ProductionOrderReference { get; set; }
    public string? ProductReference { get; set; }
    public string? MoldReference { get; set; }
    public MaterialRequirementType RequirementType { get; set; } = MaterialRequirementType.ManualPlanning;
    public decimal? PlannedProductionQuantity { get; set; }
    public decimal? GoodPartTargetQuantity { get; set; }
    public decimal? PartWeightGrams { get; set; }
    public decimal? RunnerWeightGramsPerShot { get; set; }
    public int? EffectiveCavityCount { get; set; }
    public decimal? SetupAllowanceGrams { get; set; }
    public decimal? ProcessLossAllowancePercent { get; set; }
    public decimal? PlannedVirginPercent { get; set; }
    public decimal? PlannedRegrindPercent { get; set; }
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialRegistryEntry? Material { get; set; }
}

public sealed class MaterialConsumptionObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? MaterialLotId { get; set; }
    public Guid? RequirementProfileId { get; set; }
    public string? ProductionOrderReference { get; set; }
    public string? ProductReference { get; set; }
    public string? MoldReference { get; set; }
    public MaterialConsumptionObservationType ObservationType { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "kg";
    public decimal? VirginQuantity { get; set; }
    public decimal? RegrindQuantity { get; set; }
    public decimal? GoodPartQuantity { get; set; }
    public decimal? ScrapQuantity { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public string? Notes { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? PayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialRegistryEntry? Material { get; set; }
    public MaterialLot? MaterialLot { get; set; }
    public MaterialRequirementProfile? RequirementProfile { get; set; }
}

public sealed class MaterialRequirementAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? RequirementProfileId { get; set; }
    public MaterialRequirementAssessmentType AssessmentType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = string.Empty;
    public string InputFingerprint { get; set; } = string.Empty;
    public string StructuredDrivers { get; set; } = "{}";
    public string StructuredEvidence { get; set; } = "{}";
    public decimal? TheoreticalRequirementGrams { get; set; }
    public decimal? ObservedConsumptionGrams { get; set; }
    public decimal? VarianceGrams { get; set; }
    public decimal? VariancePercent { get; set; }
    public decimal? VirginRequirementGrams { get; set; }
    public decimal? RegrindRequirementGrams { get; set; }
    public decimal? ErpAvailableQuantity { get; set; }
    public decimal? ErpReservedQuantity { get; set; }
    public string? ErpUnit { get; set; }
    public decimal? AvailableAfterReservationQuantity { get; set; }
    public decimal? ShortageQuantity { get; set; }
    public DateTimeOffset? ErpSourceUpdatedAtUtc { get; set; }
    public string? SourceFreshness { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public Guid? LinkedAiTaskId { get; set; }
    public string FollowUpStatus { get; set; } = MaterialAvailabilityRiskFollowUpStatuses.RequiresReview;
    public string? FollowUpUpdatedBy { get; set; }

    public MaterialRegistryEntry? Material { get; set; }
    public MaterialRequirementProfile? RequirementProfile { get; set; }
}

public sealed class MaterialAvailabilityRiskFollowUpAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public MaterialRequirementAssessment? Assessment { get; set; }
}
