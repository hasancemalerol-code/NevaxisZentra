using NevaxisZentra.Api.Authorization;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace NevaxisZentra.SmokeTests;

public sealed class FacilityAccessResolverTests
{
    private static readonly Guid IdentityId = Guid.Parse("77000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("77000000-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("77000000-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("77000000-0000-4000-8000-000000000004");

    [Fact]
    public async Task MissingIdentityReturnsUnauthenticated()
    {
        var resolver = CreateResolver(
            identity: null,
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext());

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.Unauthenticated, result.FailureCode);
    }

    [Fact]
    public async Task MissingTenantContextReturnsTenantContextMissing()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: null,
            authorization: CreateAuthorizationContext());

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.TenantContextMissing, result.FailureCode);
    }

    [Fact]
    public async Task MissingAuthorizationContextReturnsMembershipMissing()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: null);

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.MembershipMissing, result.FailureCode);
    }

    [Fact]
    public async Task EmptyFacilityIdReturnsFacilityMissing()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext());

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(Guid.Empty, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.FacilityMissing, result.FailureCode);
    }

    [Fact]
    public async Task MissingFacilityOrMembershipReturnsFacilityAccessDenied()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            probe: new FacilityAccessProbeResult(false, false));

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.FacilityAccessDenied, result.FailureCode);
        Assert.Equal(TenantId, result.TenantId);
        Assert.Equal(FacilityId, result.FacilityId);
    }

    [Fact]
    public async Task InactiveFacilityMembershipReturnsFacilityAccessDenied()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            probe: new FacilityAccessProbeResult(true, false));

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.FacilityAccessDenied, result.FailureCode);
    }

    [Fact]
    public async Task MissingPermissionReturnsPermissionDenied()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            probe: new FacilityAccessProbeResult(true, true),
            grantedPermissions: []);

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.PermissionDenied, result.FailureCode);
    }

    [Fact]
    public async Task ActiveFacilityMembershipAndPermissionReturnsSuccess()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            probe: new FacilityAccessProbeResult(true, true),
            grantedPermissions: [ProductionPlanningPermissionKeys.Read]);

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.True(result.Succeeded);
        Assert.Equal("granted", result.Decision);
        Assert.NotNull(result.Access);
        Assert.Equal(TenantId, result.Access!.TenantId);
        Assert.Equal(FacilityId, result.Access.FacilityId);
        Assert.Equal(MembershipId, result.Access.MembershipId);
    }

    [Fact]
    public async Task TenantMembershipAloneDoesNotGrantFacilityAccessWithoutMembershipProbe()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            probe: new FacilityAccessProbeResult(true, false),
            grantedPermissions: [ProductionPlanningPermissionKeys.Read]);

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.FacilityAccessDenied, result.FailureCode);
    }

    [Fact]
    public async Task ReadStoreFailureReturnsSourceUnavailable()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            readStore: new ThrowingFacilityAccessReadStore());

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.False(result.Succeeded);
        Assert.Equal(FacilityAccessFailureCodes.SourceUnavailable, result.FailureCode);
        Assert.True(result.Retryable);
    }

    [Fact]
    public async Task FacilityAccessDeniedDoesNotExposeCrossTenantExistenceDetails()
    {
        var resolver = CreateResolver(
            identity: CreateIdentity(),
            tenant: CreateTenantContext(),
            authorization: CreateAuthorizationContext(),
            probe: new FacilityAccessProbeResult(false, false));

        var result = await resolver.ResolveAsync(
            new FacilityAccessRequest(FacilityId, ProductionPlanningPermissionKeys.Read));

        Assert.Equal(FacilityAccessFailureCodes.FacilityAccessDenied, result.FailureCode);
        Assert.DoesNotContain("another tenant", result.InternalReason, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("foreign key", result.InternalReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductionPlanningReadPolicyUsesExactPermissionKey()
    {
        Assert.Equal(
            "Permission:production.planning.read",
            PermissionPolicies.ProductionPlanningRead);
    }

    private static FacilityAccessResolver CreateResolver(
        AuthenticatedIdentity? identity,
        TenantContext? tenant,
        AuthorizationContext? authorization,
        FacilityAccessProbeResult? probe = null,
        IReadOnlyCollection<string>? grantedPermissions = null,
        IFacilityAccessReadStore? readStore = null)
    {
        var permissionEvaluator = new StubPermissionEvaluator(
            (grantedPermissions ?? [ProductionPlanningPermissionKeys.Read]).ToHashSet(StringComparer.Ordinal));

        return new FacilityAccessResolver(
            new StubIdentityAccessor(identity),
            new StubTenantContextAccessor(tenant),
            new StubAuthorizationContext(authorization),
            permissionEvaluator,
            readStore ?? new StubFacilityAccessReadStore(probe ?? new FacilityAccessProbeResult(false, false)),
            new HttpContextAccessor());
    }

    private static AuthenticatedIdentity CreateIdentity() =>
        new(IdentityId, "local-tests", "active-subject");

    private static TenantContext CreateTenantContext() =>
        new(TenantId, "tenant-a", MembershipId);

    private static AuthorizationContext CreateAuthorizationContext() =>
        new(IdentityId, TenantId, MembershipId);

    private sealed class StubIdentityAccessor(AuthenticatedIdentity? identity) : ICurrentIdentityAccessor
    {
        public bool IsResolved => identity is not null;

        public AuthenticatedIdentity? Current => identity;

        public Guid RequireIdentitySubjectId() =>
            identity?.IdentitySubjectId
            ?? throw new InvalidOperationException("Identity is unavailable.");
    }

    private sealed class StubTenantContextAccessor(TenantContext? tenantContext) : ITenantContextAccessor
    {
        public bool IsAvailable => tenantContext is not null;

        public TenantContext? Current => tenantContext;

        public TenantContext RequireTenantContext() =>
            tenantContext ?? throw new InvalidOperationException("Tenant context is unavailable.");

        public Guid RequireTenantId() =>
            tenantContext?.TenantId
            ?? throw new InvalidOperationException("Tenant context is unavailable.");
    }

    private sealed class StubAuthorizationContext(AuthorizationContext? authorizationContext)
        : IAuthorizationContext
    {
        public bool IsAvailable => authorizationContext is not null;

        public AuthorizationContext? Current => authorizationContext;
    }

    private sealed class StubFacilityAccessReadStore(FacilityAccessProbeResult probe)
        : IFacilityAccessReadStore
    {
        public Task<FacilityAccessProbeResult> ProbeAsync(
            Guid tenantId,
            Guid facilityId,
            Guid tenantMembershipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(probe);
    }

    private sealed class ThrowingFacilityAccessReadStore : IFacilityAccessReadStore
    {
        public Task<FacilityAccessProbeResult> ProbeAsync(
            Guid tenantId,
            Guid facilityId,
            Guid tenantMembershipId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated persistence outage.");
    }

    private sealed class StubPermissionEvaluator(IReadOnlySet<string> grantedPermissions)
        : IPermissionEvaluator
    {
        public Task<PermissionCheckResult> EvaluateAsync(
            Guid identitySubjectId,
            Guid tenantId,
            Guid membershipId,
            string permissionKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                grantedPermissions.Contains(permissionKey)
                    ? PermissionCheckResult.Grant()
                    : PermissionCheckResult.Deny("Permission was not granted."));
    }
}

public sealed class ProductionPlanningReadAuthorizationHandlerTests
{
    [Fact]
    public async Task HandlerSucceedsWhenFacilityAccessResolverGrantsAccess()
    {
        var handler = new ProductionPlanningReadAuthorizationHandler(new GrantingFacilityAccessResolver());
        var requirement = new ProductionPlanningReadRequirement();
        var resource = new ProductionPlanningFacilityResource(
            Guid.Parse("77000000-0000-4000-8000-000000000010"));
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("test", "true")],
                "test"));
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandlerDoesNotSucceedWhenFacilityAccessResolverDeniesAccess()
    {
        var handler = new ProductionPlanningReadAuthorizationHandler(new DenyingFacilityAccessResolver());
        var requirement = new ProductionPlanningReadRequirement();
        var resource = new ProductionPlanningFacilityResource(
            Guid.Parse("77000000-0000-4000-8000-000000000010"));
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("test", "true")],
                "test"));
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class GrantingFacilityAccessResolver : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default)
        {
            var access = new FacilityAccessContext(
                Guid.NewGuid(),
                request.FacilityId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                request.PermissionKey,
                "test",
                false);

            return Task.FromResult(FacilityAccessResolutionResult.Grant(access, DateTimeOffset.UtcNow));
        }
    }

    private sealed class DenyingFacilityAccessResolver : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityAccessDenied,
                    "Denied in test.",
                    DateTimeOffset.UtcNow,
                    facilityId: request.FacilityId,
                    permissionKey: request.PermissionKey));
    }
}
