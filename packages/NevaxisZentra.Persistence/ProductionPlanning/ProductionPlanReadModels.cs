namespace NevaxisZentra.Persistence.ProductionPlanning;

public sealed record ProductionPlanListReadInput(
    Guid TenantId,
    Guid FacilityId,
    ProductionPlanStatus? Status,
    string? Search,
    int Limit);

public sealed record ProductionPlanDetailReadInput(
    Guid TenantId,
    Guid FacilityId,
    Guid ProductionPlanId);

public sealed record ProductionPlanSummaryReadModel(
    Guid Id,
    string ReferenceCode,
    string Title,
    Guid FacilityId,
    string Status,
    string Origin,
    bool AiGenerated,
    string Confidence,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    string ApprovalStatus,
    string TransferStatus,
    string? TransferTargetSystem,
    DateTimeOffset UpdatedAtUtc,
    int DemandCount,
    decimal? TotalRequiredQuantity,
    string? TotalRequiredQuantityUnit,
    decimal? PrimaryRequiredQuantity,
    string? PrimaryUnit,
    string? PrimaryProductCode,
    string? PrimaryProductName,
    string? HighestDemandPriority,
    int RiskCount,
    int CriticalRiskCount,
    int BlockingRiskCount,
    bool HasBlockingRisk,
    bool HasMachineAssignment,
    bool HasMoldAssignment,
    bool HasMaterialAssignment,
    bool HasOperatorGroupAssignment,
    string? ShiftName,
    DateTimeOffset? EarliestRequestedDeliveryDateUtc);

public sealed record ProductionPlanListOperationalSummaryReadModel(
    int DemandCount,
    decimal? TotalRequiredQuantity,
    string? TotalRequiredQuantityUnit,
    decimal? PrimaryRequiredQuantity,
    string? PrimaryUnit,
    string? PrimaryProductCode,
    string? PrimaryProductName,
    string? HighestDemandPriority,
    int RiskCount,
    int CriticalRiskCount,
    int BlockingRiskCount,
    bool HasBlockingRisk,
    bool HasMachineAssignment,
    bool HasMoldAssignment,
    bool HasMaterialAssignment,
    bool HasOperatorGroupAssignment,
    string? ShiftName,
    DateTimeOffset? EarliestRequestedDeliveryDateUtc);

public sealed record ProductionPlanDetailReadModel(
    Guid Id,
    string ReferenceCode,
    string Title,
    Guid FacilityId,
    string Status,
    string Origin,
    bool AiGenerated,
    string Confidence,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    string ApprovalStatus,
    string TransferStatus,
    string? TransferTargetSystem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    string? Notes,
    string CreatedBy,
    string? AiExplanation,
    string DemandsJson,
    string RisksJson,
    string ResourceAssignmentJson,
    string TimeWindowJson,
    string ApprovalJson,
    string TransferJson,
    string SourceReferencesJson,
    string HistoryJson);

public sealed record ProductionPlanListReadResult
{
    public required bool Succeeded { get; init; }

    public IReadOnlyList<ProductionPlanSummaryReadModel> Items { get; init; } =
        Array.Empty<ProductionPlanSummaryReadModel>();

    public ProductionPlanReadFailure? Failure { get; init; }

    public static ProductionPlanListReadResult Success(
        IReadOnlyList<ProductionPlanSummaryReadModel> items) =>
        new() { Succeeded = true, Items = items };

    public static ProductionPlanListReadResult Fail(ProductionPlanReadFailure failure) =>
        new() { Succeeded = false, Failure = failure };
}

public sealed record ProductionPlanDetailReadResult
{
    public required bool Succeeded { get; init; }

    public ProductionPlanDetailReadModel? Plan { get; init; }

    public ProductionPlanReadFailure? Failure { get; init; }

    public static ProductionPlanDetailReadResult Success(ProductionPlanDetailReadModel plan) =>
        new() { Succeeded = true, Plan = plan };

    public static ProductionPlanDetailReadResult Fail(ProductionPlanReadFailure failure) =>
        new() { Succeeded = false, Failure = failure };
}

public sealed record ProductionPlanDemandReadModel(
    string Id,
    string ProductId,
    string ProductName,
    decimal RequiredQuantity,
    string Unit,
    string Priority,
    string? OrderReference,
    string? CustomerId,
    string? CustomerName,
    string? ProductCode,
    DateTimeOffset? RequestedDeliveryDateUtc);

public sealed record ProductionPlanRiskReadModel(
    string Id,
    string Type,
    string Severity,
    string Title,
    string Description,
    bool Blocking,
    string? Source);

public sealed record ProductionPlanResourceAssignmentReadModel(
    string? MachineId,
    string? MachineName,
    string? MoldId,
    string? MoldName,
    string? MaterialId,
    string? MaterialName,
    string? OperatorGroupId,
    string? OperatorGroupName);

public sealed record ProductionPlanTimeWindowReadModel(
    DateTimeOffset? PlannedStartAtUtc,
    DateTimeOffset? PlannedEndAtUtc,
    int? EstimatedDurationMinutes,
    string? ShiftId,
    string? ShiftName);

public sealed record ProductionPlanApprovalReadModel(
    string Status,
    string? RequiredRole,
    DateTimeOffset? RequestedAtUtc,
    string? RequestedBy,
    DateTimeOffset? ApprovedAtUtc,
    string? ApprovedBy,
    DateTimeOffset? RejectedAtUtc,
    string? RejectedBy,
    string? RejectionReason);

public sealed record ProductionPlanTransferReadModel(
    string? TargetSystem,
    string Status,
    string? ExternalPlanId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ProductionPlanSourceReferenceReadModel(
    string SourceType,
    string SourceId,
    DateTimeOffset? SourceUpdatedAtUtc);

public sealed record ProductionPlanHistoryEntryReadModel(
    string Id,
    string Type,
    string Title,
    string Description,
    DateTimeOffset? OccurredAtUtc);

public sealed record ProductionPlanDetailMappedReadModel(
    ProductionPlanSummaryReadModel Summary,
    string? Notes,
    string CreatedBy,
    string? AiExplanation,
    DateTimeOffset CreatedAtUtc,
    uint Version,
    IReadOnlyList<ProductionPlanDemandReadModel> Demands,
    IReadOnlyList<ProductionPlanRiskReadModel> Risks,
    ProductionPlanResourceAssignmentReadModel Resources,
    ProductionPlanTimeWindowReadModel TimeWindow,
    ProductionPlanApprovalReadModel Approval,
    ProductionPlanTransferReadModel Transfer,
    IReadOnlyList<ProductionPlanSourceReferenceReadModel> SourceReferences,
    IReadOnlyList<ProductionPlanHistoryEntryReadModel> History);

public sealed record ProductionPlanJsonMapFailure(
    string Field,
    string Code,
    string Message);

public sealed record ProductionPlanJsonMapResult<T>(
    bool Succeeded,
    T? Value,
    ProductionPlanJsonMapFailure? Failure);
