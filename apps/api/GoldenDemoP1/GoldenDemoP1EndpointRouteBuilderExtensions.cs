using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.GoldenDemoP1;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.GoldenDemoP1;

public static class GoldenDemoP1EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGoldenDemoP1Endpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/golden-demo-p1").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapPost("/events/{eventId:guid}/refresh", RefreshAnalysisAsync);
        group.MapPost("/events/{eventId:guid}/approve", ApprovePlanAsync);
        group.MapPost("/events/{eventId:guid}/execute", ExecutePlanAsync);
        group.MapPost("/events/{eventId:guid}/complete-intervention", CompleteInterventionAsync);
        group.MapPost("/events/{eventId:guid}/verify", VerifyOutcomeAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSGoldenDemoP1Api(this IServiceCollection services)
    {
        services.AddNevaxisFactOSGoldenDemoP1();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        IFacilityAccessResolver facilityAccessResolver,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP1Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP1EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        if (!tenantContext.IsAvailable)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteReadAsync(
                ct => service.GetWorkspaceAsync(
                    tenantContext.Current!.TenantId,
                    access.FacilityId,
                    machineAssetId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoP1EndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP1EndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> RefreshAnalysisAsync(
        Guid eventId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP1Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP1EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var result = await service.RefreshAnalysisAsync(eventId, ct);
                    if (result.Event.FacilityId != access.FacilityId)
                    {
                        throw new GoldenDemoP1Exception(
                            GoldenDemoP1FailureCodes.EventNotFound,
                            "Production event not found.");
                    }

                    return result;
                },
                cancellationToken);
            return TypedResults.Ok(GoldenDemoP1EndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP1EndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApprovePlanAsync(
        Guid eventId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveRecoveryPlanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP1Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP1EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Approve, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var plan = await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var result = await service.ApprovePlanAsync(eventId, request, subjectId, ct);
                    if (result.FacilityId != access.FacilityId)
                    {
                        throw new GoldenDemoP1Exception(
                            GoldenDemoP1FailureCodes.EventNotFound,
                            "Recovery plan not found.");
                    }

                    return result;
                },
                cancellationToken);
            return TypedResults.Ok(new { plan.Id, plan.Status, plan.ApprovedAtUtc });
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP1EndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ExecutePlanAsync(
        Guid eventId,
        [FromQuery] Guid? facilityId,
        [FromBody] ExecuteRecoveryPlanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP1Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP1EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var evt = await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var result = await service.ExecutePlanAsync(eventId, request, subjectId, ct);
                    if (result.FacilityId != access.FacilityId)
                    {
                        throw new GoldenDemoP1Exception(
                            GoldenDemoP1FailureCodes.EventNotFound,
                            "Production event not found.");
                    }

                    return result;
                },
                cancellationToken);
            return TypedResults.Ok(new { evt.Id, evt.Status });
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP1EndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> CompleteInterventionAsync(
        Guid eventId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteInterventionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP1Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP1EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var evt = await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var result = await service.CompleteInterventionAsync(eventId, request, subjectId, ct);
                    if (result.FacilityId != access.FacilityId)
                    {
                        throw new GoldenDemoP1Exception(
                            GoldenDemoP1FailureCodes.EventNotFound,
                            "Production event not found.");
                    }

                    return result;
                },
                cancellationToken);
            return TypedResults.Ok(new { evt.Id, evt.Status });
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP1EndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyOutcomeAsync(
        Guid eventId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyOutcomeRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP1Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP1EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Verify, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var run = await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var result = await service.VerifyOutcomeAsync(eventId, request, subjectId, ct);
                    if (result.FacilityId != access.FacilityId)
                    {
                        throw new GoldenDemoP1Exception(
                            GoldenDemoP1FailureCodes.EventNotFound,
                            "Verification run not found.");
                    }

                    return result;
                },
                cancellationToken);
            return TypedResults.Ok(new
            {
                run.Id,
                run.Result,
                run.Summary,
                run.RecoveredGoodPartsPerShift,
                run.VerifiedAtUtc,
            });
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP1EndpointHelpers.MapException(ex);
        }
    }
}
