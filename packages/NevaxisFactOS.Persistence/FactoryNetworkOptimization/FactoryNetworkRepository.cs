using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.FactoryNetworkOptimization;

public interface IFactoryNetworkRepository
{
    Task<Guid?> ResolveNetworkIdAsync(Guid? networkId, Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetNetworkFacilityIdsAsync(Guid networkId, CancellationToken cancellationToken);
    Task<FactoryNetwork?> GetNetworkAsync(Guid networkId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryNetworkFacility>> GetNetworkFacilitiesAsync(
        Guid networkId, IReadOnlyList<Guid>? permittedFacilityIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<FacilityCapabilityProfile>> GetCapabilityProfilesAsync(
        Guid tenantId, IReadOnlyList<Guid> facilityIds, CancellationToken cancellationToken);
    Task<NetworkCapacitySnapshot?> GetLatestCapacitySnapshotAsync(Guid networkId, CancellationToken cancellationToken);
    Task<FactoryNetworkPolicy> GetOrCreatePolicyAsync(Guid tenantId, Guid networkId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkScenario>> GetScenariosAsync(Guid networkId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkAllocation>> GetActiveAllocationsAsync(Guid networkId, CancellationToken cancellationToken);
    Task<NetworkAllocation?> GetAllocationAsync(Guid allocationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkAllocationItem>> GetAllocationItemsAsync(Guid allocationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkActivityEvent>> GetActivityAsync(Guid networkId, int take, CancellationToken cancellationToken);
    Task AddScenarioAsync(NetworkScenario scenario, CancellationToken cancellationToken);
    Task AddAllocationAsync(NetworkAllocation allocation, IReadOnlyList<NetworkAllocationItem> items, CancellationToken cancellationToken);
    Task AddActivityAsync(NetworkActivityEvent activity, CancellationToken cancellationToken);
    Task AddLearningMemoryAsync(FactoryLearning.FactoryLearningMemory memory, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class FactoryNetworkRepository : IFactoryNetworkRepository
{
    private readonly NevaxisFactOSDbContext dbContext;

    public FactoryNetworkRepository(NevaxisFactOSDbContext dbContext) => this.dbContext = dbContext;

    public async Task<Guid?> ResolveNetworkIdAsync(
        Guid? networkId, Guid facilityId, CancellationToken cancellationToken)
    {
        if (networkId is not null && networkId != Guid.Empty)
        {
            return networkId;
        }

        var link = await dbContext.FactoryNetworkFacilities
            .Where(f => f.FacilityId == facilityId)
            .OrderBy(f => f.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
        return link?.NetworkId;
    }

    public async Task<IReadOnlyList<Guid>> GetNetworkFacilityIdsAsync(
        Guid networkId, CancellationToken cancellationToken) =>
        await dbContext.FactoryNetworkFacilities
            .Where(f => f.NetworkId == networkId)
            .OrderBy(f => f.SortOrder)
            .Select(f => f.FacilityId)
            .ToListAsync(cancellationToken);

    public Task<FactoryNetwork?> GetNetworkAsync(Guid networkId, CancellationToken cancellationToken) =>
        dbContext.FactoryNetworks.FirstOrDefaultAsync(n => n.Id == networkId, cancellationToken);

    public async Task<IReadOnlyList<FactoryNetworkFacility>> GetNetworkFacilitiesAsync(
        Guid networkId, IReadOnlyList<Guid>? permittedFacilityIds, CancellationToken cancellationToken)
    {
        var query = dbContext.FactoryNetworkFacilities
            .Where(f => f.NetworkId == networkId);
        if (permittedFacilityIds is { Count: > 0 })
            query = query.Where(f => permittedFacilityIds.Contains(f.FacilityId));

        return await query.OrderBy(f => f.SortOrder).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FacilityCapabilityProfile>> GetCapabilityProfilesAsync(
        Guid tenantId, IReadOnlyList<Guid> facilityIds, CancellationToken cancellationToken) =>
        await dbContext.FacilityCapabilityProfiles
            .Where(p => p.TenantId == tenantId && facilityIds.Contains(p.FacilityId))
            .ToListAsync(cancellationToken);

    public Task<NetworkCapacitySnapshot?> GetLatestCapacitySnapshotAsync(
        Guid networkId, CancellationToken cancellationToken) =>
        dbContext.NetworkCapacitySnapshots
            .Where(s => s.NetworkId == networkId)
            .OrderByDescending(s => s.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<FactoryNetworkPolicy> GetOrCreatePolicyAsync(
        Guid tenantId, Guid networkId, CancellationToken cancellationToken)
    {
        var policy = await dbContext.FactoryNetworkPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.NetworkId == networkId, cancellationToken);
        if (policy is not null) return policy;

        policy = new FactoryNetworkPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NetworkId = networkId,
            AutonomyMode = NetworkAutonomyModes.Limited,
            PolicyEnvelopeJson =
                """{"approvalThresholdOrders":2,"maxTransferQuantity":500,"prohibitedPairs":[],"requireLogisticsKnown":false}""",
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ConcurrencyToken = 1,
        };
        dbContext.FactoryNetworkPolicies.Add(policy);
        return policy;
    }

    public async Task<IReadOnlyList<NetworkScenario>> GetScenariosAsync(
        Guid networkId, int take, CancellationToken cancellationToken) =>
        await dbContext.NetworkScenarios
            .Where(s => s.NetworkId == networkId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NetworkAllocation>> GetActiveAllocationsAsync(
        Guid networkId, CancellationToken cancellationToken) =>
        await dbContext.NetworkAllocations
            .Where(a => a.NetworkId == networkId &&
                        a.Status != NetworkAllocationStatuses.Cancelled &&
                        a.Status != NetworkAllocationStatuses.Verified)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<NetworkAllocation?> GetAllocationAsync(Guid allocationId, CancellationToken cancellationToken) =>
        dbContext.NetworkAllocations.FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);

    public async Task<IReadOnlyList<NetworkAllocationItem>> GetAllocationItemsAsync(
        Guid allocationId, CancellationToken cancellationToken) =>
        await dbContext.NetworkAllocationItems
            .Where(i => i.AllocationId == allocationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NetworkActivityEvent>> GetActivityAsync(
        Guid networkId, int take, CancellationToken cancellationToken) =>
        await dbContext.NetworkActivityEvents
            .Where(a => a.NetworkId == networkId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task AddScenarioAsync(NetworkScenario scenario, CancellationToken cancellationToken)
    {
        dbContext.NetworkScenarios.Add(scenario);
        return Task.CompletedTask;
    }

    public Task AddAllocationAsync(
        NetworkAllocation allocation, IReadOnlyList<NetworkAllocationItem> items, CancellationToken cancellationToken)
    {
        dbContext.NetworkAllocations.Add(allocation);
        dbContext.NetworkAllocationItems.AddRange(items);
        return Task.CompletedTask;
    }

    public Task AddActivityAsync(NetworkActivityEvent activity, CancellationToken cancellationToken)
    {
        dbContext.NetworkActivityEvents.Add(activity);
        return Task.CompletedTask;
    }

    public Task AddLearningMemoryAsync(FactoryLearning.FactoryLearningMemory memory, CancellationToken cancellationToken)
    {
        dbContext.FactoryLearningMemories.Add(memory);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
