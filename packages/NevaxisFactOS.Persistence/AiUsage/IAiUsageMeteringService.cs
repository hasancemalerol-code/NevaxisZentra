namespace NevaxisFactOS.Persistence.AiUsage;

public interface IAiUsageMeteringService
{
    Task RecordUsageAsync(
        Guid tenantId,
        Guid facilityId,
        string userSubjectId,
        string moduleName,
        string agent,
        Guid runId,
        string provider,
        string model,
        int inputUsage,
        int outputUsage,
        decimal creditsConsumed,
        CancellationToken cancellationToken = default);

    Task<AiUsageSummaryReadModel> GetCurrentUsageAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default);
}

public sealed record AiUsageSummaryReadModel(
    decimal CreditsConsumedThisPeriod,
    decimal? MonthlyQuota,
    decimal UsagePercent,
    string WarningLevel);
