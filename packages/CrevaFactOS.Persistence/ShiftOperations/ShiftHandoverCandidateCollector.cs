using CrevaFactOS.Persistence.DigitalWorkforce;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ShiftOperations;

public interface IShiftHandoverCandidateCollector
{
    Task<IReadOnlyList<ShiftOpenItem>> CollectAsync(
        OperationalShiftInstance shiftInstance,
        CancellationToken cancellationToken);
}

public sealed class ShiftHandoverCandidateCollector : IShiftHandoverCandidateCollector
{
    private readonly CrevaFactOSDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public ShiftHandoverCandidateCollector(
        CrevaFactOSDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<IReadOnlyList<ShiftOpenItem>> CollectAsync(
        OperationalShiftInstance shiftInstance,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => CollectCoreAsync(shiftInstance, ct),
            cancellationToken);

    private async Task<IReadOnlyList<ShiftOpenItem>> CollectCoreAsync(
        OperationalShiftInstance shiftInstance,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = new List<ShiftOpenItem>();

        var activeCases = await dbContext.DigitalWorkCases
            .AsNoTracking()
            .Where(c => c.TenantId == shiftInstance.TenantId &&
                        c.FacilityId == shiftInstance.FacilityId &&
                        c.Status != DigitalWorkCaseStatuses.Closed)
            .Include(c => c.PrimaryWorker)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var workCase in activeCases)
        {
            candidates.Add(new ShiftOpenItem
            {
                Id = Guid.NewGuid(),
                TenantId = shiftInstance.TenantId,
                FacilityId = shiftInstance.FacilityId,
                ShiftInstanceId = shiftInstance.Id,
                SourceType = "digital-workforce",
                SourceId = workCase.Id.ToString(),
                Category = MapWorkerCategory(workCase.PrimaryWorker?.Code),
                Title = workCase.Title,
                Description = workCase.Description,
                Severity = workCase.Severity,
                Status = ShiftOpenItemStatuses.Open,
                PrimaryWorkerId = workCase.PrimaryWorkerId,
                DigitalWorkCaseId = workCase.Id,
                RequiresHandover = true,
                HandoverPriority = ShiftHandoverPriorityEngine.ResolvePriority(
                    ShiftOpenItemCategories.DigitalWorkforce,
                    workCase.Severity,
                    workCase.RequiresApproval,
                    0,
                    workCase.TargetAtUtc),
                VerificationRequired = workCase.VerificationState != DigitalWorkVerificationResults.Passed,
                ProvenanceJson = """{"source":"digital-workforce","module":"digital-workforce"}""",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        var pendingApprovals = await dbContext.DigitalWorkApprovals
            .AsNoTracking()
            .Where(a => a.TenantId == shiftInstance.TenantId &&
                        a.Status == DigitalWorkApprovalStatuses.Pending)
            .Take(30)
            .ToListAsync(cancellationToken);

        foreach (var approval in pendingApprovals)
        {
            candidates.Add(new ShiftOpenItem
            {
                Id = Guid.NewGuid(),
                TenantId = shiftInstance.TenantId,
                FacilityId = shiftInstance.FacilityId,
                ShiftInstanceId = shiftInstance.Id,
                SourceType = "digital-workforce-approval",
                SourceId = approval.Id.ToString(),
                Category = ShiftOpenItemCategories.Approval,
                Title = $"Bekleyen onay: {approval.Action}",
                Description = approval.BusinessReason,
                Severity = ShiftHandoverPriorities.High,
                Status = ShiftOpenItemStatuses.Open,
                RequiresHandover = true,
                HandoverPriority = ShiftHandoverPriorities.High,
                VerificationRequired = true,
                ProvenanceJson = """{"source":"digital-workforce","module":"approval"}""",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        return candidates;
    }

    private static string MapWorkerCategory(string? workerCode) => workerCode switch
    {
        DigitalWorkforceWorkerCodes.MaintenanceResolution => ShiftOpenItemCategories.Maintenance,
        DigitalWorkforceWorkerCodes.MaterialShortage => ShiftOpenItemCategories.Material,
        DigitalWorkforceWorkerCodes.QualityNcr => ShiftOpenItemCategories.Quality,
        DigitalWorkforceWorkerCodes.SupplierRecovery => ShiftOpenItemCategories.Supplier,
        DigitalWorkforceWorkerCodes.MoldLifecycle => ShiftOpenItemCategories.Mold,
        DigitalWorkforceWorkerCodes.ProductionRecovery => ShiftOpenItemCategories.Production,
        _ => ShiftOpenItemCategories.DigitalWorkforce,
    };
}
