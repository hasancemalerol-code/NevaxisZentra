namespace CrevaFactOS.Api.Facilities;

public interface IFacilityAccessReadStore
{
    Task<FacilityAccessProbeResult> ProbeAsync(
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken = default);
}
