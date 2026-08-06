using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Facilities;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Api.Facilities;

public sealed class PostgreSqlFacilityAccessReadStore(CrevaFactOSDbContext dbContext)
    : IFacilityAccessReadStore
{
    public async Task<FacilityAccessProbeResult> ProbeAsync(
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken = default)
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
