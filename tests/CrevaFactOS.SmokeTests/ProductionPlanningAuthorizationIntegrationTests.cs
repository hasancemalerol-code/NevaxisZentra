using CrevaFactOS.Api.Authorization;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CrevaFactOS.SmokeTests;

public sealed class ProductionPlanningAuthorizationIntegrationTests
{
    [Fact]
    public async Task HandlerUsesProductionPlanningFacilityResource()
    {
        var resolver = new TrackingFacilityAccessResolver();
        var handler = new ProductionPlanningReadAuthorizationHandler(resolver);
        var facilityId = Guid.Parse("99000000-0000-4000-8000-000000000040");
        var requirement = new ProductionPlanningReadRequirement();
        var resource = new ProductionPlanningFacilityResource(facilityId);
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("test", "true")],
                "test"));
        var context = new AuthorizationHandlerContext([requirement], user, resource);

        await handler.HandleAsync(context);

        Assert.Equal(facilityId, resolver.LastFacilityId);
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void ProductionPlanningReadPolicyRequiresDedicatedRequirement()
    {
        Assert.Equal(
            "Permission:production.planning.read",
            PermissionPolicies.ProductionPlanningRead);
    }

    [Fact]
    public async Task TenantMembershipAloneDoesNotSatisfyProductionPlanningReadHandler()
    {
        var handler = new ProductionPlanningReadAuthorizationHandler(new DenyingFacilityAccessResolver());
        var requirement = new ProductionPlanningReadRequirement();
        var resource = new ProductionPlanningFacilityResource(
            Guid.Parse("99000000-0000-4000-8000-000000000041"));
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("test", "true")],
                "test"));
        var context = new AuthorizationHandlerContext([requirement], user, resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task InactiveFacilityMembershipDoesNotSatisfyProductionPlanningReadHandler()
    {
        var handler = new ProductionPlanningReadAuthorizationHandler(new DenyingFacilityAccessResolver());
        var requirement = new ProductionPlanningReadRequirement();
        var resource = new ProductionPlanningFacilityResource(
            Guid.Parse("99000000-0000-4000-8000-000000000042"));
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("test", "true")],
                "test"));
        var context = new AuthorizationHandlerContext([requirement], user, resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandlerRequiresProductionPlanningReadPermissionThroughResolver()
    {
        var resolver = new TrackingFacilityAccessResolver();
        var handler = new ProductionPlanningReadAuthorizationHandler(resolver);
        var requirement = new ProductionPlanningReadRequirement();
        var resource = new ProductionPlanningFacilityResource(
            Guid.Parse("99000000-0000-4000-8000-000000000043"));
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("test", "true")],
                "test"));
        var context = new AuthorizationHandlerContext([requirement], user, resource);

        await handler.HandleAsync(context);

        Assert.Equal(ProductionPlanningPermissionKeys.Read, resolver.LastPermissionKey);
    }

    private sealed class TrackingFacilityAccessResolver : IFacilityAccessResolver
    {
        public Guid LastFacilityId { get; private set; }

        public string? LastPermissionKey { get; private set; }

        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default)
        {
            LastFacilityId = request.FacilityId;
            LastPermissionKey = request.PermissionKey;

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
