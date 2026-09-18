using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.FactoryOutcomeExecution;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.FactoryDecisionCenter;

public interface IFactoryDecisionCenterRepository
{
    Task<IReadOnlyList<DigitalWorkCase>> ListDecisionCasesAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<DigitalWorkCase?> GetDecisionCaseAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkCase>> ListChildCasesAsync(Guid rootCaseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DigitalWorkVerification>> ListVerificationsAsync(Guid caseId, CancellationToken cancellationToken);
    Task<DigitalWorkApproval?> GetPendingApprovalAsync(Guid caseId, CancellationToken cancellationToken);
    Task<string?> GetFacilityLabelAsync(Guid facilityId, CancellationToken cancellationToken);
}

public sealed class FactoryDecisionCenterRepository : IFactoryDecisionCenterRepository
{
    private readonly NevaxisZentraDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public FactoryDecisionCenterRepository(
        NevaxisZentraDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<IReadOnlyList<DigitalWorkCase>> ListDecisionCasesAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListDecisionCasesCoreAsync(facilityId, ct),
            cancellationToken);

    public Task<DigitalWorkCase?> GetDecisionCaseAsync(
        Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetDecisionCaseCoreAsync(caseId, ct),
            cancellationToken);

    public Task<IReadOnlyList<DigitalWorkCase>> ListChildCasesAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListChildCasesCoreAsync(rootCaseId, ct),
            cancellationToken);

    public Task<IReadOnlyList<DigitalWorkVerification>> ListVerificationsAsync(
        Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<IReadOnlyList<DigitalWorkVerification>>(
            async ct => await dbContext.DigitalWorkVerifications
                .AsNoTracking()
                .Where(v => v.CaseId == caseId)
                .OrderByDescending(v => v.VerifiedAtUtc)
                .ToListAsync(ct),
            cancellationToken);

    public Task<DigitalWorkApproval?> GetPendingApprovalAsync(
        Guid caseId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.DigitalWorkApprovals
                .AsNoTracking()
                .Where(a => a.CaseId == caseId && a.Status == DigitalWorkApprovalStatuses.Pending)
                .OrderByDescending(a => a.CreatedAtUtc)
                .FirstOrDefaultAsync(ct),
            cancellationToken);

    public Task<string?> GetFacilityLabelAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var facility = await dbContext.Facilities
                    .AsNoTracking()
                    .Where(f => f.Id == facilityId)
                    .Select(f => new { f.Name, f.Code })
                    .FirstOrDefaultAsync(ct);
                return facility is null ? null : $"{facility.Name} ({facility.Code})";
            },
            cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkCase>> ListDecisionCasesCoreAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Include(c => c.PrimaryWorker)
            .Where(c => c.FacilityId == facilityId &&
                        c.ParentCaseId == null &&
                        (c.Source == FactoryDecisionCenterConstants.SourceModule ||
                         c.Source == FactoryOutcomeExecutionConstants.SourceModule ||
                         (c.CorrelationKey != null &&
                          (c.CorrelationKey.Contains("ord-2026-0915-047") ||
                           c.CorrelationKey.Contains("outcome:order:")))))
            .OrderByDescending(c => c.Severity == "critical")
            .ThenByDescending(c => c.Severity == "high")
            .ThenByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

    private async Task<DigitalWorkCase?> GetDecisionCaseCoreAsync(
        Guid caseId, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .Include(c => c.PrimaryWorker)
            .Include(c => c.Activities)
            .Include(c => c.Approvals)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

    private async Task<IReadOnlyList<DigitalWorkCase>> ListChildCasesCoreAsync(
        Guid rootCaseId, CancellationToken cancellationToken) =>
        await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Include(c => c.PrimaryWorker)
            .Where(c => c.OrchestrationRootCaseId == rootCaseId && c.Id != rootCaseId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
