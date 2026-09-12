using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Development;

/// <summary>
/// Ensures dev-pilot SaaS commercial catalog and active subscription exist even when the
/// main bootstrap transaction rolls back on re-seed conflicts.
/// </summary>
internal static class DevelopmentPilotAccessEnsureService
{
    internal static async Task EnsurePaidAccessAsync(
        IOptions<PersistenceOptions> persistenceOptions,
        IOptions<DevelopmentPilotOptions> pilotOptions,
        CancellationToken cancellationToken = default)
    {
        var pilot = pilotOptions.Value;
        if (!pilot.Enabled)
        {
            return;
        }

        var bootstrapContext = DevelopmentPilotBootstrapDbContextFactory.Create(persistenceOptions.Value);
        await using var dbContext = bootstrapContext.DbContext;

        var seedTimestampUtc = DateTimeOffset.UtcNow;
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == DevelopmentPilotIds.TenantId
                    || candidate.TenantKey == pilot.TenantKey.Trim(),
                cancellationToken);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = DevelopmentPilotIds.TenantId,
                TenantKey = pilot.TenantKey.Trim(),
                DisplayName = pilot.TenantName.Trim(),
                Status = TenantStatus.Active,
                CreatedAtUtc = seedTimestampUtc,
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        bootstrapContext.TenantContextAccessor.Set(
            new TenantContext(
                tenant.Id,
                tenant.TenantKey,
                DevelopmentPilotIds.TenantMembershipId));

        await DevelopmentPilotSaasCommercialSeed.EnsureSaasCommercialDevelopmentCatalogAsync(
            dbContext,
            "production-pilot-operator",
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotSaasSubscriptionSeed.EnsureDevelopmentSubscriptionAsync(
            dbContext,
            tenant.Id,
            "production-pilot-operator",
            seedTimestampUtc,
            cancellationToken);
        await DevelopmentPilotSaasSubscriptionRlsRepair.EnsureRuntimeReadAccessAsync(
            dbContext,
            cancellationToken);
    }
}
