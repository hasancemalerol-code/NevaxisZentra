using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Teams;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Teams;

public static class TeamsNotificationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamsNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams/notifications").RequireAuthorization();
        group.MapGet("/", ListNotificationsAsync);
        group.MapPost("/{notificationId:guid}/read", MarkReadAsync);
        return endpoints;
    }

    private static async Task<IResult> ListNotificationsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] bool? unreadOnly,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsNotificationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveNotificationReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveNotificationCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var list = await repository.ListAsync(
            context,
            capabilities,
            unreadOnly ?? false,
            page ?? 1,
            pageSize ?? TeamsConstants.DefaultPageSize,
            cancellationToken);
        return TypedResults.Ok(TeamsEndpointHelpers.MapNotificationList(list));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid notificationId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsNotificationRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveNotificationReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveNotificationCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.MarkReadAsync(
            context,
            capabilities,
            new MarkTeamsNotificationReadCommand(notificationId),
            cancellationToken);
        return TeamsEndpointHelpers.MapNotificationWriteResult(result);
    }
}
