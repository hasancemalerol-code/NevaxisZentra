namespace CrevaFactOS.Api.ProductionPlanning;

public sealed record ProductionPlanListRequest(
    Guid FacilityId,
    string? Status,
    string? Search,
    int? Limit);

public sealed record ProductionPlanDetailRequest(
    Guid FacilityId,
    Guid ProductionPlanId);
