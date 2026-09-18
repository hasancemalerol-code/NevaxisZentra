using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.ShiftOperations;

public interface IShiftOperationsRepository
{
    Task<OperationalShiftInstance?> GetShiftInstanceAsync(Guid id, CancellationToken cancellationToken);
    Task<OperationalShiftInstance?> GetCurrentShiftAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationalShiftInstance>> ListHistoryAsync(Guid facilityId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShiftOpenItem>> ListOpenItemsAsync(Guid shiftInstanceId, CancellationToken cancellationToken);
    Task<ShiftHandoverAcceptance?> GetAcceptanceAsync(Guid shiftInstanceId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class ShiftOperationsRepository : IShiftOperationsRepository
{
    private readonly NevaxisZentraDbContext dbContext;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public ShiftOperationsRepository(
        NevaxisZentraDbContext dbContext,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.dbContext = dbContext;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<OperationalShiftInstance?> GetShiftInstanceAsync(Guid id, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => GetShiftInstanceCoreAsync(id, ct), cancellationToken);

    public Task<OperationalShiftInstance?> GetCurrentShiftAsync(Guid facilityId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct => GetCurrentShiftCoreAsync(facilityId, ct), cancellationToken);

    public Task<IReadOnlyList<OperationalShiftInstance>> ListHistoryAsync(
        Guid facilityId, int limit, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListHistoryCoreAsync(facilityId, limit, ct),
            cancellationToken);

    public Task<IReadOnlyList<ShiftOpenItem>> ListOpenItemsAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListOpenItemsCoreAsync(shiftInstanceId, ct),
            cancellationToken);

    public Task<ShiftHandoverAcceptance?> GetAcceptanceAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.ShiftHandoverAcceptances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ShiftInstanceId == shiftInstanceId, ct),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await dbContext.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);

    private async Task<OperationalShiftInstance?> GetShiftInstanceCoreAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.OperationalShiftInstances
            .Include(s => s.OpenItems)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    private async Task<OperationalShiftInstance?> GetCurrentShiftCoreAsync(
        Guid facilityId, CancellationToken cancellationToken) =>
        await dbContext.OperationalShiftInstances
            .Include(s => s.OpenItems)
            .Where(s => s.FacilityId == facilityId &&
                        (s.Status == ShiftInstanceStatuses.Active ||
                         s.Status == ShiftInstanceStatuses.Closing ||
                         s.Status == ShiftInstanceStatuses.HandoverPending))
            .OrderByDescending(s => s.ActualStartUtc)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<OperationalShiftInstance>> ListHistoryCoreAsync(
        Guid facilityId, int limit, CancellationToken cancellationToken) =>
        await dbContext.OperationalShiftInstances
            .AsNoTracking()
            .Where(s => s.FacilityId == facilityId)
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.PlannedStartUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<ShiftOpenItem>> ListOpenItemsCoreAsync(
        Guid shiftInstanceId, CancellationToken cancellationToken) =>
        await dbContext.ShiftOpenItems
            .AsNoTracking()
            .Where(i => i.ShiftInstanceId == shiftInstanceId)
            .OrderByDescending(i => i.HandoverPriority)
            .ThenBy(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
