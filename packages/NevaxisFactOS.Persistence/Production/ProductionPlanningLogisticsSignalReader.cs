using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Production;

/// <summary>
/// Reads normalized Planning Logistics signals only — never Export shipment/broker/compliance details.
/// </summary>
public sealed class ProductionPlanningLogisticsSignalReader(NevaxisFactOSDbContext dbContext)
    : IProductionPlanningLogisticsSignalReader
{
    public async Task<ProductionLogisticsRiskSignal> ReadAggregateForFacilityAsync(
        Guid tenantId,
        Guid facilityId,
        IReadOnlyCollection<Guid> linkedPlanIds,
        CancellationToken cancellationToken = default)
    {
        if (linkedPlanIds.Count == 0)
        {
            return Unavailable("no-linked-plans");
        }

        var byPlan = await ReadForPlansAsync(tenantId, facilityId, linkedPlanIds, cancellationToken);
        var available = byPlan.Values.Where(v => v.Available).ToList();
        if (available.Count == 0)
        {
            return Unavailable("planning-logistics-unavailable");
        }

        var worst = available
            .OrderByDescending(v => v.Blocking)
            .ThenByDescending(v => RiskRank(v.RiskLevel))
            .ThenBy(v => v.SlackMinutes)
            .First();

        return worst with { Reason = worst.Reason ?? "Normalized Planning Logistics signal." };
    }

    public async Task<IReadOnlyDictionary<Guid, ProductionLogisticsRiskSignal>> ReadForPlansAsync(
        Guid tenantId,
        Guid facilityId,
        IReadOnlyCollection<Guid> planIds,
        CancellationToken cancellationToken = default)
    {
        if (planIds.Count == 0)
        {
            return new Dictionary<Guid, ProductionLogisticsRiskSignal>();
        }

        var distinctPlanIds = planIds.Distinct().ToList();
        var evaluations = await dbContext.PlanningLogisticsConstraintEvaluations
            .AsNoTracking()
            .Where(e =>
                e.TenantId == tenantId
                && e.FacilityId == facilityId
                && distinctPlanIds.Contains(e.ProductionPlanId))
            .OrderByDescending(e => e.EvaluatedAtUtc)
            .ToListAsync(cancellationToken);

        var latestByPlan = evaluations
            .GroupBy(e => e.ProductionPlanId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new Dictionary<Guid, ProductionLogisticsRiskSignal>();
        foreach (var planId in distinctPlanIds)
        {
            if (!latestByPlan.TryGetValue(planId, out var entity))
            {
                result[planId] = Unavailable("not-evaluated");
                continue;
            }

            result[planId] = new ProductionLogisticsRiskSignal(
                entity.RiskLevel,
                entity.SlackMinutes,
                entity.Blocking,
                entity.BlockingReason,
                entity.LatestProductionCompletionUtc,
                Available: true);
        }

        return result;
    }

    private static ProductionLogisticsRiskSignal Unavailable(string reason) =>
        new("none", 0, false, reason, null, Available: false);

    private static int RiskRank(string riskLevel) =>
        riskLevel switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0,
        };
}
