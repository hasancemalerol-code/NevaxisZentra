namespace NevaxisZentra.Persistence.ProductionPlanning;

public interface IPlanningLogisticsConstraintRepository
{
    Task<PlanningLogisticsEvaluationReadResult> GetLatestAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionPlanId,
        CancellationToken cancellationToken = default);

    Task<PlanningLogisticsEvaluationWriteResult> EvaluateAsync(
        PlanningLogisticsEvaluateContext context,
        PlanningLogisticsEvaluateCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record PlanningLogisticsEvaluateContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class PlanningLogisticsFailureCodes
{
    public const string NotFound = "not-found";
    public const string InvalidScope = "invalid-scope";
    public const string SourceUnavailable = "source-unavailable";
}
