namespace CrevaFactOS.Persistence.ShiftHandover;

public sealed record ShiftHandoverOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ShiftHandoverAccessCapabilities(
    bool CanRead,
    bool CanManage,
    bool CanAcknowledge,
    bool CanCreateTask);

public sealed record CreateShiftHandoverCommand(
    DateOnly ShiftDate,
    string ShiftPeriod,
    string Department,
    string Summary,
    Guid? IncomingMembershipId);

public sealed record UpdateShiftHandoverCommand(
    Guid HandoverId,
    uint ExpectedVersion,
    DateOnly ShiftDate,
    string ShiftPeriod,
    string Department,
    string Summary,
    Guid? IncomingMembershipId);

public sealed record AddShiftHandoverItemCommand(
    Guid HandoverId,
    string Category,
    string Title,
    string Description,
    string Severity,
    string? SourceModule,
    string? SourceEntityId,
    bool RequiresFollowUp,
    string? EvidenceMetadataJson);

public sealed record UpdateShiftHandoverItemCommand(
    Guid HandoverId,
    Guid ItemId,
    uint ExpectedVersion,
    string Category,
    string Title,
    string Description,
    string Severity,
    string Status,
    bool RequiresFollowUp,
    string? EvidenceMetadataJson);

public sealed record ShiftHandoverLifecycleCommand(
    Guid HandoverId,
    uint ExpectedVersion);

public sealed record LinkShiftHandoverItemTaskCommand(
    Guid HandoverId,
    Guid ItemId,
    Guid AiTaskId);

public sealed class ShiftHandoverWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }

    public static ShiftHandoverWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static ShiftHandoverWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public sealed record ShiftHandoverSummaryReadModel(
    Guid Id,
    string HandoverReference,
    DateOnly ShiftDate,
    string ShiftPeriod,
    string Department,
    string Status,
    string OutgoingLabel,
    string? IncomingLabel,
    int UnresolvedItemCount,
    int FollowUpItemCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? HandedOverAtUtc);

public sealed record ShiftHandoverItemReadModel(
    Guid Id,
    int SortOrder,
    string Category,
    string Title,
    string Description,
    string Severity,
    string? SourceModule,
    string? SourceEntityId,
    string Status,
    bool RequiresFollowUp,
    Guid? LinkedAiTaskId,
    string? EvidenceMetadataJson,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ShiftHandoverEventReadModel(
    Guid Id,
    string EventType,
    string ActorLabel,
    string PayloadJson,
    DateTimeOffset OccurredAtUtc);

public sealed record ShiftHandoverDetailReadModel(
    Guid Id,
    string HandoverReference,
    DateOnly ShiftDate,
    string ShiftPeriod,
    string Department,
    string Summary,
    string Status,
    Guid OutgoingMembershipId,
    string OutgoingLabel,
    Guid? IncomingMembershipId,
    string? IncomingLabel,
    string FacilityName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? HandedOverAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    uint Version,
    IReadOnlyList<ShiftHandoverItemReadModel> Items,
    IReadOnlyList<ShiftHandoverEventReadModel> Events,
    ShiftHandoverAccessCapabilities Capabilities);

public sealed record ShiftHandoverListReadModel(
    IReadOnlyList<ShiftHandoverSummaryReadModel> Items,
    ShiftHandoverAccessCapabilities Capabilities,
    string AutomationDisclaimer);
