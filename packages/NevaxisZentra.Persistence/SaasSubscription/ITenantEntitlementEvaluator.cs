namespace NevaxisZentra.Persistence.SaasSubscription;

public interface ITenantEntitlementEvaluator
{
    Task<TenantEntitlementEvaluationReadModel> EvaluatePaidApplicationAccessAsync(
        CancellationToken cancellationToken = default);

    Task<TenantEntitlementEvaluationReadModel> EvaluateModuleAccessAsync(
        string routePath,
        CancellationToken cancellationToken = default);

    Task<TenantEntitlementEvaluationReadModel> EvaluateFeatureAccessAsync(
        string featureCode,
        CancellationToken cancellationToken = default);

    Task<TenantEntitlementEvaluationReadModel> EvaluateLimitAsync(
        string limitEntitlementCode,
        CancellationToken cancellationToken = default);

    Task<TenantEntitlementEvaluationReadModel> EvaluateEntitlementAsync(
        string entitlementCode,
        CancellationToken cancellationToken = default);

    Task<TenantEntitlementEvaluationReadModel> EvaluateUserLimitForAdditionalSeatsAsync(
        int additionalSeats,
        CancellationToken cancellationToken = default);
}
