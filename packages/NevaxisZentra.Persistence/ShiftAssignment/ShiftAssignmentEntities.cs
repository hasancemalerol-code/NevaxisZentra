namespace NevaxisZentra.Persistence.ShiftAssignment;

public sealed class ShiftOperatorEligibilityAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProductionOrderReference { get; set; } = string.Empty;
    public Guid MachineAssetId { get; set; }
    public Guid ProductionShiftId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public DateTimeOffset PlannedStartUtc { get; set; }
    public DateTimeOffset PlannedEndUtc { get; set; }
    public string RequiredCompetencyCode { get; set; } = string.Empty;
    public string? MachineFamilyScope { get; set; }
    public string? ProcessScope { get; set; }
    public string RuleSetVersion { get; set; } = ShiftAssignmentConstants.RuleSetVersion;
    public string InputFingerprint { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<ShiftOperatorEligibilityResult> Results { get; set; } = [];
}

public sealed class ShiftOperatorEligibilityResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid WorkforcePersonId { get; set; }
    public string Outcome { get; set; } = ShiftAssignmentEligibilityOutcomes.InsufficientData;
    public string? PrimaryReasonCode { get; set; }
    public string EmployeeCodeSnapshot { get; set; } = string.Empty;
    public string DisplayNameSnapshot { get; set; } = string.Empty;

    public ShiftOperatorEligibilityAssessment? Assessment { get; set; }
    public ICollection<ShiftOperatorEligibilityReason> Reasons { get; set; } = [];
}

public sealed class ShiftOperatorEligibilityReason
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ResultId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string LabelTr { get; set; } = string.Empty;
    public string Severity { get; set; } = ShiftAssignmentReasonSeverities.Info;
    public string? RequiredValue { get; set; }
    public string? ObservedValue { get; set; }
    public string EvidenceSource { get; set; } = string.Empty;
    public DateTimeOffset? EvidenceTimestampUtc { get; set; }
    public string? FreshnessState { get; set; }

    public ShiftOperatorEligibilityResult? Result { get; set; }
}

public sealed class ShiftOperatorAssignment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProductionOrderReference { get; set; } = string.Empty;
    public Guid MachineAssetId { get; set; }
    public Guid ProductionShiftId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public Guid WorkforcePersonId { get; set; }
    public DateTimeOffset PlannedStartUtc { get; set; }
    public DateTimeOffset PlannedEndUtc { get; set; }
    public string Status { get; set; } = ShiftAssignmentStatuses.Draft;
    public Guid EligibilityAssessmentId { get; set; }
    public Guid EligibilityResultId { get; set; }
    public string? RequiredCompetencyCode { get; set; }
    public string? MachineFamilyScope { get; set; }
    public string? ProcessScope { get; set; }
    public string? ApprovedBySubjectId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string? CancelledBySubjectId { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public string RuleSetVersion { get; set; } = ShiftAssignmentConstants.RuleSetVersion;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public string? ApproveIdempotencyKey { get; set; }
    public string? ApproveIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class ShiftOperatorAssignmentAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = ShiftAssignmentEntityTypes.Assignment;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
