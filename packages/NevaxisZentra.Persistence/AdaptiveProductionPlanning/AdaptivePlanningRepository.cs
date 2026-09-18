using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.FactoryLearning;
using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Persistence.AdaptiveProductionPlanning;

public interface IAdaptivePlanningRepository
{
    Task<ProductionPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken);
    Task<ProductionPlanVersion?> GetVersionAsync(Guid versionId, CancellationToken cancellationToken);
    Task<ProductionPlanVersion?> GetActiveVersionAsync(Guid planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPlanOperation>> GetOperationsAsync(Guid versionId, CancellationToken cancellationToken);
    Task<AdaptivePlanningPolicy> GetOrCreatePolicyAsync(Guid tenantId, Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanActivityEvent>> GetActivityAsync(Guid planId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanChange>> GetChangesAsync(Guid planId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanDeviation>> GetDeviationsAsync(Guid planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlanningScenario>> GetScenariosAsync(Guid planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionPlanVersion>> GetVersionsForPlanAsync(Guid planId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VerifiedImprovement>> GetChangeoverLearningAsync(Guid facilityId, CancellationToken cancellationToken);
    Task AddSnapshotAsync(PlanningConstraintSnapshot snapshot, CancellationToken cancellationToken);
    Task AddVersionAsync(ProductionPlanVersion version, CancellationToken cancellationToken);
    Task AddOperationsAsync(IReadOnlyList<ProductionPlanOperation> operations, CancellationToken cancellationToken);
    Task AddScenarioAsync(PlanningScenario scenario, IReadOnlyList<PlanningScenarioMetric> metrics, CancellationToken cancellationToken);
    Task AddChangeAsync(PlanChange change, CancellationToken cancellationToken);
    Task AddDeviationAsync(PlanDeviation deviation, CancellationToken cancellationToken);
    Task AddActivityAsync(PlanActivityEvent activity, CancellationToken cancellationToken);
    Task AddLearningMemoryAsync(FactoryLearning.FactoryLearningMemory memory, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class AdaptivePlanningRepository : IAdaptivePlanningRepository
{
    private readonly NevaxisZentraDbContext dbContext;

    public AdaptivePlanningRepository(NevaxisZentraDbContext dbContext) => this.dbContext = dbContext;

    public Task<ProductionPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken) =>
        dbContext.ProductionPlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

    public Task<ProductionPlanVersion?> GetVersionAsync(Guid versionId, CancellationToken cancellationToken) =>
        dbContext.ProductionPlanVersions.FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

    public Task<ProductionPlanVersion?> GetActiveVersionAsync(Guid planId, CancellationToken cancellationToken) =>
        dbContext.ProductionPlanVersions
            .Where(v => v.ProductionPlanId == planId && v.LifecycleStatus == AdaptivePlanLifecycleStatuses.Active)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<IReadOnlyList<ProductionPlanOperation>> GetOperationsAsync(Guid versionId, CancellationToken cancellationToken) =>
        dbContext.ProductionPlanOperations
            .Where(o => o.PlanVersionId == versionId)
            .OrderBy(o => o.SequenceRank)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ProductionPlanOperation>)t.Result, cancellationToken);

    public async Task<AdaptivePlanningPolicy> GetOrCreatePolicyAsync(
        Guid tenantId, Guid facilityId, CancellationToken cancellationToken)
    {
        var policy = await dbContext.AdaptivePlanningPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.FacilityId == facilityId, cancellationToken);
        if (policy is not null) return policy;

        policy = new AdaptivePlanningPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            FrozenWindowMinutes = 60,
            NervousnessCooldownMinutes = 30,
            MinBenefitThreshold = 0.05m,
            AutonomyMode = PlanningAutonomyModes.Draft,
            PolicyEnvelopeJson = """{"maxOrdersAffected":2,"maxScheduleMovementMinutes":90,"maxLatenessImpactHours":2,"approvalThresholdOrders":3}""",
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            ConcurrencyToken = 1,
        };
        dbContext.AdaptivePlanningPolicies.Add(policy);
        return policy;
    }

    public Task<IReadOnlyList<PlanActivityEvent>> GetActivityAsync(Guid planId, int take, CancellationToken cancellationToken) =>
        dbContext.PlanActivityEvents
            .Where(a => a.ProductionPlanId == planId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<PlanActivityEvent>)t.Result, cancellationToken);

    public Task<IReadOnlyList<PlanChange>> GetChangesAsync(Guid planId, int take, CancellationToken cancellationToken) =>
        dbContext.PlanChanges
            .Where(c => c.ProductionPlanId == planId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<PlanChange>)t.Result, cancellationToken);

    public Task<IReadOnlyList<PlanDeviation>> GetDeviationsAsync(Guid planId, CancellationToken cancellationToken) =>
        dbContext.PlanDeviations
            .Where(d => d.ProductionPlanId == planId)
            .OrderByDescending(d => d.DetectedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<PlanDeviation>)t.Result, cancellationToken);

    public Task<IReadOnlyList<PlanningScenario>> GetScenariosAsync(Guid planId, CancellationToken cancellationToken) =>
        dbContext.PlanningScenarios
            .Where(s => s.ProductionPlanId == planId)
            .OrderBy(s => s.Rank)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<PlanningScenario>)t.Result, cancellationToken);

    public Task<IReadOnlyList<ProductionPlanVersion>> GetVersionsForPlanAsync(Guid planId, CancellationToken cancellationToken) =>
        dbContext.ProductionPlanVersions
            .Where(v => v.ProductionPlanId == planId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ProductionPlanVersion>)t.Result, cancellationToken);

    public Task<IReadOnlyList<VerifiedImprovement>> GetChangeoverLearningAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        dbContext.VerifiedImprovements
            .Where(v => v.FacilityId == facilityId && v.StandardizationState == "standardized")
            .OrderByDescending(v => v.EffectivenessScore)
            .Take(10)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<VerifiedImprovement>)t.Result, cancellationToken);

    public Task AddSnapshotAsync(PlanningConstraintSnapshot snapshot, CancellationToken cancellationToken)
    {
        dbContext.PlanningConstraintSnapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task AddVersionAsync(ProductionPlanVersion version, CancellationToken cancellationToken)
    {
        dbContext.ProductionPlanVersions.Add(version);
        return Task.CompletedTask;
    }

    public Task AddOperationsAsync(IReadOnlyList<ProductionPlanOperation> operations, CancellationToken cancellationToken)
    {
        dbContext.ProductionPlanOperations.AddRange(operations);
        return Task.CompletedTask;
    }

    public Task AddScenarioAsync(
        PlanningScenario scenario, IReadOnlyList<PlanningScenarioMetric> metrics, CancellationToken cancellationToken)
    {
        dbContext.PlanningScenarios.Add(scenario);
        dbContext.PlanningScenarioMetrics.AddRange(metrics);
        return Task.CompletedTask;
    }

    public Task AddChangeAsync(PlanChange change, CancellationToken cancellationToken)
    {
        dbContext.PlanChanges.Add(change);
        return Task.CompletedTask;
    }

    public Task AddDeviationAsync(PlanDeviation deviation, CancellationToken cancellationToken)
    {
        dbContext.PlanDeviations.Add(deviation);
        return Task.CompletedTask;
    }

    public Task AddActivityAsync(PlanActivityEvent activity, CancellationToken cancellationToken)
    {
        dbContext.PlanActivityEvents.Add(activity);
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
