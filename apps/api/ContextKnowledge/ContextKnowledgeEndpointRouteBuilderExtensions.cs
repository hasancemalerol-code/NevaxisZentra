using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.ContextKnowledge;

namespace NevaxisFactOS.Api.ContextKnowledge;

public static class ContextKnowledgeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapContextKnowledgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/context-knowledge").RequireAuthorization();

        group.MapPost("/canonical-state/resolve", ResolveCanonicalStateAsync);
        group.MapPost("/build", BuildWorkerContextAsync);
        group.MapPost("/knowledge/promote", PromoteKnowledgeAsync);
        group.MapGet("/knowledge", ListKnowledgeAsync);
        group.MapPost("/snapshots", CreateSnapshotAsync);
        group.MapGet("/snapshots/{snapshotId:guid}", GetSnapshotAsync);
        group.MapGet("/lifecycle-policies", ListLifecyclePoliciesAsync);
        group.MapPost("/lifecycle-policies", UpsertLifecyclePolicyAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSContextKnowledge(this IServiceCollection services)
    {
        services.AddScoped<IContextKnowledgeService, ContextKnowledgeService>();
        return services;
    }

    private static async Task<IResult> ResolveCanonicalStateAsync(
        ResolveCanonicalStateRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.ResolveCanonicalStateAsync(
                access.FacilityId,
                access.TenantId,
                request.EntityType,
                request.EntityId,
                request.Claims,
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ContextKnowledgeException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> BuildWorkerContextAsync(
        ContextBuildRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var scopedRequest = request with
        {
            Scope = request.Scope with
            {
                TenantId = access.TenantId,
                FacilityId = access.FacilityId,
            },
        };

        try
        {
            var package = await service.BuildWorkerContextAsync(scopedRequest, cancellationToken);
            return TypedResults.Ok(package);
        }
        catch (ContextKnowledgeException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> PromoteKnowledgeAsync(
        KnowledgePromotionRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Promote, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.PromoteKnowledgeAsync(
                access.FacilityId, access.TenantId, request, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ContextKnowledgeException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> ListKnowledgeAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] int take,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var records = await service.ListVerifiedKnowledgeAsync(
            access.FacilityId, take <= 0 ? 20 : Math.Min(take, 100), cancellationToken);
        return TypedResults.Ok(records);
    }

    private static async Task<IResult> CreateSnapshotAsync(
        CreateContextSnapshotApiRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var snapshotRequest = new ContextSnapshotRequest(
                request.Scope with { TenantId = access.TenantId, FacilityId = access.FacilityId },
                request.ContextPackage,
                request.DigitalWorkCaseId,
                access.IdentitySubjectId.ToString(),
                request.SnapshotCode);

            var summary = await service.CreateSnapshotAsync(
                access.FacilityId, access.TenantId, snapshotRequest, cancellationToken);
            return TypedResults.Ok(summary);
        }
        catch (ContextKnowledgeException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetSnapshotAsync(
        Guid snapshotId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var snapshot = await service.GetSnapshotAsync(access.FacilityId, snapshotId, cancellationToken);
        return snapshot is null
            ? TypedResults.NotFound(new { code = ContextKnowledgeFailureCodes.NotFound, message = "Snapshot not found." })
            : TypedResults.Ok(snapshot);
    }

    private static async Task<IResult> ListLifecyclePoliciesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var policies = await service.ListLifecyclePoliciesAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(policies);
    }

    private static async Task<IResult> UpsertLifecyclePolicyAsync(
        DataLifecyclePolicyDefinition policy,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContextKnowledgeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContextKnowledgePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var saved = await service.UpsertLifecyclePolicyAsync(
            access.FacilityId, access.TenantId, policy, cancellationToken);
        return TypedResults.Ok(saved);
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

    private static IResult MapException(ContextKnowledgeException ex) =>
        ex.FailureCode switch
        {
            ContextKnowledgeFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
        };
}

public sealed record ResolveCanonicalStateRequest(
    string EntityType,
    string EntityId,
    IReadOnlyList<ContextSourceClaim> Claims);

public sealed record CreateContextSnapshotApiRequest(
    ContextScope Scope,
    WorkerContextPackage ContextPackage,
    Guid? DigitalWorkCaseId,
    string? SnapshotCode = null);
