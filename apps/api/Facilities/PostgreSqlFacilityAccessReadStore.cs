using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Api.Facilities;

public sealed class PostgreSqlFacilityAccessReadStore(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IFacilityAccessReadStore
{
    public Task<FacilityAccessProbeResult> ProbeAsync(
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ProbeCoreAsync(tenantId, facilityId, tenantMembershipId, ct),
            cancellationToken);

    private async Task<FacilityAccessProbeResult> ProbeCoreAsync(
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken)
    {
        var facilityExistsAndActive = await dbContext.Facilities
            .AsNoTracking()
            .AnyAsync(
                facility => facility.TenantId == tenantId
                    && facility.Id == facilityId
                    && facility.Status == FacilityStatus.Active,
                cancellationToken);

        if (!facilityExistsAndActive)
        {
            return new FacilityAccessProbeResult(false, false);
        }

        var membershipExistsAndActive = await dbContext.FacilityMemberships
            .AsNoTracking()
            .AnyAsync(
                membership => membership.TenantId == tenantId
                    && membership.FacilityId == facilityId
                    && membership.TenantMembershipId == tenantMembershipId
                    && membership.Status == FacilityMembershipStatus.Active,
                cancellationToken);

        return new FacilityAccessProbeResult(true, membershipExistsAndActive);
    }
}
