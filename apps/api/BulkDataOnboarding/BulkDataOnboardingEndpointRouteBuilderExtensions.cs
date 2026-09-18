using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.BulkDataOnboarding;

namespace NevaxisZentra.Api.BulkDataOnboarding;

public static class BulkDataOnboardingEndpointRouteBuilderExtensions
{
    public static IServiceCollection AddNevaxisZentraBulkDataOnboardingApi(this IServiceCollection services) =>
        services.AddNevaxisZentraBulkDataOnboarding();

    public static IEndpointRouteBuilder MapBulkDataOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bulk-data-onboarding").RequireAuthorization();

        group.MapPost("/jobs", CreateJobAsync);
        group.MapGet("/jobs/{jobId:guid}", GetWorkspaceAsync);
        group.MapPost("/jobs/{jobId:guid}/discover", DiscoverAsync);
        group.MapPost("/jobs/{jobId:guid}/stage-validate", StageValidateAsync);
        group.MapPost("/jobs/{jobId:guid}/dry-run", DryRunAsync);
        group.MapPost("/jobs/{jobId:guid}/commit", CommitAsync);
        group.MapPost("/jobs/{jobId:guid}/rollback", RollbackAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateJobAsync(
        [FromBody] CreateBulkImportJobRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.CreateJobAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            request,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var workspace = await service.GetWorkspaceAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(workspace);
    }

    private static async Task<IResult> DiscoverAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.DiscoverAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> StageValidateAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.StageAndValidateAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> DryRunAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var result = await service.DryRunAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CommitAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Commit, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.CommitAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> RollbackAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var job = await service.RollbackAsync(
                new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
                jobId,
                cancellationToken);
            return TypedResults.Ok(job);
        }
        catch (InvalidOperationException ex) when (ex.Message == BulkFailureCodes.RollbackBlocked)
        {
            return TypedResults.Conflict(new { code = BulkFailureCodes.RollbackBlocked });
        }
    }

    private static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, TypedResults.Forbid());
        }

        return (accessResult.Access, null);
    }
}
