using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Api.DigitalWorkforce;

public static class DigitalWorkforceWorksEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapDigitalWorkforceWorksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/digital-workforce/works").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/cases/{caseId:guid}", GetCaseDetailAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? queue,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceWorksService worksService,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var snapshot = await worksService.GetWorkspaceAsync(
            queue,
            search,
            limit ?? 50,
            authorizationContext.Current?.IdentitySubjectId.ToString(),
            cancellationToken);
        return TypedResults.Ok(snapshot);
    }

    private static async Task<IResult> GetCaseDetailAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceWorksService worksService,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var (approvalAccess, _) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.ApprovalDecide, facilityAccessResolver, cancellationToken);
        var canDecideApproval = approvalAccess is not null;

        try
        {
            var detail = await worksService.GetCaseDetailAsync(
                caseId,
                authorizationContext.Current?.IdentitySubjectId.ToString(),
                canDecideApproval,
                cancellationToken);
            return TypedResults.Ok(detail);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }
}
