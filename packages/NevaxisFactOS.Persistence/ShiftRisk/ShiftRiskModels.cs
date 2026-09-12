namespace NevaxisFactOS.Persistence.ShiftRisk;

public sealed record ShiftRiskOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ShiftRiskEvaluationContext(
    Guid ProductionShiftId,
    DateOnly ShiftDate,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineFamilyScope,
    string? RequiredCompetencyCode);

public sealed record ShiftRiskEvaluationContextBuildResult(
    bool Succeeded,
    ShiftRiskEvaluationContext? Context,
    string? FailureCode,
    string? FailureReason);

public sealed record ShiftRiskRequirementRecordReadModel(
    Guid Id,
    Guid ProductionShiftId,
    string ShiftLabel,
    DateOnly ShiftDate,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineAssetLabel,
    string? MachineFamilyScope,
    string RequiredCompetencyCode,
    string RequiredCompetencyLabel,
    int MinimumApprovedOperatorCount,
    bool IsCriticalRole,
    string SourceReference,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveUntil,
    bool IsActive,
    uint Version,
    bool IsPilotData);

public sealed record ShiftRiskRequirementListReadModel(
    IReadOnlyList<ShiftRiskRequirementRecordReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ShiftRiskRequirementListFilters(
    Guid? ProductionShiftId,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    bool? IsActive,
    int Page,
    int PageSize);

public sealed record ShiftRiskDriverReadModel(
    Guid Id,
    string ReasonCode,
    string LabelTr,
    string Category,
    string Severity,
    string? RequiredValue,
    string? ObservedValue,
    string? GapValue,
    Guid? ProductionShiftId,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? RequiredCompetencyCode,
    string EvidenceSource,
    string? EvidenceReference,
    DateTimeOffset? EvidenceTimestampUtc,
    string? FreshnessState,
    string RuleSetVersion,
    bool IsLeadingRisk);

public sealed record ShiftRiskAssessmentSummaryReadModel(
    Guid Id,
    Guid? CoverageRequirementId,
    Guid ProductionShiftId,
    string ShiftLabel,
    DateOnly ShiftDate,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineAssetLabel,
    string? RequiredCompetencyCode,
    int? MinimumRequiredCount,
    int ApprovedEligibleCount,
    int CoverageGap,
    string Outcome,
    string? LeadingDriverCategory,
    string? LeadingReasonCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    bool IsPilotData);

public sealed record ShiftRiskAssessmentDetailReadModel(
    Guid Id,
    Guid? CoverageRequirementId,
    Guid ProductionShiftId,
    string ShiftLabel,
    DateOnly ShiftDate,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineAssetLabel,
    string? MachineFamilyScope,
    string? RequiredCompetencyCode,
    int? MinimumRequiredCount,
    bool? IsCriticalRole,
    int ApprovedEligibleCount,
    int CoverageGap,
    string Outcome,
    string? LeadingDriverCategory,
    string? LeadingReasonCode,
    Guid? LeadingDriverId,
    string RuleSetVersion,
    string InputFingerprint,
    DateTimeOffset EvaluatedAtUtc,
    string StructuredEvidenceJson,
    string SourceFreshnessSnapshotJson,
    IReadOnlyList<ShiftRiskDriverReadModel> Drivers,
    IReadOnlyList<ShiftRiskAssignmentEvidenceReadModel> CountedAssignments,
    IReadOnlyList<ShiftRiskAssignmentEvidenceReadModel> ExcludedAssignments,
    bool IsPilotData);

public sealed record ShiftRiskAssignmentEvidenceReadModel(
    Guid AssignmentId,
    Guid WorkforcePersonId,
    string EmployeeCode,
    string DisplayName,
    string Status,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string? ExclusionReasonCode,
    bool IsPilotData);

public sealed record ShiftRiskAssessmentListReadModel(
    IReadOnlyList<ShiftRiskAssessmentSummaryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ShiftRiskAssessmentListFilters(
    Guid? ProductionShiftId,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? Outcome,
    string? DriverCategory,
    bool? CriticalOnly,
    int Page,
    int PageSize);

public sealed record ShiftRiskOverviewReadModel(
    int EvaluatedContextCount,
    int ClearCount,
    int AttentionCount,
    int CriticalCount,
    int InsufficientDataCount,
    int CoverageGapCount,
    int CriticalRoleGapCount,
    int ConflictCount);

public sealed record ShiftRiskAuditEventReadModel(
    Guid Id,
    string EventType,
    string EntityType,
    Guid EntityId,
    string ActorSubjectId,
    DateTimeOffset OccurredAtUtc,
    string Summary);

public sealed record CreateShiftCoverageRequirementCommand(
    Guid ProductionShiftId,
    string ShiftDate,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineFamilyScope,
    string RequiredCompetencyCode,
    int MinimumApprovedOperatorCount,
    bool IsCriticalRole,
    string SourceReference,
    string? EffectiveFrom,
    string? EffectiveUntil,
    bool IsActive,
    string IdempotencyKey);

public sealed record UpdateShiftCoverageRequirementCommand(
    Guid RequirementId,
    uint ExpectedVersion,
    int MinimumApprovedOperatorCount,
    bool IsCriticalRole,
    string SourceReference,
    string? EffectiveFrom,
    string? EffectiveUntil,
    bool IsActive);

public sealed record EvaluateShiftRiskCommand(
    Guid ProductionShiftId,
    string ShiftDate,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineFamilyScope,
    string? RequiredCompetencyCode,
    string IdempotencyKey);

public sealed record EvaluateWorkforceContinuityCommand(
    Guid ProductionShiftId,
    string ShiftDate,
    Guid WorkforcePersonId,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? RequiredCompetencyCode,
    string IdempotencyKey);

public sealed record ShiftContinuityAssessmentSummaryReadModel(
    Guid Id,
    Guid WorkforcePersonId,
    string EmployeeCode,
    string DisplayName,
    Guid ProductionShiftId,
    string ShiftLabel,
    DateOnly ShiftDate,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineAssetLabel,
    string? RequiredCompetencyCode,
    string AvailabilityState,
    string FreshnessState,
    int ApprovedAssignmentCount,
    int PlannedAssignmentMinutes,
    int TrustworthyAvailableCount,
    int CoverageGap,
    string Outcome,
    string? LeadingDriverCategory,
    string? LeadingReasonCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    bool IsPilotData);

public sealed record ShiftContinuityAssessmentDetailReadModel(
    Guid Id,
    Guid WorkforcePersonId,
    string EmployeeCode,
    string DisplayName,
    string Department,
    string JobTitle,
    bool IsActive,
    string AvailabilityState,
    string FreshnessState,
    DateTimeOffset? SourceUpdatedAtUtc,
    Guid ProductionShiftId,
    string ShiftLabel,
    DateOnly ShiftDate,
    DateTimeOffset PlannedStartUtc,
    DateTimeOffset PlannedEndUtc,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    string? MachineAssetLabel,
    string? RequiredCompetencyCode,
    int ApprovedAssignmentCount,
    int PlannedAssignmentMinutes,
    int TrustworthyAvailableCount,
    int CoverageGap,
    string Outcome,
    string? LeadingDriverCategory,
    string? LeadingReasonCode,
    Guid? LeadingDriverId,
    string RuleSetVersion,
    string InputFingerprint,
    DateTimeOffset EvaluatedAtUtc,
    string StructuredEvidenceJson,
    string SourceFreshnessSnapshotJson,
    IReadOnlyList<ShiftRiskDriverReadModel> Drivers,
    IReadOnlyList<ShiftContinuityCompetencyEvidenceReadModel> Competencies,
    IReadOnlyList<ShiftRiskAssignmentEvidenceReadModel> ConsideredAssignments,
    bool IsPilotData);

public sealed record ShiftContinuityCompetencyEvidenceReadModel(
    Guid AssignmentId,
    string CompetencyCode,
    string CompetencyLabel,
    string VerificationStatus,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    bool IsPilotData);

public sealed record ShiftContinuityAssessmentListReadModel(
    IReadOnlyList<ShiftContinuityAssessmentSummaryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ShiftContinuityAssessmentListFilters(
    Guid? ProductionShiftId,
    string? ProductionOrderReference,
    Guid? MachineAssetId,
    Guid? WorkforcePersonId,
    string? Outcome,
    string? DriverCategory,
    string? AvailabilityState,
    string? FreshnessState,
    string? RequiredCompetencyCode,
    bool? CriticalOnly,
    int Page,
    int PageSize);

public sealed record ShiftContinuityOverviewReadModel(
    int EvaluatedContextCount,
    int ClearCount,
    int AttentionCount,
    int CriticalCount,
    int InsufficientDataCount,
    int AbsenceImpactCount,
    int CompetencyExpiredCount,
    int CompetencyExpiringCount,
    int StaleOrUnknownCount,
    int AssignmentOverlapCount,
    int CoverageImpactCount);

public static class ShiftRiskFailureCodes
{
    public const string InvalidScope = "shift-risk.invalid-scope";
    public const string NotFound = "shift-risk.not-found";
    public const string ValidationFailed = "shift-risk.validation-failed";
    public const string ConcurrencyConflict = "shift-risk.concurrency-conflict";
    public const string IdempotencyConflict = "shift-risk.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "shift-risk.idempotency-legacy-unverifiable";
    public const string ShiftNotFound = "shift-risk.shift-not-found";
    public const string MachineNotFound = "shift-risk.machine-not-found";
    public const string PersonNotFound = "shift-risk.person-not-found";
}

public sealed class ShiftRiskWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ShiftRiskWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static ShiftRiskWriteResult SuccessAssessment(Guid assessmentId, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = assessmentId,
            IdempotentReplay = idempotentReplay,
        };

    public static ShiftRiskWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
