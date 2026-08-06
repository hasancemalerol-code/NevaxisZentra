namespace CrevaFactOS.Api.Facilities;

public sealed record FacilityAccessProbeResult(
    bool FacilityExistsAndActive,
    bool MembershipExistsAndActive);
