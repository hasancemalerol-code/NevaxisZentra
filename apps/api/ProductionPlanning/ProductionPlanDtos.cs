namespace NevaxisZentra.Api.ProductionPlanning;

public sealed record ProductionPlanSummaryDto(
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

public sealed record ProductionPlanDemandDto(
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

public sealed record ProductionPlanRiskDto(
    string Id,
    string Type,
    string Severity,
    string Title,
    string Description,
    bool Blocking,
    string? Source);

public sealed record ProductionPlanResourceAssignmentDto(
    string? MachineId,
    string? MachineName,
    string? MoldId,
    string? MoldName,
    string? MaterialId,
    string? MaterialName,
    string? OperatorGroupId,
    string? OperatorGroupName);

public sealed record ProductionPlanTimeWindowDto(
    DateTimeOffset? PlannedStartAtUtc,
    DateTimeOffset? PlannedEndAtUtc,
    int? EstimatedDurationMinutes,
    string? ShiftId,
    string? ShiftName);

public sealed record ProductionPlanApprovalDto(
    string Status,
    string? RequiredRole,
    DateTimeOffset? RequestedAtUtc,
    string? RequestedBy,
    DateTimeOffset? ApprovedAtUtc,
    string? ApprovedBy,
    DateTimeOffset? RejectedAtUtc,
    string? RejectedBy,
    string? RejectionReason);

public sealed record ProductionPlanTransferDto(
    string? TargetSystem,
    string Status,
    string? ExternalPlanId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ProductionPlanSourceReferenceDto(
    string SourceType,
    string SourceId,
    DateTimeOffset? SourceUpdatedAtUtc);

public sealed record ProductionPlanHistoryEntryDto(
    string Id,
    string Type,
    string Title,
    string Description,
    DateTimeOffset? OccurredAtUtc);

public sealed record ProductionPlanDetailDto(
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
    IReadOnlyList<ProductionPlanDemandDto> Demands,
    IReadOnlyList<ProductionPlanRiskDto> Risks,
    ProductionPlanResourceAssignmentDto Resources,
    ProductionPlanTimeWindowDto TimeWindow,
    ProductionPlanApprovalDto Approval,
    ProductionPlanTransferDto Transfer,
    IReadOnlyList<ProductionPlanSourceReferenceDto> SourceReferences,
    IReadOnlyList<ProductionPlanHistoryEntryDto> History);

public sealed record ProductionPlanListResponse(
    Guid FacilityId,
    int Count,
    IReadOnlyList<ProductionPlanSummaryDto> Items);

public sealed record ProductionPlanDetailResponse(
    ProductionPlanDetailDto Plan);
