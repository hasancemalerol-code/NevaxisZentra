using System.Net;
using System.Net.Http.Json;
using NevaxisFactOS.Api.Authentication;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NevaxisFactOS.SmokeTests;

public sealed class PermissionAuthorizationTests
{
    private static readonly Guid IdentityId = Guid.Parse("66000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("66000000-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("66000000-0000-4000-8000-000000000003");

    [Fact]
    public async Task ExactAssignedPermissionGrantsProtectedEndpoint()
    {
        await using var factory = CreateFactory(InfrastructurePermissionKeys.TenantContextRead);
        using var client = factory.CreateClient();
        using var request = CreateRequest("/internal/tenant-context", includeTenant: true);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MembershipAloneGrantsNoPermissionAndUsesUniformDenial()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = CreateRequest("/internal/tenant-context", includeTenant: true);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "{\"type\":\"about:blank\",\"status\":403,\"title\":\"Authorization denied.\"}",
            body);
    }

    [Fact]
    public async Task MissingTenantContextFailsClosed()
    {
        await using var factory = CreateFactory(InfrastructurePermissionKeys.TenantContextRead);
        using var client = factory.CreateClient();
        using var request = CreateRequest("/internal/tenant-context", includeTenant: false);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("tenant.context.read", true)]
    [InlineData("Tenant.Context.Read", false)]
    [InlineData("tenant.context", false)]
    [InlineData("tenant.context.*", false)]
    [InlineData("*", false)]
    [InlineData("system.diagnostics.read", false)]
    public async Task PermissionChecksUseExactCaseSensitiveKeys(string requestedKey, bool expected)
    {
        await using var factory = CreateFactory(
            InfrastructurePermissionKeys.AuthorizationDiagnosticsRead,
            InfrastructurePermissionKeys.TenantContextRead);
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/internal/permission-check/{Uri.EscapeDataString(requestedKey)}",
            includeTenant: true);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<PermissionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(expected, body.Granted);
        Assert.Equal(requestedKey, body.RequestedPermissionKey);
    }

    [Fact]
    public async Task DevelopmentAuthorizationEndpointsAreUnavailableInProduction()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/internal/authorization-context");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(params string[] grantedPermissions) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                SmokeTestServiceStubs.ConfigureIsolatedDevelopmentHost(builder);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.RemoveAll<ITenantMembershipResolver>();
                    services.RemoveAll<IPermissionEvaluator>();
                    services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                    services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                    services.AddScoped<IPermissionEvaluator>(_ =>
                        new StubPermissionEvaluator(grantedPermissions.ToHashSet(StringComparer.Ordinal)));
                });
            });

    private static HttpRequestMessage CreateRequest(string path, bool includeTenant)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        if (includeTenant)
        {
            request.Headers.Add("X-NevaxisFactOS-Tenant", "tenant-a");
        }

        return request;
    }

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
            string externalIssuer,
            string externalSubject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(
                externalIssuer == "local-tests" && externalSubject == "active-subject"
                    ? new AuthenticatedIdentity(IdentityId, externalIssuer, externalSubject)
                    : null);
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(
            Guid identitySubjectId,
            string tenantIdentifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantContext?>(
                identitySubjectId == IdentityId && tenantIdentifier == "tenant-a"
                    ? new TenantContext(TenantId, "tenant-a", MembershipId)
                    : null);
    }

    private sealed class StubPermissionEvaluator(IReadOnlySet<string> grantedPermissions) : IPermissionEvaluator
    {
        public Task<PermissionCheckResult> EvaluateAsync(
            Guid identitySubjectId,
            Guid tenantId,
            Guid membershipId,
            string permissionKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                identitySubjectId == IdentityId
                && tenantId == TenantId
                && membershipId == MembershipId
                && grantedPermissions.Contains(permissionKey)
                    ? PermissionCheckResult.Grant()
                    : PermissionCheckResult.Deny("Not assigned."));
    }

    private sealed record PermissionResponse(string RequestedPermissionKey, bool Granted);
}
