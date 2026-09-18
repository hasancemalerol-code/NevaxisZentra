using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.AiUsage;

public sealed class AiUsageMeteringService(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IAiUsageMeteringService
{
    private const decimal DefaultMonthlyQuota = 10_000m;

    public async Task RecordUsageAsync(
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
        CancellationToken cancellationToken = default)
    {
        await tenantDatabaseSession.ExecuteWriteAsync<int>(
            async ct =>
            {
                dbContext.AiUsageRecords.Add(new AiUsageRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FacilityId = facilityId,
                    UserSubjectId = userSubjectId,
                    Module = moduleName,
                    Agent = agent,
                    RunId = runId,
                    Provider = provider,
                    Model = model,
                    InputUsage = inputUsage,
                    OutputUsage = outputUsage,
                    CreditsConsumed = creditsConsumed,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
                await dbContext.SaveChangesAsync(ct);
                return 0;
            },
            cancellationToken);
    }

    public async Task<AiUsageSummaryReadModel> GetCurrentUsageAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        var periodStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var consumed = await tenantDatabaseSession.ExecuteReadAsync(
            async ct => await dbContext.AiUsageRecords
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.FacilityId == facilityId && r.CreatedAtUtc >= periodStart)
                .SumAsync(r => r.CreditsConsumed, ct),
            cancellationToken);

        var percent = DefaultMonthlyQuota <= 0 ? 0 : consumed / DefaultMonthlyQuota * 100m;
        var warning = percent >= 100 ? "100" : percent >= 90 ? "90" : percent >= 75 ? "75" : "none";

        return new AiUsageSummaryReadModel(consumed, DefaultMonthlyQuota, percent, warning);
    }
}

public sealed class UnavailableAiUsageMeteringService : IAiUsageMeteringService
{
    public Task RecordUsageAsync(
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
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<AiUsageSummaryReadModel> GetCurrentUsageAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiUsageSummaryReadModel(0m, null, 0m, "none"));
}
