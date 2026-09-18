using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoCustomsFile;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.GoldenDemoCustomsFile;

public static class GoldenDemoCustomsFileEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGoldenDemoCustomsFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/golden-demo-customs-file").RequireAuthorization();
        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/assign", AssignOutcomeAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/evaluate", EvaluateAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/corrective-work/{workId:guid}/complete", CompleteCorrectiveWorkAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/recheck", RecheckEvidenceAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/approve", ApproveAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/assemble", AssemblePackageAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/verify", VerifyAsync);
        group.MapPost("/outcomes/{outcomeId:guid}/reset", ResetAsync);
        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraGoldenDemoCustomsFileApi(this IServiceCollection services)
    {
        services.AddNevaxisZentraGoldenDemoCustomsFile();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Read, facilityAccessResolver, cancellationToken);
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
                ct => service.GetWorkspaceAsync(tenantContext.Current!.TenantId, access.FacilityId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> AssignOutcomeAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignCustomsOutcomeRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.AssignOutcomeAsync(outcomeId, request, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> EvaluateAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.RunWorkerEvaluationAsync(outcomeId, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> CompleteCorrectiveWorkAsync(
        Guid outcomeId,
        Guid workId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteCustomsCorrectiveWorkRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.CompleteCorrectiveWorkAsync(outcomeId, workId, request, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> RecheckEvidenceAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.RecheckEvidenceAsync(outcomeId, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApproveAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveCustomsPackageRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Approve, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.ApprovePackageAsync(outcomeId, request, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> AssemblePackageAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.AssemblePackageAsync(outcomeId, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyCustomsOutcomeRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Verify, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var snapshot = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.VerifyOutcomeAsync(outcomeId, request, subjectId, ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoCustomsFileEndpointHelpers.MapWorkspace(snapshot));
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ResetAsync(
        Guid outcomeId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoCustomsFileService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoCustomsFileEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoCustomsFilePermissionKeys.Reset, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            await tenantDatabaseSession.ExecuteWriteAsync<object?>(
                async ct =>
                {
                    await service.ResetPilotOutcomeAsync(outcomeId, ct);
                    return null;
                },
                cancellationToken);
            return TypedResults.Ok(new { resetStatus = GoldenDemoCustomsFileOutcomeStatuses.Assigned });
        }
        catch (GoldenDemoCustomsFileException ex)
        {
            return GoldenDemoCustomsFileEndpointHelpers.MapException(ex);
        }
    }
}
