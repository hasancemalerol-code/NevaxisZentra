using System.Net;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrevaFactOS.SmokeTests;

public sealed class AuditApiTests
{
    private static readonly Guid IdentityId = Guid.Parse("77000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("77000000-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("77000000-0000-4000-8000-000000000003");

    [Fact]
    public async Task ExactAuditReadPermissionGrantsDiagnosticEndpoint()
    {
        await using var factory = CreateFactory(grantAuditRead: true);
        using var client = factory.CreateClient();
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MembershipAloneCannotReadAuditRecords()
    {
        await using var factory = CreateFactory(grantAuditRead: false);
        using var client = factory.CreateClient();
        using var request = CreateRequest();

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "{\"type\":\"about:blank\",\"status\":403,\"title\":\"Authorization denied.\"}",
            body);
    }

    [Fact]
    public async Task ValidIncomingCorrelationIdIsReturnedAndDoesNotLeak()
    {
        await using var factory = CreateFactory(grantAuditRead: true);
        using var client = factory.CreateClient();
        var supplied = Guid.NewGuid();
        using var firstRequest = CreateRequest();
        firstRequest.Headers.Add("X-CrevaFactOS-Correlation-Id", supplied.ToString("D"));
        using var firstResponse = await client.SendAsync(firstRequest);

        using var secondRequest = CreateRequest();
        secondRequest.Headers.Add("X-CrevaFactOS-Correlation-Id", "invalid-and-untrusted");
        using var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(supplied.ToString("D"), firstResponse.Headers.GetValues("X-CrevaFactOS-Correlation-Id").Single());
        var generated = Guid.Parse(secondResponse.Headers.GetValues("X-CrevaFactOS-Correlation-Id").Single());
        Assert.NotEqual(supplied, generated);
        Assert.NotEqual(Guid.Empty, generated);
    }

    [Fact]
    public async Task AuditDiagnosticEndpointIsUnavailableInProduction()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/internal/audit/recent");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool grantAuditRead) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                            DevelopmentAuthenticationHandler.SchemeName,
                            _ => { });
                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.RemoveAll<ITenantMembershipResolver>();
                    services.RemoveAll<IPermissionEvaluator>();
                    services.RemoveAll<IAuditReader>();
                    services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                    services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                    services.AddScoped<IPermissionEvaluator>(_ => new StubPermissionEvaluator(grantAuditRead));
                    services.AddScoped<IAuditReader, StubAuditReader>();
                });
            });

    private static HttpRequestMessage CreateRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/internal/audit/recent");
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "audit-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");
        return request;
    }

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
            string externalIssuer,
            string externalSubject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(new(IdentityId, externalIssuer, externalSubject));
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(
            Guid identitySubjectId,
            string tenantIdentifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantContext?>(new(TenantId, "tenant-a", MembershipId));
    }

    private sealed class StubPermissionEvaluator(bool grantAuditRead) : IPermissionEvaluator
    {
        public Task<PermissionCheckResult> EvaluateAsync(
            Guid identitySubjectId,
            Guid tenantId,
            Guid membershipId,
            string permissionKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                grantAuditRead && permissionKey == InfrastructurePermissionKeys.SystemAuditRead
                    ? PermissionCheckResult.Grant()
                    : PermissionCheckResult.Deny("Not assigned."));
    }

    private sealed class StubAuditReader : IAuditReader
    {
        public Task<IReadOnlyList<AuditRecordSummary>> ReadRecentAsync(
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditRecordSummary>>([]);
    }
}
