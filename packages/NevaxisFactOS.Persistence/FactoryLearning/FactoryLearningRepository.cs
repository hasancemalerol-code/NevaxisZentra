using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.ProductionManager;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.FactoryLearning;

public interface IFactoryLearningRepository
{
    Task<IReadOnlyList<FactoryLoss>> ListLossesAsync(Guid facilityId, int days, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryLossPattern>> ListPatternsAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ImprovementOpportunity>> ListOpportunitiesAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryExperiment>> ListExperimentsAsync(Guid facilityId, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<VerifiedImprovement>> ListVerifiedImprovementsAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryLearningMemory>> ListMemoriesAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<ImprovementOpportunity?> GetOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken);
    Task<FactoryExperiment?> GetExperimentAsync(Guid experimentId, CancellationToken cancellationToken);
    Task<VerifiedImprovement?> GetVerifiedImprovementAsync(Guid verifiedImprovementId, CancellationToken cancellationToken);
    Task<FactoryStateSnapshot?> GetLatestStateAsync(Guid facilityId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class FactoryLearningRepository : IFactoryLearningRepository
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public FactoryLearningRepository(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<IReadOnlyList<FactoryLoss>> ListLossesAsync(Guid facilityId, int days, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryLoss>>(async ct =>
        {
            var since = DateTimeOffset.UtcNow.AddDays(-days);
            return await dbContext.FactoryLosses
                .AsNoTracking()
                .Where(l => l.FacilityId == facilityId && l.LastSeenAtUtc >= since)
                .OrderByDescending(l => l.LastSeenAtUtc)
                .Take(100)
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<IReadOnlyList<FactoryLossPattern>> ListPatternsAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryLossPattern>>(async ct =>
            await dbContext.FactoryLossPatterns
                .AsNoTracking()
                .Where(p => p.FacilityId == facilityId)
                .OrderByDescending(p => p.LastObservedAtUtc)
                .Take(50)
                .ToListAsync(ct), cancellationToken);

    public Task<IReadOnlyList<ImprovementOpportunity>> ListOpportunitiesAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<ImprovementOpportunity>>(async ct =>
            await dbContext.ImprovementOpportunities
                .AsNoTracking()
                .Where(o => o.FacilityId == facilityId)
                .OrderByDescending(o => o.PriorityScore)
                .Take(50)
                .ToListAsync(ct), cancellationToken);

    public Task<IReadOnlyList<FactoryExperiment>> ListExperimentsAsync(
        Guid facilityId, string? status, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryExperiment>>(async ct =>
        {
            var query = dbContext.FactoryExperiments
                .AsNoTracking()
                .Where(e => e.FacilityId == facilityId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status);
            }

            return await query.OrderByDescending(e => e.UpdatedAtUtc).Take(50).ToListAsync(ct);
        }, cancellationToken);

    public Task<IReadOnlyList<VerifiedImprovement>> ListVerifiedImprovementsAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<VerifiedImprovement>>(async ct =>
            await dbContext.VerifiedImprovements
                .AsNoTracking()
                .Where(v => v.FacilityId == facilityId)
                .OrderByDescending(v => v.VerifiedAtUtc)
                .Take(50)
                .ToListAsync(ct), cancellationToken);

    public Task<IReadOnlyList<FactoryLearningMemory>> ListMemoriesAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryLearningMemory>>(async ct =>
            await dbContext.FactoryLearningMemories
                .AsNoTracking()
                .Where(m => m.FacilityId == facilityId)
                .OrderByDescending(m => m.RecordedAtUtc)
                .Take(50)
                .ToListAsync(ct), cancellationToken);

    public Task<ImprovementOpportunity?> GetOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.ImprovementOpportunities.FirstOrDefaultAsync(o => o.Id == opportunityId, ct), cancellationToken);

    public Task<FactoryExperiment?> GetExperimentAsync(Guid experimentId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.FactoryExperiments
                .FirstOrDefaultAsync(e => e.Id == experimentId, ct), cancellationToken);

    public Task<VerifiedImprovement?> GetVerifiedImprovementAsync(
        Guid verifiedImprovementId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.VerifiedImprovements.FirstOrDefaultAsync(v => v.Id == verifiedImprovementId, ct), cancellationToken);

    public Task<FactoryStateSnapshot?> GetLatestStateAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.FactoryStateSnapshots
                .AsNoTracking()
                .Where(s => s.FacilityId == facilityId)
                .OrderByDescending(s => s.CapturedAtUtc)
                .FirstOrDefaultAsync(ct), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            await dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
}
