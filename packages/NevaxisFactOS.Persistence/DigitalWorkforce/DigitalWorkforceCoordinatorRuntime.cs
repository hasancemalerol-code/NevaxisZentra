using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceCoordinatorRuntime
{
    Task<DigitalWorkforceCoordinatorSnapshot> EvaluateCaseAsync(Guid rootCaseId, CancellationToken cancellationToken);
}

public sealed record DigitalWorkforceCoordinatorSnapshot(
    Guid RootCaseId,
    int ActiveChildCases,
    int StuckExecutions,
    int PendingApprovals,
    int Conflicts,
    IReadOnlyList<string> Recommendations);

public sealed class DigitalWorkforceCoordinatorRuntime(
    NevaxisFactOSDbContext dbContext,
    IDigitalWorkforceRepository repository) : IDigitalWorkforceCoordinatorRuntime
{
    public async Task<DigitalWorkforceCoordinatorSnapshot> EvaluateCaseAsync(
        Guid rootCaseId,
        CancellationToken cancellationToken)
    {
        var root = await repository.GetCaseAsync(rootCaseId, cancellationToken)
            ?? throw new DigitalWorkforceException(DigitalWorkforceFailureCodes.NotFound, "Case not found.");

        var childCases = await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Where(c => c.OrchestrationRootCaseId == rootCaseId || c.ParentCaseId == rootCaseId)
            .ToListAsync(cancellationToken);

        var caseIds = childCases.Select(c => c.Id).Append(rootCaseId).ToList();
        var stuckExecutions = await dbContext.DigitalWorkExecutions.CountAsync(
            e => caseIds.Contains(e.CaseId) &&
                 (e.Status == DigitalWorkExecutionStatuses.Running ||
                  e.Status == DigitalWorkExecutionStatuses.Claimed) &&
                 e.LeaseExpiresAtUtc < DateTimeOffset.UtcNow.AddMinutes(-5),
            cancellationToken);
        var pendingApprovals = await dbContext.DigitalWorkApprovals.CountAsync(
            a => caseIds.Contains(a.CaseId) && a.Status == DigitalWorkApprovalStatuses.Pending,
            cancellationToken);
        var conflicts = childCases.Count(c =>
            !string.Equals(c.CurrentBlockerCode, "none", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(c.CurrentBlockerCode));

        var recommendations = new List<string>();
        if (stuckExecutions > 0)
        {
            recommendations.Add("Takılı aksiyonlar için lease reclaim çalıştırın.");
        }

        if (pendingApprovals > 0)
        {
            recommendations.Add($"{pendingApprovals} onay bekleyen aksiyon var.");
        }

        if (conflicts > 0)
        {
            recommendations.Add("Çocuk vakalarda çakışma/blokaj tespit edildi.");
        }

        return new(
            rootCaseId,
            childCases.Count(c => c.Status != DigitalWorkCaseStatuses.Closed),
            stuckExecutions,
            pendingApprovals,
            conflicts,
            recommendations);
    }
}
