namespace CrevaFactOS.Persistence.ShiftRisk;

public sealed class ShiftCoverageRequirement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionShiftId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public DateTimeOffset PlannedStartUtc { get; set; }
    public DateTimeOffset PlannedEndUtc { get; set; }
    public string? ProductionOrderReference { get; set; }
    public Guid? MachineAssetId { get; set; }
    public string? MachineFamilyScope { get; set; }
    public string RequiredCompetencyCode { get; set; } = string.Empty;
    public int MinimumApprovedOperatorCount { get; set; }
    public bool IsCriticalRole { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class ShiftRiskAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssessmentType { get; set; } = ShiftRiskAssessmentTypes.Coverage;
    public Guid? WorkforcePersonId { get; set; }
    public Guid? CoverageRequirementId { get; set; }
    public Guid ProductionShiftId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public DateTimeOffset PlannedStartUtc { get; set; }
    public DateTimeOffset PlannedEndUtc { get; set; }
    public string? ProductionOrderReference { get; set; }
    public Guid? MachineAssetId { get; set; }
    public string? MachineFamilyScope { get; set; }
    public string? RequiredCompetencyCode { get; set; }
    public int? MinimumRequiredCount { get; set; }
    public bool? IsCriticalRole { get; set; }
    public int ApprovedEligibleCount { get; set; }
    public int ApprovedAssignmentCount { get; set; }
    public int PlannedAssignmentMinutes { get; set; }
    public int CoverageGap { get; set; }
    public string RuleSetVersion { get; set; } = ShiftRiskConstants.RuleSetVersion;
    public string InputFingerprint { get; set; } = string.Empty;
    public string Outcome { get; set; } = ShiftRiskOutcomes.InsufficientData;
    public string? LeadingDriverCategory { get; set; }
    public string? LeadingReasonCode { get; set; }
    public Guid? LeadingDriverId { get; set; }
    public string StructuredEvidenceJson { get; set; } = "{}";
    public string SourceFreshnessSnapshotJson { get; set; } = "{}";
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string? EvaluateIdempotencyKey { get; set; }
    public string? EvaluateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<ShiftRiskDriver> Drivers { get; set; } = [];
}

public sealed class ShiftRiskDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string LabelTr { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = ShiftRiskDriverSeverities.Info;
    public string? RequiredValue { get; set; }
    public string? ObservedValue { get; set; }
    public string? GapValue { get; set; }
    public Guid? ProductionShiftId { get; set; }
    public string? ProductionOrderReference { get; set; }
    public Guid? MachineAssetId { get; set; }
    public string? RequiredCompetencyCode { get; set; }
    public Guid? WorkforcePersonId { get; set; }
    public string EvidenceSource { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public DateTimeOffset? EvidenceTimestampUtc { get; set; }
    public string? FreshnessState { get; set; }
    public string RuleSetVersion { get; set; } = ShiftRiskConstants.RuleSetVersion;
    public bool IsLeadingRisk { get; set; }

    public ShiftRiskAssessment? Assessment { get; set; }
}

public sealed class ShiftRiskAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
