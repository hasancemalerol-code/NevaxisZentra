using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.GoldenDemoCustomsFile;

public interface IGoldenDemoCustomsFileRepository
{
    Task<GoldenDemoCustomsFileOutcome?> GetOutcomeAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileOutcome?> GetPilotOutcomeAsync(Guid tenantId, Guid facilityId, CancellationToken cancellationToken);
    Task UpdateOutcomeAsync(GoldenDemoCustomsFileOutcome outcome, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoldenDemoCustomsFileDocument>> GetDocumentsAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task AddDocumentAsync(GoldenDemoCustomsFileDocument document, CancellationToken cancellationToken);
    Task UpdateDocumentAsync(GoldenDemoCustomsFileDocument document, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoldenDemoCustomsFileBlocker>> GetBlockersAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task AddBlockerAsync(GoldenDemoCustomsFileBlocker blocker, CancellationToken cancellationToken);
    Task UpdateBlockerAsync(GoldenDemoCustomsFileBlocker blocker, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoldenDemoCustomsFileCorrectiveWork>> GetCorrectiveWorksAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileCorrectiveWork?> GetCorrectiveWorkAsync(Guid workId, CancellationToken cancellationToken);
    Task AddCorrectiveWorkAsync(GoldenDemoCustomsFileCorrectiveWork work, CancellationToken cancellationToken);
    Task UpdateCorrectiveWorkAsync(GoldenDemoCustomsFileCorrectiveWork work, CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFilePackage?> GetPackageAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task AddPackageAsync(GoldenDemoCustomsFilePackage package, CancellationToken cancellationToken);
    Task<GoldenDemoCustomsFileVerificationRun?> GetLatestVerificationAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task AddVerificationAsync(GoldenDemoCustomsFileVerificationRun verification, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoldenDemoCustomsFileActivity>> GetActivitiesAsync(Guid outcomeId, CancellationToken cancellationToken);
    Task AddActivityAsync(GoldenDemoCustomsFileActivity activity, CancellationToken cancellationToken);
    Task ResetPilotOutcomeAsync(Guid outcomeId, CancellationToken cancellationToken);
}

public sealed class GoldenDemoCustomsFileRepository : IGoldenDemoCustomsFileRepository
{
    private readonly NevaxisFactOSDbContext dbContext;

    public GoldenDemoCustomsFileRepository(NevaxisFactOSDbContext dbContext) => this.dbContext = dbContext;

    public Task<GoldenDemoCustomsFileOutcome?> GetOutcomeAsync(Guid outcomeId, CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCustomsFileOutcomes.FirstOrDefaultAsync(o => o.Id == outcomeId, cancellationToken);

    public Task<GoldenDemoCustomsFileOutcome?> GetPilotOutcomeAsync(
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCustomsFileOutcomes
            .Where(o => o.TenantId == tenantId && o.FacilityId == facilityId)
            .OrderBy(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpdateOutcomeAsync(GoldenDemoCustomsFileOutcome outcome, CancellationToken cancellationToken)
    {
        outcome.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoldenDemoCustomsFileDocument>> GetDocumentsAsync(
        Guid outcomeId,
        CancellationToken cancellationToken) =>
        await dbContext.GoldenDemoCustomsFileDocuments
            .Where(d => d.OutcomeId == outcomeId)
            .OrderBy(d => d.DocumentType)
            .ThenBy(d => d.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task AddDocumentAsync(GoldenDemoCustomsFileDocument document, CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCustomsFileDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDocumentAsync(GoldenDemoCustomsFileDocument document, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<GoldenDemoCustomsFileBlocker>> GetBlockersAsync(
        Guid outcomeId,
        CancellationToken cancellationToken) =>
        await dbContext.GoldenDemoCustomsFileBlockers
            .Where(b => b.OutcomeId == outcomeId)
            .OrderBy(b => b.DetectedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddBlockerAsync(GoldenDemoCustomsFileBlocker blocker, CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCustomsFileBlockers.Add(blocker);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBlockerAsync(GoldenDemoCustomsFileBlocker blocker, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<GoldenDemoCustomsFileCorrectiveWork>> GetCorrectiveWorksAsync(
        Guid outcomeId,
        CancellationToken cancellationToken) =>
        await dbContext.GoldenDemoCustomsFileCorrectiveWorks
            .Where(w => w.OutcomeId == outcomeId)
            .OrderBy(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<GoldenDemoCustomsFileCorrectiveWork?> GetCorrectiveWorkAsync(
        Guid workId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCustomsFileCorrectiveWorks.FirstOrDefaultAsync(w => w.Id == workId, cancellationToken);

    public async Task AddCorrectiveWorkAsync(GoldenDemoCustomsFileCorrectiveWork work, CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCustomsFileCorrectiveWorks.Add(work);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCorrectiveWorkAsync(GoldenDemoCustomsFileCorrectiveWork work, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public Task<GoldenDemoCustomsFilePackage?> GetPackageAsync(Guid outcomeId, CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCustomsFilePackages
            .Where(p => p.OutcomeId == outcomeId)
            .OrderByDescending(p => p.PackageVersion)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddPackageAsync(GoldenDemoCustomsFilePackage package, CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCustomsFilePackages.Add(package);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<GoldenDemoCustomsFileVerificationRun?> GetLatestVerificationAsync(
        Guid outcomeId,
        CancellationToken cancellationToken) =>
        dbContext.GoldenDemoCustomsFileVerificationRuns
            .Where(v => v.OutcomeId == outcomeId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddVerificationAsync(GoldenDemoCustomsFileVerificationRun verification, CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCustomsFileVerificationRuns.Add(verification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoldenDemoCustomsFileActivity>> GetActivitiesAsync(
        Guid outcomeId,
        CancellationToken cancellationToken) =>
        await dbContext.GoldenDemoCustomsFileActivities
            .Where(a => a.OutcomeId == outcomeId)
            .OrderBy(a => a.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddActivityAsync(GoldenDemoCustomsFileActivity activity, CancellationToken cancellationToken)
    {
        dbContext.GoldenDemoCustomsFileActivities.Add(activity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPilotOutcomeAsync(Guid outcomeId, CancellationToken cancellationToken)
    {
        var verifications = await dbContext.GoldenDemoCustomsFileVerificationRuns
            .Where(v => v.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCustomsFileVerificationRuns.RemoveRange(verifications);

        var packages = await dbContext.GoldenDemoCustomsFilePackages
            .Where(p => p.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCustomsFilePackages.RemoveRange(packages);

        var works = await dbContext.GoldenDemoCustomsFileCorrectiveWorks
            .Where(w => w.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCustomsFileCorrectiveWorks.RemoveRange(works);

        var blockers = await dbContext.GoldenDemoCustomsFileBlockers
            .Where(b => b.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCustomsFileBlockers.RemoveRange(blockers);

        var activities = await dbContext.GoldenDemoCustomsFileActivities
            .Where(a => a.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCustomsFileActivities.RemoveRange(activities);

        var documents = await dbContext.GoldenDemoCustomsFileDocuments
            .Where(d => d.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
        dbContext.GoldenDemoCustomsFileDocuments.RemoveRange(documents);

        var outcome = await dbContext.GoldenDemoCustomsFileOutcomes
            .FirstOrDefaultAsync(o => o.Id == outcomeId, cancellationToken);
        if (outcome is not null)
        {
            outcome.Status = GoldenDemoCustomsFileOutcomeStatuses.Assigned;
            outcome.ApprovedBySubjectId = null;
            outcome.ApprovedAtUtc = null;
            outcome.CurrentPackageId = null;
            outcome.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
