namespace NevaxisZentra.Persistence.ShiftWorkforce;

public sealed record ShiftWorkforceOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ShiftWorkforcePersonListFilters(
    string? Search,
    bool? IsActive,
    string? Availability,
    string? CompetencyCode,
    string? Freshness,
    int Page,
    int PageSize);

public sealed record ShiftWorkforceOverviewReadModel(
    int TotalWorkforce,
    int ActiveWorkforce,
    int CurrentlyAvailable,
    int UnavailableOrLeave,
    int StaleSourceRecords,
    int CompetencyAttentionCount);

public sealed record ShiftWorkforcePersonSummaryReadModel(
    Guid Id,
    string EmployeeCode,
    string DisplayName,
    string? Department,
    string? JobTitle,
    bool IsActive,
    string AvailabilityState,
    string? ShiftLabel,
    string SourceSystem,
    string FreshnessState,
    string CompetencySummary,
    bool CompetencyAttention);

public sealed record ShiftWorkforcePersonDetailReadModel(
    Guid Id,
    string EmployeeCode,
    string DisplayName,
    string? Department,
    string? JobTitle,
    bool IsActive,
    string AvailabilityState,
    string? ShiftLabel,
    string SourceSystem,
    string FreshnessState,
    string? SourceShiftReference,
    string? SourceCalendarReference,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset? SynchronizedAtUtc,
    string? SourceFingerprint,
    IReadOnlyList<string> SourceLimitations,
    bool IsPilotData,
    IReadOnlyList<ShiftWorkforceCompetencyAssignmentReadModel> Competencies,
    uint Version);

public sealed record ShiftWorkforceCompetencyDefinitionReadModel(
    string Code,
    string DisplayLabelTr,
    string? DescriptionTr,
    bool IsActive,
    int SortOrder);

public sealed record ShiftWorkforceCompetencyAssignmentReadModel(
    Guid Id,
    string CompetencyCode,
    string CompetencyLabel,
    string? MachineFamilyScope,
    string? ProcessScope,
    string? MaterialScope,
    string? EquipmentScope,
    string CompetencyLevel,
    string VerificationStatus,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    DateTimeOffset? VerifiedAtUtc,
    string? OperationalNote,
    uint Version,
    bool RequiresAttention);

public sealed record ShiftWorkforcePersonListReadModel(
    IReadOnlyList<ShiftWorkforcePersonSummaryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CreateShiftWorkforceCompetencyCommand(
    string CompetencyDefinitionCode,
    string? MachineFamilyScope,
    string? ProcessScope,
    string? MaterialScope,
    string? EquipmentScope,
    string CompetencyLevel,
    string VerificationStatus,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    string? OperationalNote,
    string IdempotencyKey);

public sealed record UpdateShiftWorkforceCompetencyCommand(
    Guid AssignmentId,
    uint ExpectedVersion,
    string? CompetencyLevel,
    string? VerificationStatus,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    string? OperationalNote);

public sealed record ShiftWorkforceSynchronizeResult(
    int CreatedCount,
    int UpdatedCount,
    int UnchangedCount);

public static class ShiftWorkforceFailureCodes
{
    public const string InvalidScope = "shift-workforce.invalid-scope";
    public const string NotFound = "shift-workforce.not-found";
    public const string ValidationFailed = "shift-workforce.validation-failed";
    public const string ConcurrencyConflict = "shift-workforce.concurrency-conflict";
    public const string IdempotencyConflict = "shift-workforce.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "shift-workforce.idempotency-legacy-unverifiable";
    public const string PersonInactive = "shift-workforce.person-inactive";
    public const string CompetencyDefinitionNotFound = "shift-workforce.competency-definition-not-found";
}

public sealed class ShiftWorkforceWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ShiftWorkforceWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static ShiftWorkforceWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
