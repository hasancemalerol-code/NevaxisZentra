namespace NevaxisFactOS.Persistence.ShiftWorkforce;

public sealed class ShiftWorkforcePerson
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string SourceSystem { get; set; } = ShiftWorkforceSourceSystems.Unknown;
    public string ExternalEmployeeRef { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeCodeNormalized { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; } = true;
    public string AvailabilityState { get; set; } = ShiftWorkforceAvailabilityStates.Unknown;
    public string? SourceShiftReference { get; set; }
    public string? SourceCalendarReference { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset? SynchronizedAtUtc { get; set; }
    public string? SourceFingerprint { get; set; }
    public string FreshnessState { get; set; } = ShiftWorkforceFreshnessStates.Unknown;
    public string SourceLimitationsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public ICollection<ShiftWorkforceCompetencyAssignment> CompetencyAssignments { get; set; } = [];
}

public sealed class ShiftWorkforceCompetencyDefinition
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayLabelTr { get; set; } = string.Empty;
    public string? DescriptionTr { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<ShiftWorkforceCompetencyAssignment> Assignments { get; set; } = [];
}

public sealed class ShiftWorkforceCompetencyAssignment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid WorkforcePersonId { get; set; }
    public Guid CompetencyDefinitionId { get; set; }
    public string? MachineFamilyScope { get; set; }
    public string? ProcessScope { get; set; }
    public string? MaterialScope { get; set; }
    public string? EquipmentScope { get; set; }
    public string CompetencyLevel { get; set; } = ShiftWorkforceCompetencyLevels.Basic;
    public string VerificationStatus { get; set; } = ShiftWorkforceVerificationStatuses.PendingVerification;
    public DateTimeOffset? ValidFromUtc { get; set; }
    public DateTimeOffset? ValidUntilUtc { get; set; }
    public string? VerifiedBySubjectId { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string? OperationalNote { get; set; }
    public uint Version { get; set; } = 1;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ShiftWorkforcePerson? WorkforcePerson { get; set; }
    public ShiftWorkforceCompetencyDefinition? CompetencyDefinition { get; set; }
}

public sealed class ShiftWorkforceAuditEvent
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
