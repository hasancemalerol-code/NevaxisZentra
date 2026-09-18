using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.AdminAccess;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.SaasIdentity;
using NevaxisZentra.Persistence.SaasSubscription;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.AdminAccess;

public static class AdminAccessEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdminAccessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin-access").RequireAuthorization();

        group.MapGet("/permissions", ListPermissionsAsync);
        group.MapGet("/roles", ListRolesAsync);
        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);
        group.MapGet("/members", ListMembersAsync);
        group.MapGet("/invitations", ListInvitationsAsync);
        group.MapGet("/facilities", ListFacilitiesAsync);
        group.MapGet("/sessions", ListTenantSessionsAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapPatch("/roles/{roleReference}", PatchRoleAsync);
        group.MapPost("/roles/{roleReference}/permissions", SetRolePermissionsAsync);
        group.MapGet("/me/effective-access", GetCurrentEffectiveAccessAsync);
        group.MapGet("/members/{memberReference}/effective-access", GetMemberEffectiveAccessAsync);
        group.MapPut("/members/{memberReference}/roles", PutMemberRolesAsync);
        group.MapPut("/members/{memberReference}/facilities", PutMemberFacilitiesAsync);
        group.MapPut("/members/{memberReference}/permission-overrides", PutMemberOverridesAsync);
        group.MapPost("/invitations", CreateInvitationAsync);
        group.MapPost("/members/{memberReference}/suspend", SuspendMemberAsync);
        group.MapPost("/members/{memberReference}/reactivate", ReactivateMemberAsync);
        group.MapPost("/sessions/{sessionReference}/revoke", RevokeSessionAsync);
        group.MapPost("/members/{memberReference}/sessions/revoke-all", RevokeAllMemberSessionsAsync);
        group.MapGet("/audit-events", ListAuditEventsAsync);

        return endpoints;
    }

    private static async Task<IResult> ListPermissionsAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var permissions = await repository.ListPermissionsAsync(cancellationToken);
        return TypedResults.Ok(new { permissions });
    }

    private static async Task<IResult> ListRolesAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var roles = await repository.ListRolesAsync(cancellationToken);
        return TypedResults.Ok(new { roles });
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromBody] CreateAdminAccessRoleRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.CreateRoleAsync(
            new CreateAdminAccessRoleCommand(request.RoleKey, request.DisplayName, request.Description, request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { created = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> PatchRoleAsync(
        string roleReference,
        [FromBody] PatchAdminAccessRoleRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.PatchRoleAsync(
            new PatchAdminAccessRoleCommand(
                roleReference,
                request.DisplayName,
                request.Description,
                request.Status,
                request.ExpectedVersion,
                request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> SetRolePermissionsAsync(
        string roleReference,
        [FromBody] SetAdminAccessRolePermissionsRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.SetRolePermissionsAsync(
            new SetAdminAccessRolePermissionsCommand(
                roleReference,
                (request.Grants ?? []).Select(g => new AdminAccessPermissionGrantInput(g.PermissionKey, g.RecordScope, g.Effect)).ToArray(),
                request.ExpectedVersion,
                request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> GetCurrentEffectiveAccessAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: false, cancellationToken);
        if (actor is null) return failure!;

        var access = await repository.GetCurrentEffectiveAccessAsync(actor.MembershipId, cancellationToken);
        return access is null
            ? AdminAccessEndpointHelpers.Forbidden(AdminAccessFailureCodes.Forbidden, "Effective access could not be resolved.")
            : TypedResults.Ok(access);
    }

    private static async Task<IResult> GetMemberEffectiveAccessAsync(
        string memberReference,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var access = await repository.GetEffectiveAccessAsync(memberReference, cancellationToken);
        return access is null
            ? TypedResults.Json(
                AdminAccessEndpointHelpers.Problem(404, AdminAccessFailureCodes.NotFound, "Member was not found."),
                statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(access);
    }

    private static async Task<IResult> PutMemberRolesAsync(
        string memberReference,
        [FromBody] PutAdminAccessMemberRolesRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.PutMemberRolesAsync(
            new PutAdminAccessMemberRolesCommand(
                memberReference,
                request.RoleReferences ?? [],
                request.ExpectedAuthorizationVersion,
                request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> PutMemberFacilitiesAsync(
        string memberReference,
        [FromBody] PutAdminAccessMemberFacilitiesRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.PutMemberFacilitiesAsync(
            new PutAdminAccessMemberFacilitiesCommand(
                memberReference,
                request.FacilityCodes ?? [],
                request.ExpectedAuthorizationVersion,
                request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> PutMemberOverridesAsync(
        string memberReference,
        [FromBody] PutAdminAccessMemberOverridesRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.PutMemberOverridesAsync(
            new PutAdminAccessMemberOverridesCommand(
                memberReference,
                (request.Overrides ?? []).Select(o => new AdminAccessPermissionOverrideInput(o.PermissionKey, o.Effect, o.RecordScope)).ToArray(),
                request.ExpectedAuthorizationVersion,
                request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> ListAuditEventsAsync(
        int? limit,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var events = await repository.ListAuditEventsAsync(limit ?? 50, cancellationToken);
        return TypedResults.Ok(new { events });
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var summary = await repository.GetWorkspaceSummaryAsync(cancellationToken);
        return summary is null
            ? AdminAccessEndpointHelpers.Forbidden(AdminAccessFailureCodes.Forbidden, "Workspace summary could not be resolved.")
            : TypedResults.Ok(summary);
    }

    private static async Task<IResult> ListMembersAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var members = await repository.ListMembersAsync(cancellationToken);
        return TypedResults.Ok(new { members });
    }

    private static async Task<IResult> ListInvitationsAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var invitations = await repository.ListInvitationsAsync(cancellationToken);
        return TypedResults.Ok(new { invitations });
    }

    private static async Task<IResult> ListFacilitiesAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var facilities = await repository.ListFacilitiesAsync(cancellationToken);
        return TypedResults.Ok(new { facilities });
    }

    private static async Task<IResult> ListTenantSessionsAsync(
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveReadAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, requireAdminAccessRead: true, cancellationToken);
        if (actor is null) return failure!;

        var sessions = await repository.ListTenantSessionsAsync(cancellationToken);
        return TypedResults.Ok(new { sessions });
    }

    private static async Task<IResult> CreateInvitationAsync(
        [FromBody] CreateAdminAccessInvitationRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.CreateInvitationAsync(
            new CreateAdminAccessInvitationCommand(request.Email, request.IntendedRoleReference, request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new
            {
                created = true,
                invitationReference = result.InvitationReference,
                deliveryStatus = result.DeliveryStatus,
                idempotentReplay = result.IdempotentReplay,
            })
            : AdminAccessEndpointHelpers.MapInvitationFailure(result);
    }

    private static async Task<IResult> SuspendMemberAsync(
        string memberReference,
        [FromBody] SuspendAdminAccessMemberRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.SuspendMemberAsync(
            new SuspendAdminAccessMemberCommand(memberReference, request.ExpectedAuthorizationVersion, request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> ReactivateMemberAsync(
        string memberReference,
        [FromBody] ReactivateAdminAccessMemberRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.ReactivateMemberAsync(
            new ReactivateAdminAccessMemberCommand(memberReference, request.ExpectedAuthorizationVersion, request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { updated = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> RevokeSessionAsync(
        string sessionReference,
        [FromBody] RevokeAdminAccessSessionRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.RevokeSessionAsync(
            new RevokeAdminAccessSessionCommand(sessionReference, request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { revoked = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }

    private static async Task<IResult> RevokeAllMemberSessionsAsync(
        string memberReference,
        [FromBody] RevokeAllAdminAccessMemberSessionsRequest request,
        HttpContext httpContext,
        IAdminAccessRepository repository,
        IPermissionEvaluator permissionEvaluator,
        ICurrentIdentityAccessor identityAccessor,
        ITenantContextAccessor tenantContextAccessor,
        IAuthorizationContext authorizationContext,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (actor, failure) = await AdminAccessEndpointHelpers.ResolveMutationAccessAsync(
            httpContext, permissionEvaluator, identityAccessor, tenantContextAccessor,
            authorizationContext, correlationContext, cancellationToken);
        if (actor is null) return failure!;

        var result = await repository.RevokeAllMemberSessionsAsync(
            new RevokeAllAdminAccessMemberSessionsCommand(memberReference, request.IdempotencyKey),
            actor,
            cancellationToken);
        return result.Succeeded
            ? TypedResults.Ok(new { revoked = true, idempotentReplay = result.IdempotentReplay })
            : AdminAccessEndpointHelpers.MapWriteFailure(result);
    }
}

public static class AdminAccessServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraAdminAccess(this IServiceCollection services)
    {
        services.AddScoped<IAdminAccessRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableAdminAccessRepository()
                : new AdminAccessRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetService<IIdentityEmailDeliveryProvider>()
                        ?? new UnavailableIdentityEmailDeliveryProvider(),
                    serviceProvider.GetRequiredService<ITenantEntitlementEvaluator>(),
                    serviceProvider.GetService<IDevelopmentTokenDeliverySink>());
        });

        return services;
    }
}

public sealed record CreateAdminAccessRoleRequest(
    string RoleKey,
    string DisplayName,
    string? Description,
    string? IdempotencyKey);

public sealed record PatchAdminAccessRoleRequest(
    string? DisplayName,
    string? Description,
    string? Status,
    uint ExpectedVersion,
    string? IdempotencyKey);

public sealed record SetAdminAccessRolePermissionsRequest(
    IReadOnlyList<AdminAccessPermissionGrantRequest>? Grants,
    uint ExpectedVersion,
    string? IdempotencyKey);

public sealed record AdminAccessPermissionGrantRequest(
    string PermissionKey,
    string? RecordScope,
    string Effect);

public sealed record PutAdminAccessMemberRolesRequest(
    IReadOnlyList<string>? RoleReferences,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record PutAdminAccessMemberFacilitiesRequest(
    IReadOnlyList<string>? FacilityCodes,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record PutAdminAccessMemberOverridesRequest(
    IReadOnlyList<AdminAccessPermissionOverrideRequest>? Overrides,
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record AdminAccessPermissionOverrideRequest(
    string PermissionKey,
    string Effect,
    string? RecordScope);

public sealed record CreateAdminAccessInvitationRequest(
    string Email,
    string? IntendedRoleReference,
    string? IdempotencyKey);

public sealed record SuspendAdminAccessMemberRequest(
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record ReactivateAdminAccessMemberRequest(
    uint ExpectedAuthorizationVersion,
    string? IdempotencyKey);

public sealed record RevokeAdminAccessSessionRequest(string? IdempotencyKey);

public sealed record RevokeAllAdminAccessMemberSessionsRequest(string? IdempotencyKey);
