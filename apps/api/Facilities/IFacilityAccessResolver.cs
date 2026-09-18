namespace NevaxisZentra.Api.Facilities;

public interface IFacilityAccessResolver
{
    Task<FacilityAccessResolutionResult> ResolveAsync(
        FacilityAccessRequest request,
        CancellationToken cancellationToken = default);
}
