namespace NevaxisZentra.Api.Facilities;

public sealed record FacilityAccessProbeResult(
    bool FacilityExistsAndActive,
    bool MembershipExistsAndActive);
