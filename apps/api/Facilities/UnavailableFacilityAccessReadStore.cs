namespace NevaxisZentra.Api.Facilities;

internal sealed class UnavailableFacilityAccessReadStore : IFacilityAccessReadStore
{
    public Task<FacilityAccessProbeResult> ProbeAsync(
        Guid tenantId,
        Guid facilityId,
        Guid tenantMembershipId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Facility access data is unavailable because persistence is disabled.");
}
