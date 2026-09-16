using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

public interface IFactoryRecoveryLearningRepository
{
    Task<IReadOnlyList<FactoryOutcomeExecution.FactoryOutcomeLearningRecord>> FindCandidatesAsync(
        Guid tenantId,
        Guid facilityId,
        string patternKey,
        string outcomeType,
        string eventCategory,
        string? machineCode,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FactoryOutcomeExecution.FactoryOutcomeLearningRecord>> SearchAsync(
        Guid tenantId,
        Guid facilityId,
        FactoryRecoveryLearningSearchFilters filters,
        CancellationToken cancellationToken);

    Task<FactoryOutcomeExecution.FactoryOutcomeLearningRecord?> GetByIdAsync(
        Guid tenantId,
        Guid learningRecordId,
        CancellationToken cancellationToken);

    Task<FactoryOutcomeExecution.FactoryOutcomeLearningRecord?> GetLatestBySourceCaseAsync(
        Guid tenantId,
        Guid sourceCaseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FactoryOutcomeExecution.FactoryOutcomeLearningRecord>> ListFailureModesAsync(
        Guid tenantId,
        Guid facilityId,
        string patternKeyPrefix,
        int limit,
        CancellationToken cancellationToken);
}

public sealed class FactoryRecoveryLearningRepository : IFactoryRecoveryLearningRepository
{
    private readonly NevaxisFactOSDbContext dbContext;

    public FactoryRecoveryLearningRepository(NevaxisFactOSDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FactoryOutcomeExecution.FactoryOutcomeLearningRecord>> FindCandidatesAsync(
        Guid tenantId,
        Guid facilityId,
        string patternKey,
        string outcomeType,
        string eventCategory,
        string? machineCode,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FactoryOutcomeLearningRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.FacilityId == facilityId);

        var candidates = await query
            .Where(r =>
                r.PatternKey.StartsWith(patternKey.Split('|')[0]) ||
                r.OutcomeType == outcomeType ||
                r.EventCategory == eventCategory ||
                (machineCode != null && r.MachineCode == machineCode))
            .OrderByDescending(r => r.VerifiedAtUtc)
            .Take(Math.Min(limit * 4, 40))
            .ToListAsync(cancellationToken);

        return candidates;
    }

    public async Task<IReadOnlyList<FactoryOutcomeExecution.FactoryOutcomeLearningRecord>> SearchAsync(
        Guid tenantId,
        Guid facilityId,
        FactoryRecoveryLearningSearchFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FactoryOutcomeLearningRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.FacilityId == facilityId);

        if (!string.IsNullOrWhiteSpace(filters.OutcomeType))
        {
            query = query.Where(r => r.OutcomeType == filters.OutcomeType);
        }

        if (!string.IsNullOrWhiteSpace(filters.MachineCode))
        {
            query = query.Where(r => r.MachineCode == filters.MachineCode);
        }

        if (!string.IsNullOrWhiteSpace(filters.OrderReference))
        {
            query = query.Where(r => r.OrderReference == filters.OrderReference);
        }

        if (!string.IsNullOrWhiteSpace(filters.MaterialCode))
        {
            query = query.Where(r => r.MaterialCode == filters.MaterialCode);
        }

        if (!string.IsNullOrWhiteSpace(filters.EventCategory))
        {
            query = query.Where(r => r.EventCategory == filters.EventCategory);
        }

        if (!string.IsNullOrWhiteSpace(filters.StrategyKey))
        {
            query = query.Where(r => r.SelectedStrategyKey == filters.StrategyKey);
        }

        if (!string.IsNullOrWhiteSpace(filters.WorkerCode))
        {
            query = query.Where(r => r.WorkersJson.Contains(filters.WorkerCode));
        }

        if (filters.VerifiedAfterUtc.HasValue)
        {
            query = query.Where(r => r.VerifiedAtUtc >= filters.VerifiedAfterUtc.Value);
        }

        return await query
            .OrderByDescending(r => r.VerifiedAtUtc)
            .Take(Math.Min(filters.Limit, 50))
            .ToListAsync(cancellationToken);
    }

    public Task<FactoryOutcomeExecution.FactoryOutcomeLearningRecord?> GetByIdAsync(
        Guid tenantId,
        Guid learningRecordId,
        CancellationToken cancellationToken) =>
        dbContext.FactoryOutcomeLearningRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == learningRecordId, cancellationToken);

    public Task<FactoryOutcomeExecution.FactoryOutcomeLearningRecord?> GetLatestBySourceCaseAsync(
        Guid tenantId,
        Guid sourceCaseId,
        CancellationToken cancellationToken) =>
        dbContext.FactoryOutcomeLearningRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.SourceCaseId == sourceCaseId)
            .OrderByDescending(r => r.RecordVersion)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FactoryOutcomeExecution.FactoryOutcomeLearningRecord>> ListFailureModesAsync(
        Guid tenantId,
        Guid facilityId,
        string patternKeyPrefix,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.FactoryOutcomeLearningRecords
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId &&
                r.FacilityId == facilityId &&
                r.LearningDisposition != FactoryRecoveryLearningDisposition.VerifiedSuccess &&
                r.PatternKey.StartsWith(patternKeyPrefix))
            .OrderByDescending(r => r.VerifiedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
