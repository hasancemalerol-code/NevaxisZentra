using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public interface IFactoryOutcomeExecutionRepository
{
    Task<FactoryOutcomeEventReceipt?> GetEventReceiptAsync(
        Guid tenantId, Guid facilityId, string sourceType, string sourceId, CancellationToken cancellationToken);
    Task<FactoryOutcomeExecutionAction?> GetActionByKeyAsync(
        Guid tenantId, Guid rootCaseId, string actionKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryOutcomeExecutionAction>> ListActionsAsync(
        Guid rootCaseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FactoryOutcomeLearningRecord>> FindSimilarLearningRecordsAsync(
        Guid tenantId, Guid facilityId, string problemPattern, int limit, CancellationToken cancellationToken);
}

public sealed class FactoryOutcomeExecutionRepository : IFactoryOutcomeExecutionRepository
{
    private readonly NevaxisFactOSDbContext dbContext;

    public FactoryOutcomeExecutionRepository(NevaxisFactOSDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<FactoryOutcomeEventReceipt?> GetEventReceiptAsync(
        Guid tenantId, Guid facilityId, string sourceType, string sourceId, CancellationToken cancellationToken) =>
        dbContext.FactoryOutcomeEventReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.FacilityId == facilityId &&
                     r.SourceType == sourceType &&
                     r.SourceId == sourceId,
                cancellationToken);

    public Task<FactoryOutcomeExecutionAction?> GetActionByKeyAsync(
        Guid tenantId, Guid rootCaseId, string actionKey, CancellationToken cancellationToken) =>
        dbContext.FactoryOutcomeExecutionActions
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.RootCaseId == rootCaseId && a.ActionKey == actionKey,
                cancellationToken);

    public async Task<IReadOnlyList<FactoryOutcomeExecutionAction>> ListActionsAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        await dbContext.FactoryOutcomeExecutionActions
            .Where(a => a.RootCaseId == rootCaseId)
            .OrderBy(a => a.ExecutionOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FactoryOutcomeLearningRecord>> FindSimilarLearningRecordsAsync(
        Guid tenantId, Guid facilityId, string problemPattern, int limit, CancellationToken cancellationToken) =>
        await dbContext.FactoryOutcomeLearningRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId &&
                        r.FacilityId == facilityId &&
                        r.ProblemPattern == problemPattern &&
                        r.VerificationResult == DigitalWorkforce.DigitalWorkVerificationResults.Passed)
            .OrderByDescending(r => r.VerifiedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
