namespace NevaxisFactOS.Persistence.ProductionPlanning;

/// <summary>
/// Vendor-agnostic canonical payload for production plan transfer to ERP/MES systems.
/// Must not contain tenant membership IDs, raw audit data, full history, auth tokens, or secrets.
/// </summary>
public sealed record ProductionPlanTransferPayload(
    Guid PlanId,
    string ReferenceCode,
    string FacilityReferenceId,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    string? ShiftName,
    IReadOnlyList<ProductionPlanTransferDemand> Demands,
    ProductionPlanTransferResourceAssignment ResourceAssignment,
    ProductionPlanTransferApprovalSummary ApprovalSummary,
    uint Version);

public sealed record ProductionPlanTransferDemand(
    string ProductCode,
    decimal Quantity,
    string Unit);

public sealed record ProductionPlanTransferResourceAssignment(
    string? MachineId,
    string? MoldId,
    string? ShiftTeam);

public sealed record ProductionPlanTransferApprovalSummary(
    string ApprovedBy,
    DateTimeOffset ApprovedAtUtc);
