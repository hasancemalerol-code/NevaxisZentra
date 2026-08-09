using CrevaFactOS.Persistence.ProductionPlanning;

namespace CrevaFactOS.Api.ProductionPlanning;

public sealed record PlanningLogisticsConstraintApiModel(
    Guid Id,
    string ConstraintType,
    string Status,
    string Severity,
    DateTimeOffset? RequiredByUtc,
    DateTimeOffset? CalculatedLatestStartUtc,
    DateTimeOffset? CalculatedLatestCompletionUtc,
    int? BufferMinutes,
    int? SlackMinutes,
    bool Blocking,
    string RiskLevel,
    string Reason,
    string SourceReference,
    string SourceType,
    Guid? SourceRecordId,
    Guid? SourceExportCaseId,
    Guid? SourceShipmentId);

public sealed record PlanningLogisticsEvaluationApiResponse(
    Guid EvaluationId,
    Guid ProductionPlanId,
    Guid? ExportCaseId,
    string? ExportCaseNumber,
    Guid? ShipmentId,
    string? ShipmentReference,
    string? SourceOrderReference,
    DateTimeOffset? PickupDeadlineUtc,
    DateTimeOffset? DepartureDeadlineUtc,
    DateTimeOffset LatestProductionCompletionUtc,
    DateTimeOffset PlannedProductionCompletionUtc,
    int SlackMinutes,
    string RiskLevel,
    bool Blocking,
    string? BlockingReason,
    uint SourceVersion,
    DateTimeOffset EvaluatedAtUtc,
    uint Version,
    IReadOnlyList<PlanningLogisticsConstraintApiModel> Constraints,
    string LinkageStatus);

public interface IPlanningLogisticsService
{
    Task<PlanningLogisticsEvaluationReadResult> GetLatestAsync(
        ProductionPlanReadOperationContext operationContext,
        Guid productionPlanId,
        CancellationToken cancellationToken = default);

    Task<PlanningLogisticsEvaluationWriteResult> EvaluateAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        CancellationToken cancellationToken = default);
}

public sealed class PlanningLogisticsService(
    IPlanningLogisticsConstraintRepository repository) : IPlanningLogisticsService
{
    public Task<PlanningLogisticsEvaluationReadResult> GetLatestAsync(
        ProductionPlanReadOperationContext operationContext,
        Guid productionPlanId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);

        return repository.GetLatestAsync(
            operationContext.TenantId,
            operationContext.FacilityId,
            productionPlanId,
            cancellationToken);
    }

    public Task<PlanningLogisticsEvaluationWriteResult> EvaluateAsync(
        ProductionPlanWriteOperationContext operationContext,
        Guid productionPlanId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationContext);

        var context = new PlanningLogisticsEvaluateContext(
            operationContext.TenantId,
            operationContext.FacilityId,
            operationContext.MembershipId,
            operationContext.IdentitySubjectId,
            operationContext.PermissionKey,
            operationContext.CorrelationId);

        var command = new PlanningLogisticsEvaluateCommand(
            operationContext.TenantId,
            operationContext.FacilityId,
            productionPlanId,
            operationContext.IdentitySubjectId,
            operationContext.CorrelationId);

        return repository.EvaluateAsync(context, command, cancellationToken);
    }
}

public static class PlanningLogisticsApiMapper
{
    public static PlanningLogisticsEvaluationApiResponse Map(
        PlanningLogisticsEvaluationReadModel evaluation) =>
        new(
            evaluation.EvaluationId,
            evaluation.ProductionPlanId,
            evaluation.ExportCaseId,
            evaluation.ExportCaseNumber,
            evaluation.ShipmentId,
            evaluation.ShipmentReference,
            evaluation.SourceOrderReference,
            evaluation.PickupDeadlineUtc,
            evaluation.DepartureDeadlineUtc,
            evaluation.LatestProductionCompletionUtc,
            evaluation.PlannedProductionCompletionUtc,
            evaluation.SlackMinutes,
            PlanningLogisticsRiskPolicy.ToStorage(evaluation.RiskLevel),
            evaluation.Blocking,
            evaluation.BlockingReason,
            evaluation.SourceVersion,
            evaluation.EvaluatedAtUtc,
            evaluation.Version,
            evaluation.Constraints.Select(MapConstraint).ToArray(),
            evaluation.LinkageStatus);

    private static PlanningLogisticsConstraintApiModel MapConstraint(
        PlanningLogisticsConstraintReadModel constraint) =>
        new(
            constraint.Id,
            PlanningLogisticsValueConversions.ToStorage(constraint.ConstraintType),
            PlanningLogisticsValueConversions.ToStorage(constraint.Status),
            PlanningLogisticsRiskPolicy.ToStorage(constraint.Severity),
            constraint.RequiredByUtc,
            constraint.CalculatedLatestStartUtc,
            constraint.CalculatedLatestCompletionUtc,
            constraint.BufferMinutes,
            constraint.SlackMinutes,
            constraint.Blocking,
            PlanningLogisticsRiskPolicy.ToStorage(constraint.RiskLevel),
            constraint.Reason,
            constraint.SourceReference,
            constraint.SourceType.ToString(),
            constraint.SourceRecordId,
            constraint.SourceExportCaseId,
            constraint.SourceShipmentId);
}
