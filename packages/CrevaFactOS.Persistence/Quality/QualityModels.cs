namespace CrevaFactOS.Persistence.Quality;

public sealed record QualityOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class QualityFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string ValidationFailed = "validation-failed";
    public const string ProductionOrderNotFound = "production-order-not-found";
    public const string PlanNotActive = "plan-not-active";
    public const string ReleaseNotAllowed = "release-not-allowed";
    public const string SourceUnavailable = "source-unavailable";
    public const string BlockingQualityHold = "blocking-quality-hold";
}

public sealed class QualityWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }

    public static QualityWriteResult Success(Guid entityId, uint version) =>
        new() { Succeeded = true, EntityId = entityId, Version = version };

    public static QualityWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record ProductionOrderQualityContext(
    Guid ProductionOrderId,
    string ReferenceCode,
    string? ProductReference,
    string Status,
    decimal? PlannedQuantity,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    bool Available);

public sealed record QualityInspectionDefinitionInput(
    string Code,
    string Name,
    string? Description,
    string InspectionType,
    int Sequence,
    string? Unit,
    decimal? NominalValue,
    decimal? LowerLimit,
    decimal? UpperLimit,
    bool Required,
    bool Critical,
    string? Instruction);

public sealed record CreateQualityControlPlanCommand(
    string Code,
    string Name,
    string? Description,
    string? ProductReference,
    string? ProductGroup,
    string? OperationReference,
    string Status,
    string Revision,
    IReadOnlyList<QualityInspectionDefinitionInput> InspectionDefinitions);

public sealed record UpdateQualityControlPlanCommand(
    Guid PlanId,
    uint ExpectedVersion,
    string Code,
    string Name,
    string? Description,
    string? ProductReference,
    string? ProductGroup,
    string? OperationReference,
    string Status,
    string Revision,
    IReadOnlyList<QualityInspectionDefinitionInput> InspectionDefinitions);

public sealed record CreateQualityInspectionCommand(
    Guid ProductionOrderId,
    Guid QualityControlPlanId,
    string? Notes);

public sealed record RecordQualityInspectionResultCommand(
    Guid InspectionId,
    uint ExpectedVersion,
    Guid InspectionDefinitionId,
    decimal? MeasuredNumericValue,
    string? MeasuredTextValue,
    bool? BooleanResult,
    string? Comment);

public sealed record QualityInspectionLifecycleCommand(
    Guid InspectionId,
    uint ExpectedVersion,
    string? Note);

public sealed record QualityControlPlanSummaryReadModel(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string Revision,
    string? ProductReference,
    string? OperationReference,
    int DefinitionCount,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record QualityControlPlanDetailReadModel(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status,
    string Revision,
    string? ProductReference,
    string? ProductGroup,
    string? OperationReference,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<QualityInspectionDefinitionReadModel> InspectionDefinitions);

public sealed record QualityInspectionDefinitionReadModel(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string InspectionType,
    int Sequence,
    string? Unit,
    decimal? NominalValue,
    decimal? LowerLimit,
    decimal? UpperLimit,
    bool Required,
    bool Critical,
    string? Instruction,
    uint Version);

public sealed record QualityInspectionSummaryReadModel(
    Guid Id,
    string InspectionNumber,
    string Status,
    string OverallResult,
    string ReleaseStatus,
    Guid ProductionOrderId,
    string? ProductionOrderReference,
    string? ProductReference,
    Guid QualityControlPlanId,
    string? PlanCode,
    string? InspectorSubjectId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record QualityInspectionDetailReadModel(
    Guid Id,
    string InspectionNumber,
    string Status,
    string OverallResult,
    string ReleaseStatus,
    Guid ProductionOrderId,
    ProductionOrderQualityContext ProductionContext,
    Guid QualityControlPlanId,
    string PlanCode,
    string PlanName,
    string PlanRevision,
    string? InspectorSubjectId,
    string? Notes,
    string? ReleaseNote,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? ReleasedAtUtc,
    uint Version,
    IReadOnlyList<QualityInspectionResultReadModel> Results,
    IReadOnlyList<LinkedNonconformanceReadModel> LinkedNonconformances);

public sealed record QualityInspectionResultReadModel(
    Guid Id,
    Guid InspectionDefinitionId,
    string DefinitionCode,
    string DefinitionName,
    string InspectionType,
    string? Unit,
    decimal? NominalValue,
    decimal? LowerLimit,
    decimal? UpperLimit,
    bool Required,
    bool Critical,
    decimal? MeasuredNumericValue,
    string? MeasuredTextValue,
    bool? BooleanResult,
    string Result,
    string? Comment,
    DateTimeOffset RecordedAtUtc,
    string RecordedBySubjectId);

public sealed record QualityWorkspaceSummaryReadModel(
    int OpenInspectionCount,
    int PendingReviewCount,
    int FailedInspectionCount,
    int ReleasedInspectionCount,
    int OpenNcrCount,
    int ActiveHoldCount,
    int ActionPendingCount);
