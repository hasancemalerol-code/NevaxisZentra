using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotDigitalWorkforceSeed
{
    public static async Task EnsureDigitalWorkforcePilotDataAsync(
        NevaxisFactOSDbContext dbContext,
        TenantContextAccessor tenantContextAccessor,
        Guid tenantId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        await dbContext.SetTenantSessionContextAsync(tenantId, cancellationToken);
        var repository = new DigitalWorkforceRepository(
            dbContext,
            new TenantDatabaseSession(dbContext, tenantContextAccessor),
            tenantContextAccessor);
        await repository.EnsureCanonicalWorkersAsync(tenantId, facilityId, cancellationToken);

        var runtimeState = await dbContext.DigitalWorkforceRuntimeStates
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
        if (runtimeState is null)
        {
            dbContext.DigitalWorkforceRuntimeStates.Add(new DigitalWorkforceRuntimeState
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                ConcurrencyToken = 1,
            });
        }
    }
}
