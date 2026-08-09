namespace CrevaFactOS.Persistence.Maintenance;

public sealed record MaintenanceOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class MaintenanceFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string ValidationFailed = "validation-failed";
    public const string ProductionOrderNotFound = "production-order-not-found";
    public const string SourceUnavailable = "source-unavailable";
    public const string CompletionNoteRequired = "completion-note-required";
}

public sealed class MaintenanceWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }

    public static MaintenanceWriteResult Success(Guid entityId, uint version) =>
        new() { Succeeded = true, EntityId = entityId, Version = version };

    public static MaintenanceWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record MaintainableAssetReference(
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName);

public sealed record CreateMaintenanceRequestCommand(
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    Guid? ProductionOrderId,
    string Type,
    string Priority,
    string Title,
    string Description,
    string? Source);

public sealed record UpdateMaintenanceRequestCommand(
    Guid RequestId,
    uint ExpectedVersion,
    string? Title,
    string? Description,
    string? Priority,
    string? Status);

public sealed record ConvertMaintenanceRequestCommand(
    Guid RequestId,
    uint ExpectedVersion,
    string? AssignedTechnicianSubjectId,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc);

public sealed record CreateMaintenanceWorkOrderCommand(
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    Guid? ProductionOrderId,
    Guid? MaintenanceRequestId,
    string Type,
    string Priority,
    string Title,
    string Description,
    string? AssignedTechnicianSubjectId,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc,
    string InitialStatus);

public sealed record AssignMaintenanceWorkOrderCommand(
    Guid WorkOrderId,
    uint ExpectedVersion,
    string? AssignedTechnicianSubjectId);

public sealed record MaintenanceWorkOrderLifecycleCommand(
    Guid WorkOrderId,
    uint ExpectedVersion,
    string? Note);

public sealed record CompleteMaintenanceWorkOrderCommand(
    Guid WorkOrderId,
    uint ExpectedVersion,
    string CompletionNote,
    string? ResolutionCode);

public sealed record CreatePreventiveMaintenancePlanCommand(
    string Code,
    string Name,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string IntervalType,
    int IntervalValue,
    DateTimeOffset? NextDueAtUtc,
    string Status,
    string? Description);

public sealed record UpdatePreventiveMaintenancePlanCommand(
    Guid PlanId,
    uint ExpectedVersion,
    string Code,
    string Name,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string IntervalType,
    int IntervalValue,
    DateTimeOffset? NextDueAtUtc,
    string Status,
    string? Description);

public sealed record MaintenanceRequestSummaryReadModel(
    Guid Id,
    string RequestNumber,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    string Title,
    string Type,
    string Priority,
    string Status,
    string ReportedBySubjectId,
    DateTimeOffset ReportedAtUtc,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record MaintenanceRequestDetailReadModel(
    Guid Id,
    string RequestNumber,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    Guid? ProductionOrderId,
    string? ProductionOrderReference,
    string? ProductionOrderStatus,
    string Type,
    string Priority,
    string Status,
    string Title,
    string Description,
    string Source,
    string ReportedBySubjectId,
    DateTimeOffset ReportedAtUtc,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<MaintenanceActivityReadModel> Activity);

public sealed record MaintenanceWorkOrderSummaryReadModel(
    Guid Id,
    string WorkOrderNumber,
    Guid? MaintenanceRequestId,
    string? MaintenanceRequestNumber,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    string Type,
    string Priority,
    string Status,
    string? AssignedTechnicianSubjectId,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc,
    DateTimeOffset? StartedAtUtc,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record MaintenanceWorkOrderDetailReadModel(
    Guid Id,
    string WorkOrderNumber,
    Guid? MaintenanceRequestId,
    string? MaintenanceRequestNumber,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    Guid? ProductionOrderId,
    string? ProductionOrderReference,
    string? ProductionOrderStatus,
    string Type,
    string Priority,
    string Status,
    string Title,
    string Description,
    string? AssignedTechnicianSubjectId,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? CompletionNote,
    string? ResolutionCode,
    string CreatedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<MaintenanceActivityReadModel> Activity);

public sealed record PreventiveMaintenancePlanSummaryReadModel(
    Guid Id,
    string Code,
    string Name,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string IntervalType,
    int IntervalValue,
    DateTimeOffset? NextDueAtUtc,
    string Status,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record PreventiveMaintenancePlanDetailReadModel(
    Guid Id,
    string Code,
    string Name,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string IntervalType,
    int IntervalValue,
    DateTimeOffset? NextDueAtUtc,
    string Status,
    string? Description,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MaintenanceWorkspaceSummaryReadModel(
    int OpenRequestCount,
    int OpenWorkOrderCount,
    int CriticalWorkOrderCount,
    int InProgressCount,
    int WaitingCount,
    int CompletedTodayCount,
    int OverduePreventiveCount);

public sealed record MaintenanceActivityReadModel(
    string Action,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string? ActorSubjectId,
    string Summary);
