using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.GoldenDemoP1;

public interface IGoldenDemoP1Repository
{
    Task<GoldenDemoProductionEvent?> GetActiveEventAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken);
    Task<GoldenDemoProductionEvent?> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<GoldenDemoCycleBaseline?> GetBaselineAsync(Guid productionEventId, CancellationToken cancellationToken);
    Task<GoldenDemoCycleObservation?> GetObservationAsync(
        Guid productionEventId,
        string observationKind,
        CancellationToken cancellationToken);
    Task<GoldenDemoRecoveryPlan?> GetPlanAsync(Guid productionEventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoldenDemoRecoveryPlanAction>> GetPlanActionsAsync(
        Guid planId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<GoldenDemoAgentFinding>> GetFindingsAsync(
        Guid productionEventId,
        CancellationToken cancellationToken);
    Task<GoldenDemoVerificationRun?> GetLatestVerificationAsync(
        Guid productionEventId,
        CancellationToken cancellationToken);
    Task SaveFindingsAsync(IReadOnlyList<GoldenDemoAgentFinding> findings, CancellationToken cancellationToken);
    Task UpdateEventAsync(GoldenDemoProductionEvent evt, CancellationToken cancellationToken);
    Task UpdatePlanAsync(GoldenDemoRecoveryPlan plan, CancellationToken cancellationToken);
    Task UpdatePlanActionAsync(GoldenDemoRecoveryPlanAction action, CancellationToken cancellationToken);
    Task AddObservationAsync(GoldenDemoCycleObservation observation, CancellationToken cancellationToken);
    Task AddVerificationAsync(GoldenDemoVerificationRun verification, CancellationToken cancellationToken);
}

public sealed class GoldenDemoP1Repository : IGoldenDemoP1Repository
{
    private readonly NevaxisZentraDbContext dbContext;

    public GoldenDemoP1Repository(NevaxisZentraDbContext dbContext) => this.dbContext = dbContext;

    public Task<GoldenDemoProductionEvent?> GetActiveEventAsync(
        Guid tenantId,
        Guid facilityId,
        Guid? machineAssetId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.GoldenDemoProductionEvents
            .Where(e => e.TenantId == tenantId && e.FacilityId == facilityId &&
                        e.Status != GoldenDemoP1EventStatuses.Closed);
        if (machineAssetId.HasValue)
        {
            query = query.Where(e => e.MachineAssetId == machineAssetId.Value);
        }

        return query.OrderByDescending(e => e.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<GoldenDemoProductionEvent?> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.GoldenDemoProductionEvents.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

    public Task<GoldenDemoCycleBaseline?> GetBaselineAsync(
        Guid productionEventId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCycleBaselines
            .FirstOrDefaultAsync(b => b.ProductionEventId == productionEventId, cancellationToken);

    public Task<GoldenDemoCycleObservation?> GetObservationAsync(
        Guid productionEventId,
        string observationKind,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCycleObservations
            .Where(o => o.ProductionEventId == productionEventId && o.ObservationKind == observationKind)
            .OrderByDescending(o => o.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<GoldenDemoRecoveryPlan?> GetPlanAsync(
        Guid productionEventId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoRecoveryPlans
            .FirstOrDefaultAsync(p => p.ProductionEventId == productionEventId, cancellationToken);

    public Task<IReadOnlyList<GoldenDemoRecoveryPlanAction>> GetPlanActionsAsync(
        Guid planId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoRecoveryPlanActions
            .Where(a => a.RecoveryPlanId == planId)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<GoldenDemoRecoveryPlanAction>)t.Result, cancellationToken);

    public Task<IReadOnlyList<GoldenDemoAgentFinding>> GetFindingsAsync(
        Guid productionEventId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoAgentFindings
            .Where(f => f.ProductionEventId == productionEventId)
            .OrderBy(f => f.AgentCode)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<GoldenDemoAgentFinding>)t.Result, cancellationToken);

    public Task<GoldenDemoVerificationRun?> GetLatestVerificationAsync(
        Guid productionEventId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoVerificationRuns
            .Where(v => v.ProductionEventId == productionEventId)
            .OrderByDescending(v => v.VerifiedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SaveFindingsAsync(
        IReadOnlyList<GoldenDemoAgentFinding> findings,
        CancellationToken cancellationToken)
    {
        if (findings.Count == 0)
        {
            return;
        }

        var eventId = findings[0].ProductionEventId;
        var existing = await dbContext.GoldenDemoAgentFindings
            .Where(f => f.ProductionEventId == eventId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoAgentFindings.RemoveRange(existing);
        dbContext.GoldenDemoAgentFindings.AddRange(findings);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateEventAsync(GoldenDemoProductionEvent evt, CancellationToken cancellationToken)
    {
        evt.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePlanAsync(GoldenDemoRecoveryPlan plan, CancellationToken cancellationToken)
    {
        plan.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePlanActionAsync(
        GoldenDemoRecoveryPlanAction action,
        CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task AddObservationAsync(
        GoldenDemoCycleObservation observation,
        CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCycleObservations.Add(observation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVerificationAsync(
        GoldenDemoVerificationRun verification,
        CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoVerificationRuns.Add(verification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
