namespace CrevaFactOS.Persistence.Production;

public sealed record ProductionOrderOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record ConsumeHandoffCommand(
    Guid HandoffId,
    uint ExpectedHandoffVersion);

public sealed record StartProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion);

public sealed record PauseProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion,
    string Reason);

public sealed record ResumeProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion);

public sealed record CompleteProductionOrderCommand(
    Guid ProductionOrderId,
    uint ExpectedVersion);

public static class ProductionOrderFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string HandoffNotFound = "handoff-not-found";
    public const string HandoffNotReady = "handoff-not-ready";
    public const string HandoffAlreadyConsumed = "handoff-already-consumed";
    public const string HandoffInvalid = "handoff-invalid";
    public const string NotFound = "not-found";
    public const string InvalidTransition = "invalid-transition";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string SourceUnavailable = "source-unavailable";
    public const string DataIntegrity = "data-integrity";
}

public sealed record ProductionOrderReadModel(
    Guid Id,
    Guid FacilityId,
    Guid ProductionPlanHandoffId,
    Guid SourceProductionPlanId,
    uint SourceProductionPlanVersion,
    string ReferenceCode,
    string Title,
    string Status,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    DateTimeOffset? ActualStartAtUtc,
    DateTimeOffset? ActualEndAtUtc,
    string? ShiftName,
    string? MachineReference,
    string? MoldReference,
    string? ProductReference,
    string CreatedBySubjectId,
    string LastModifiedBySubjectId,
    uint Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string HistoryJson);

public sealed record ProductionOrderListReadModel(
    Guid Id,
    Guid FacilityId,
    Guid SourceProductionPlanId,
    string ReferenceCode,
    string Title,
    string Status,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    DateTimeOffset? ActualStartAtUtc,
    string? MachineReference,
    string? MoldReference,
    string? ShiftName,
    uint Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record ProductionOrderWriteResult
{
    public required bool Succeeded { get; init; }
    public Guid? ProductionOrderId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? Status { get; init; }
    public uint? Version { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOrderWriteResult Success(
        Guid orderId,
        string referenceCode,
        string status,
        uint version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new()
        {
            Succeeded = true,
            ProductionOrderId = orderId,
            ReferenceCode = referenceCode,
            Status = status,
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
        };

    public static ProductionOrderWriteResult Fail(string failureCode, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = failureCode,
            FailureReason = reason,
        };
}

public sealed record ProductionOrderListResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<ProductionOrderListReadModel>? Items { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOrderListResult Success(IReadOnlyList<ProductionOrderListReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ProductionOrderListResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}

public sealed record ProductionOrderDetailResult
{
    public required bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public ProductionOrderReadModel? Order { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ProductionOrderDetailResult Success(ProductionOrderReadModel order) =>
        new() { Succeeded = true, Order = order };

    public static ProductionOrderDetailResult Missing() =>
        new() { Succeeded = false, NotFound = true, FailureCode = ProductionOrderFailureCodes.NotFound };

    public static ProductionOrderDetailResult Fail(string failureCode, string reason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = reason };
}
