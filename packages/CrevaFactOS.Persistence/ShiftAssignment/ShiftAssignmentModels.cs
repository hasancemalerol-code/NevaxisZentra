namespace CrevaFactOS.Persistence.ShiftAssignment;

public sealed record ShiftAssignmentEvaluationContextBuildResult(
    bool Succeeded,
    ShiftAssignmentEvaluationContext? Context,
    string? FailureCode,
    string? FailureReason);

public sealed record ShiftAssignmentOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ShiftAssignmentEvaluationContext(
    string ProductionOrderReference,
    Guid MachineAssetId,
    Guid ProductionShiftId,
    DateOnly ShiftDate,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string RequiredCompetencyCode,
    string? MachineFamilyScope,
    string? ProcessScope,
    string ShiftCode);

public sealed record ShiftAssignmentEligibilityReasonReadModel(
    string ReasonCode,
    string LabelTr,
    string Severity,
    string? RequiredValue,
    string? ObservedValue,
    string EvidenceSource,
    DateTimeOffset? EvidenceTimestampUtc,
    string? FreshnessState);

public sealed record ShiftAssignmentCandidateSummaryReadModel(
    Guid PersonId,
    Guid EligibilityResultId,
    string EmployeeCode,
    string DisplayName,
    string? Department,
    string? JobTitle,
    string AvailabilityState,
    string CompetencySummary,
    string FreshnessState,
    string Outcome,
    string? PrimaryReasonCode,
    bool HasOverlapWarning,
    bool IsPilotData);

public sealed record ShiftAssignmentCandidateDetailReadModel(
    Guid PersonId,
    Guid EligibilityResultId,
    string EmployeeCode,
    string DisplayName,
    string? Department,
    string? JobTitle,
    string AvailabilityState,
    string CompetencySummary,
    string FreshnessState,
    string Outcome,
    string? PrimaryReasonCode,
    bool HasOverlapWarning,
    bool IsPilotData,
    string SourceSystem,
    DateTimeOffset? SourceUpdatedAtUtc,
    string? ShiftLabel,
    Guid AssessmentId,
    string RuleSetVersion,
    IReadOnlyList<ShiftAssignmentEligibilityReasonReadModel> Reasons);

public sealed record ShiftAssignmentEligibilitySummaryReadModel(
    int EligibleCount,
    int ConditionalCount,
    int IneligibleCount,
    int InsufficientDataCount);

public sealed record ShiftAssignmentCandidatesReadModel(
    Guid AssessmentId,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    ShiftAssignmentEligibilitySummaryReadModel Summary,
    IReadOnlyList<ShiftAssignmentCandidateSummaryReadModel> Candidates);

public sealed record ShiftAssignmentListFilters(
    string? ProductionOrderReference,
    string? Status,
    int Page,
    int PageSize);

public sealed record ShiftAssignmentRecordReadModel(
    Guid Id,
    string ProductionOrderReference,
    string MachineAssetLabel,
    string ShiftLabel,
    DateOnly ShiftDate,
    string EmployeeCode,
    string DisplayName,
    string Status,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string? ApprovedBySubjectId,
    DateTimeOffset? ApprovedAtUtc,
    string? CancelledBySubjectId,
    DateTimeOffset? CancelledAtUtc,
    string RuleSetVersion,
    uint Version,
    bool IsPilotData);

public sealed record ShiftAssignmentDetailReadModel(
    Guid Id,
    string ProductionOrderReference,
    Guid MachineAssetId,
    Guid ProductionShiftId,
    string MachineAssetLabel,
    string ShiftLabel,
    DateOnly ShiftDate,
    Guid WorkforcePersonId,
    string EmployeeCode,
    string DisplayName,
    string? Department,
    string? JobTitle,
    string Status,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    Guid EligibilityAssessmentId,
    Guid EligibilityResultId,
    string? RequiredCompetencyCode,
    string? MachineFamilyScope,
    string? ProcessScope,
    string? ApprovedBySubjectId,
    DateTimeOffset? ApprovedAtUtc,
    string? CancelledBySubjectId,
    DateTimeOffset? CancelledAtUtc,
    string RuleSetVersion,
    uint Version,
    bool IsPilotData);

public sealed record ShiftAssignmentListReadModel(
    IReadOnlyList<ShiftAssignmentRecordReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ShiftAssignmentAuditEventReadModel(
    Guid Id,
    string EventType,
    string ActorSubjectId,
    DateTimeOffset OccurredAtUtc,
    string Summary);

public sealed record CreateShiftOperatorAssignmentCommand(
    string ProductionOrderReference,
    Guid MachineAssetId,
    Guid ProductionShiftId,
    DateOnly ShiftDate,
    Guid WorkforcePersonId,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    Guid EligibilityAssessmentId,
    Guid EligibilityResultId,
    string RequiredCompetencyCode,
    string? MachineFamilyScope,
    string? ProcessScope,
    string IdempotencyKey);

public sealed record ApproveShiftOperatorAssignmentCommand(
    uint ExpectedVersion,
    string IdempotencyKey);

public sealed record CancelShiftOperatorAssignmentCommand(
    uint ExpectedVersion,
    string? CancellationReason);

public static class ShiftAssignmentFailureCodes
{
    public const string InvalidScope = "shift-assign.invalid-scope";
    public const string NotFound = "shift-assign.not-found";
    public const string ValidationFailed = "shift-assign.validation-failed";
    public const string ConcurrencyConflict = "shift-assign.concurrency-conflict";
    public const string IdempotencyConflict = "shift-assign.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "shift-assign.idempotency-legacy-unverifiable";
    public const string EligibilityBlocked = "shift-assign.eligibility-blocked";
    public const string OverlapConflict = "shift-assign.overlap-conflict";
    public const string AssessmentMismatch = "shift-assign.assessment-mismatch";
    public const string ShiftNotFound = "shift-assign.shift-not-found";
    public const string MachineNotFound = "shift-assign.machine-not-found";
    public const string PersonNotFound = "shift-assign.person-not-found";
    public const string InvalidStatus = "shift-assign.invalid-status";
}

public sealed class ShiftAssignmentWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ShiftAssignmentWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static ShiftAssignmentWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
