using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ProductionManager;

public interface IProductionManagerRepository
{
    Task<ProductionManagerPolicy> GetOrCreatePolicyAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FactoryStateSnapshot> CaptureStateAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryPrediction>> ListActivePredictionsAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryDecision>> ListDecisionsAsync(Guid facilityId, string? status, int limit, CancellationToken cancellationToken);
    Task<FactoryDecision?> GetDecisionAsync(Guid decisionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryDecisionMemory>> ListMemoriesAsync(Guid facilityId, string decisionType, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class ProductionManagerRepository : IProductionManagerRepository
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public ProductionManagerRepository(
        NevaxisFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public Task<ProductionManagerPolicy> GetOrCreatePolicyAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => GetOrCreatePolicyCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<FactoryStateSnapshot> CaptureStateAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var tenantId = tenantContextAccessor.RequireTenantId();
                var snapshot = await FactoryStateProjector.CaptureAsync(dbContext, tenantId, facilityId, ct);
                dbContext.FactoryStateSnapshots.Add(snapshot);
                await dbContext.SaveChangesAsync(ct);
                return snapshot;
            },
            cancellationToken);

    public Task<IReadOnlyList<FactoryPrediction>> ListActivePredictionsAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryPrediction>>(
            async ct => await dbContext.FactoryPredictions
                .AsNoTracking()
                .Where(p => p.FacilityId == facilityId && p.IsActive)
                .OrderByDescending(p => p.PredictedAtUtc)
                .Take(50)
                .ToListAsync(ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryDecision>> ListDecisionsAsync(
        Guid facilityId, string? status, int limit, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryDecision>>(
            async ct =>
            {
                var query = dbContext.FactoryDecisions
                    .AsNoTracking()
                    .Include(d => d.Options)
                    .Where(d => d.FacilityId == facilityId);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(d => d.Status == status);
                }

                return await query
                    .OrderByDescending(d => d.UpdatedAtUtc)
                    .Take(limit)
                    .ToListAsync(ct);
            },
            cancellationToken);

    public Task<FactoryDecision?> GetDecisionAsync(Guid decisionId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.FactoryDecisions
                .Include(d => d.Options)
                .Include(d => d.StateSnapshot)
                .FirstOrDefaultAsync(d => d.Id == decisionId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryDecisionMemory>> ListMemoriesAsync(
        Guid facilityId, string decisionType, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<FactoryDecisionMemory>>(
            async ct => await dbContext.FactoryDecisionMemories
                .AsNoTracking()
                .Where(m => m.FacilityId == facilityId && m.DecisionType == decisionType)
                .OrderByDescending(m => m.EffectivenessScore)
                .Take(20)
                .ToListAsync(ct),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await dbContext.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);

    private async Task<ProductionManagerPolicy> GetOrCreatePolicyCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var tenantId = tenantContextAccessor.RequireTenantId();

        var policy = await dbContext.ProductionManagerPolicies
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId, cancellationToken);

        if (policy is not null)
        {
            return policy;
        }

        policy = new ProductionManagerPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            AutonomyMode = ProductionManagerAutonomyModes.Draft,
            AllowedDecisionTypesJson = "[]",
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.ProductionManagerPolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }
}
