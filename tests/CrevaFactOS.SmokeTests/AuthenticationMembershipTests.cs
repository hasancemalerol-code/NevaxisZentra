using System.Net;
using System.Net.Http.Json;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Persistence.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CrevaFactOS.SmokeTests;

public sealed class AuthenticationMembershipTests
{
    private static readonly Guid IdentityId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    private static readonly Guid TenantAId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid MembershipId = Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee");

    [Theory]
    [InlineData("tenant-a")]
    [InlineData("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1")]
    public async Task ActiveIdentityAndMembershipEstablishTenantContext(string selector)
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(selector, "active-subject");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<TenantContextResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Authenticated);
        Assert.True(body.IdentityResolved);
        Assert.True(body.TenantContextAvailable);
        Assert.Equal(IdentityId, body.IdentitySubjectId);
        Assert.Equal(TenantAId, body.TenantId);
    }

    [Fact]
    public async Task HeaderAloneCannotEstablishTenantContext()
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/tenant-context");
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedDevelopmentIdentityIsRejected()
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/tenant-context");
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InactiveIdentityIsRejected()
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest("tenant-a", "inactive-subject");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("missing-membership")]
    [InlineData("inactive-membership")]
    [InlineData("inactive-tenant")]
    [InlineData("unknown-tenant")]
    [InlineData("tenant-b")]
    public async Task MembershipAndTenantFailuresUseUniformResponse(string selector)
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(selector, "active-subject");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "{\"type\":\"about:blank\",\"status\":403,\"title\":\"Tenant access denied.\"}",
            body);
    }

    [Fact]
    public async Task HealthEndpointsRemainAnonymousAndTenantNeutral()
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();

        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task IdentityAndTenantContextDoNotLeakAcrossRequests()
    {
        await using var factory = CreateDevelopmentFactory();
        using var client = factory.CreateClient();
        using (var authenticatedRequest = CreateAuthenticatedRequest("tenant-a", "active-subject"))
        using (var authenticatedResponse = await client.SendAsync(authenticatedRequest))
        {
            Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
        }

        using var anonymousResponse = await client.GetAsync("/internal/tenant-context");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
    }

    [Fact]
    public void DevelopmentAuthenticationCannotStartInProduction()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "Development",
            })
            .Build();
        var services = new ServiceCollection();
        var environment = new StubHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() =>
            services.AddCrevaFactOSAuthentication(configuration, environment));
    }

    private static WebApplicationFactory<Program> CreateDevelopmentFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(authenticationOptions =>
                        {
                            authenticationOptions.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
                            authenticationOptions.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                            DevelopmentAuthenticationHandler.SchemeName,
                            _ => { });
                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.RemoveAll<ITenantMembershipResolver>();
                    services.RemoveAll<IPermissionEvaluator>();
                    services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                    services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                    services.AddScoped<IPermissionEvaluator, StubPermissionEvaluator>();
                });
            });

    private static HttpRequestMessage CreateAuthenticatedRequest(string selector, string subject)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/internal/tenant-context");
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, subject);
        request.Headers.Add("X-CrevaFactOS-Tenant", selector);
        return request;
    }

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
            string externalIssuer,
            string externalSubject,
            CancellationToken cancellationToken = default)
        {
            AuthenticatedIdentity? identity = externalIssuer == "local-tests"
                && externalSubject == "active-subject"
                ? new AuthenticatedIdentity(IdentityId, externalIssuer, externalSubject)
                : null;
            return Task.FromResult(identity);
        }
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(
            Guid identitySubjectId,
            string tenantIdentifier,
            CancellationToken cancellationToken = default)
        {
            TenantContext? context = identitySubjectId == IdentityId
                && (tenantIdentifier == "tenant-a"
                    || tenantIdentifier == TenantAId.ToString())
                ? new TenantContext(TenantAId, "tenant-a", MembershipId)
                : null;
            return Task.FromResult(context);
        }
    }

    private sealed class StubPermissionEvaluator : IPermissionEvaluator
    {
        public Task<PermissionCheckResult> EvaluateAsync(
            Guid identitySubjectId,
            Guid tenantId,
            Guid membershipId,
            string permissionKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                identitySubjectId == IdentityId
                && tenantId == TenantAId
                && membershipId == MembershipId
                && permissionKey == InfrastructurePermissionKeys.TenantContextRead
                    ? PermissionCheckResult.Grant()
                    : PermissionCheckResult.Deny("Not assigned."));
    }

    private sealed record TenantContextResponse(
        bool Authenticated,
        bool IdentityResolved,
        bool TenantContextAvailable,
        Guid? IdentitySubjectId,
        Guid? TenantId,
        Guid? MembershipId);

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "CrevaFactOS.SmokeTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
