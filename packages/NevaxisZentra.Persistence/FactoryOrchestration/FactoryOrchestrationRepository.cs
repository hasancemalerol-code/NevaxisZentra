using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public interface IFactoryOrchestrationRepository
{
    Task<IReadOnlyList<DigitalWorkCase>> ListActiveRootCasesAsync(Guid facilityId, int limit, CancellationToken cancellationToken);
    Task<DigitalWorkCase?> GetRootCaseAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkCase>> ListChildCasesAsync(Guid rootCaseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryCaseDependency>> ListDependenciesAsync(Guid rootCaseId, CancellationToken cancellationToken);
    Task<FactoryEventCorrelation?> FindCorrelationAsync(string correlationKey, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class FactoryOrchestrationRepository : IFactoryOrchestrationRepository
{
    private readonly NevaxisZentraDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public FactoryOrchestrationRepository(
        NevaxisZentraDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<IReadOnlyList<DigitalWorkCase>> ListActiveRootCasesAsync(
        Guid facilityId, int limit, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListActiveRootCasesCoreAsync(facilityId, limit, ct),
            cancellationToken);

    public Task<DigitalWorkCase?> GetRootCaseAsync(Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetRootCaseCoreAsync(caseId, ct),
            cancellationToken);

    public Task<IReadOnlyList<DigitalWorkCase>> ListChildCasesAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListChildCasesCoreAsync(rootCaseId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryCaseDependency>> ListDependenciesAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryCaseDependency>>(
            async ct => await dbContext.FactoryCaseDependencies
                .AsNoTracking()
                .Where(d => d.RootCaseId == rootCaseId)
                .OrderBy(d => d.CreatedAtUtc)
                .ToListAsync(ct),
            cancellationToken);

    public Task<FactoryEventCorrelation?> FindCorrelationAsync(
        string correlationKey, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.FactoryEventCorrelations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CorrelationKey == correlationKey, ct),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await dbContext.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkCase>> ListActiveRootCasesCoreAsync(
        Guid facilityId, int limit, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Include(c => c.PrimaryWorker)
            .Where(c => c.FacilityId == facilityId &&
                        c.ParentCaseId == null &&
                        c.Status != DigitalWorkCaseStatuses.Closed)
            .OrderByDescending(c => c.Severity)
            .ThenByDescending(c => c.UpdatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    private async Task<DigitalWorkCase?> GetRootCaseCoreAsync(Guid caseId, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .Include(c => c.PrimaryWorker)
            .Include(c => c.Approvals)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkCase>> ListChildCasesCoreAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Include(c => c.PrimaryWorker)
            .Where(c => c.OrchestrationRootCaseId == rootCaseId || c.ParentCaseId == rootCaseId)
            .ToListAsync(cancellationToken);
}
