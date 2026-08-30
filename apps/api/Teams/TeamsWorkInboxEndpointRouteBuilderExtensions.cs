using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Teams;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Teams;

public static class TeamsWorkInboxEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamsWorkInboxEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams/work-inbox").RequireAuthorization();
        group.MapGet("/", GetInboxAsync);
        return endpoints;
    }

    private static async Task<IResult> GetInboxAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsWorkInboxRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveWorkInboxCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var inbox = await repository.GetInboxAsync(context, capabilities, cancellationToken);
        return TypedResults.Ok(TeamsEndpointHelpers.MapWorkInbox(inbox));
    }
}
