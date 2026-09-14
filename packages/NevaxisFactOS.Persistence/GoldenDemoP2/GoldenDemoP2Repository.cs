using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.GoldenDemoP1;

namespace NevaxisFactOS.Persistence.GoldenDemoP2;

public interface IGoldenDemoP2Repository
{
    Task ResetPilotEventAsync(Guid productionEventId, CancellationToken cancellationToken);
}

public sealed class GoldenDemoP2Repository : IGoldenDemoP2Repository
{
    private readonly NevaxisFactOSDbContext dbContext;

    public GoldenDemoP2Repository(NevaxisFactOSDbContext dbContext) => this.dbContext = dbContext;

    public async Task ResetPilotEventAsync(Guid productionEventId, CancellationToken cancellationToken)
    {
        var verifications = await dbContext.GoldenDemoVerificationRuns
            .Where(v => v.ProductionEventId == productionEventId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoVerificationRuns.RemoveRange(verifications);

        var findings = await dbContext.GoldenDemoAgentFindings
            .Where(f => f.ProductionEventId == productionEventId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoAgentFindings.RemoveRange(findings);

        var postObservations = await dbContext.GoldenDemoCycleObservations
            .Where(o => o.ProductionEventId == productionEventId && o.ObservationKind == "post-intervention")
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCycleObservations.RemoveRange(postObservations);

        var plan = await dbContext.GoldenDemoRecoveryPlans
            .FirstOrDefaultAsync(p => p.ProductionEventId == productionEventId, cancellationToken);
        if (plan is not null)
        {
            var actions = await dbContext.GoldenDemoRecoveryPlanActions
                .Where(a => a.RecoveryPlanId == plan.Id)
                .ToListAsync(cancellationToken);
            foreach (var action in actions)
            {
                action.Status = GoldenDemoPlanActionStatuses.Planned;
                action.CompletedAtUtc = null;
            }

            plan.Status = GoldenDemoPlanActionStatuses.Planned;
            plan.ApprovedBySubjectId = null;
            plan.ApprovedAtUtc = null;
            plan.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var evt = await dbContext.GoldenDemoProductionEvents
            .FirstOrDefaultAsync(e => e.Id == productionEventId, cancellationToken);
        if (evt is not null)
        {
            evt.Status = GoldenDemoP1EventStatuses.Active;
            evt.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
