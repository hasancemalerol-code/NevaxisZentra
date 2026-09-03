using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public interface IAiTaskRoutingService
{
    Task<AiTaskRoutingDecisionReadModel?> EvaluateRoutingAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken cancellationToken = default);
}

public sealed class AiTaskRoutingService(CrevaFactOSDbContext dbContext) : IAiTaskRoutingService
{
    public async Task<AiTaskRoutingDecisionReadModel?> EvaluateRoutingAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken cancellationToken = default)
    {
        var rules = await dbContext.AiTaskRoutingRules.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.Enabled)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.RuleKey)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (rule.SourceModule is not null
                && !string.Equals(rule.SourceModule, task.SourceModule, StringComparison.Ordinal))
            {
                continue;
            }

            if (rule.Category is not null
                && !string.Equals(rule.Category, task.Category, StringComparison.Ordinal))
            {
                continue;
            }

            if (rule.Priority is not null
                && !string.Equals(rule.Priority, task.Priority, StringComparison.Ordinal))
            {
                continue;
            }

            return new AiTaskRoutingDecisionReadModel(
                rule.Id,
                rule.RuleKey,
                rule.TargetType,
                rule.TargetMembershipId,
                rule.TargetTeamChannelId,
                $"Matched routing rule '{rule.Name}'.");
        }

        return null;
    }
}
