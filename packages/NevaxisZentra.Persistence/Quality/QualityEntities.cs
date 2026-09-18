namespace NevaxisZentra.Persistence.Quality;

public sealed class QualityControlPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProductReference { get; set; }
    public string? ProductGroup { get; set; }
    public string? OperationReference { get; set; }
    public QualityControlPlanStatus Status { get; set; } = QualityControlPlanStatus.Draft;
    public string Revision { get; set; } = "1";
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public ICollection<QualityInspectionDefinition> InspectionDefinitions { get; set; } = [];
}

public sealed class QualityInspectionDefinition
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid QualityControlPlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QualityInspectionType InspectionType { get; set; } = QualityInspectionType.Measurement;
    public int Sequence { get; set; }
    public string? Unit { get; set; }
    public decimal? NominalValue { get; set; }
    public decimal? LowerLimit { get; set; }
    public decimal? UpperLimit { get; set; }
    public bool Required { get; set; } = true;
    public bool Critical { get; set; }
    public string? Instruction { get; set; }
    public uint Version { get; set; } = 1;

    public QualityControlPlan? QualityControlPlan { get; set; }
}

public sealed class QualityInspection
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid QualityControlPlanId { get; set; }
    public string? PlanCodeSnapshot { get; set; }
    public string? PlanNameSnapshot { get; set; }
    public string? PlanRevisionSnapshot { get; set; }
    public string InspectionNumber { get; set; } = string.Empty;
    public QualityInspectionStatus Status { get; set; } = QualityInspectionStatus.Draft;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? InspectorSubjectId { get; set; }
    public QualityInspectionOverallResult OverallResult { get; set; } = QualityInspectionOverallResult.Pending;
    public QualityInspectionReleaseStatus ReleaseStatus { get; set; } = QualityInspectionReleaseStatus.NotReady;
    public string? Notes { get; set; }
    public string? ReleaseNote { get; set; }
    public string? ReleasedBySubjectId { get; set; }
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public ICollection<QualityInspectionResult> Results { get; set; } = [];
}

public sealed class QualityInspectionResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid QualityInspectionId { get; set; }
    public Guid InspectionDefinitionId { get; set; }
    public string? DefinitionCodeSnapshot { get; set; }
    public string? DefinitionNameSnapshot { get; set; }
    public string? InspectionTypeSnapshot { get; set; }
    public string? UnitSnapshot { get; set; }
    public decimal? NominalValueSnapshot { get; set; }
    public decimal? LowerLimitSnapshot { get; set; }
    public decimal? UpperLimitSnapshot { get; set; }
    public bool? RequiredSnapshot { get; set; }
    public bool? CriticalSnapshot { get; set; }
    public decimal? MeasuredNumericValue { get; set; }
    public string? MeasuredTextValue { get; set; }
    public bool? BooleanResult { get; set; }
    public QualityInspectionItemResult Result { get; set; } = QualityInspectionItemResult.Pending;
    public string? Comment { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public string RecordedBySubjectId { get; set; } = string.Empty;
    public string Source { get; set; } = "manual";
    public uint Version { get; set; } = 1;
}
