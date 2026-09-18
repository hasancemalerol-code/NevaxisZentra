using System.Net;
using System.Net.Http.Json;
using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Api.Development;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.ProductionPlanning;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace NevaxisZentra.SmokeTests;

public sealed class ProductionPlanningHandoffApiEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000008-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000008-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000008-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000008-0000-4000-8000-000000000004");
    private static readonly Guid PlanId = Guid.Parse("99000008-0000-4000-8000-000000000005");
    private static readonly Guid HandoffId = Guid.Parse("99000008-0000-4000-8000-000000000006");

    // ── Authentication / authorization ────────────────────────────────────────

    [Fact]
    public async Task HandoffWithoutAuthenticationReturns401()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HandoffWithoutFacilityIdReturns400()
    {
        await using var factory = CreateFactory(new GrantingHandoffAccessResolver());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidRequest, problem!.FailureCode);
    }

    [Fact]
    public async Task HandoffWithoutMembershipReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.FacilityAccessDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.FacilityAccessDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task HandoffWithoutHandoffPermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    // ── Success ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandoffSuccessReturns201WithHandoffId()
    {
        await using var factory = CreateFactory(
            new GrantingHandoffAccessResolver(),
            new SucceedingHandoffService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var result = await response.Content.ReadFromJsonAsync<HandoffSuccessResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(HandoffId, result.HandoffId);
        Assert.Equal("prepared", result.Status);
    }

    // ── Failure codes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandoffNotScheduledReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingHandoffAccessResolver(),
            new FailingHandoffService(ProductionPlanHandoffFailureCodes.NotScheduled));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidTransition, problem!.FailureCode);
    }

    [Fact]
    public async Task HandoffAlreadyPreparedReturns409WithHandoffAlreadyPreparedCode()
    {
        await using var factory = CreateFactory(
            new GrantingHandoffAccessResolver(),
            new FailingHandoffService(ProductionPlanHandoffFailureCodes.AlreadyPrepared));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("handoff-already-prepared", problem!.FailureCode);
    }

    [Fact]
    public async Task HandoffBlockingRiskReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingHandoffAccessResolver(),
            new FailingHandoffService(ProductionPlanHandoffFailureCodes.BlockingRisk));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task HandoffPlanNotFoundReturns404()
    {
        await using var factory = CreateFactory(
            new GrantingHandoffAccessResolver(),
            new FailingHandoffService(ProductionPlanHandoffFailureCodes.NotFound));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HandoffConcurrencyConflictReturns409WithConcurrencyCode()
    {
        await using var factory = CreateFactory(
            new GrantingHandoffAccessResolver(),
            new FailingHandoffService(ProductionPlanHandoffFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    // ── Permission separation: approve does not grant handoff ─────────────────

    [Fact]
    public async Task HandoffWithApprovePermissionButNotHandoffPermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/handoff?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Factory / infrastructure ──────────────────────────────────────────────

    private static WebApplicationFactory<Program> CreateFactory(
        IFacilityAccessResolver? facilityResolver = null,
        IProductionPlanHandoffService? handoffService = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Development");
                host.ConfigureAppConfiguration((_, cfg) =>
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Persistence:Enabled"] = "false",
                        ["DevelopmentPilot:Enabled"] = "false",
                    }));
                host.ConfigureServices(services =>
                {
                    services.ReplaceEntitlementEvaluatorWithAllowStub();
                    if (facilityResolver is not null)
                    {
                        services.RemoveAll<IFacilityAccessResolver>();
                        services.AddSingleton(facilityResolver);
                    }

                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.AddSingleton<IIdentitySubjectResolver>(new StubIdentityResolver());

                    services.RemoveAll<ITenantMembershipResolver>();
                    services.AddSingleton<ITenantMembershipResolver>(new StubMembershipResolver());

                    if (handoffService is not null)
                    {
                        services.RemoveAll<IProductionPlanHandoffService>();
                        services.AddSingleton(handoffService);
                    }
                });
            });
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, object body)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-NevaxisZentra-Tenant", "tenant-a");
        return request;
    }

    private sealed record ProblemDetailsResponse(
        int Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("failureCode")] string? FailureCode);

    private sealed record HandoffSuccessResponse(
        Guid HandoffId,
        Guid ProductionPlanId,
        string ReferenceCode,
        string Status,
        uint HandoffVersion,
        DateTimeOffset PreparedAtUtc);

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

    private sealed class GrantingHandoffAccessResolver : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default)
        {
            var access = new FacilityAccessContext(
                TenantId,
                request.FacilityId,
                MembershipId,
                IdentityId,
                ProductionPlanningPermissionKeys.Handoff,
                "test",
                true);
            return Task.FromResult(FacilityAccessResolutionResult.Grant(access, DateTimeOffset.UtcNow));
        }
    }

    private sealed class DenyingFacilityAccessResolver(string failureCode) : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                FacilityAccessResolutionResult.Deny(
                    failureCode,
                    "Denied in test.",
                    DateTimeOffset.UtcNow,
                    facilityId: request.FacilityId,
                    permissionKey: request.PermissionKey));
    }

    private sealed class SucceedingHandoffService : IProductionPlanHandoffService
    {
        public Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
            ProductionPlanWriteOperationContext operationContext,
            Guid productionPlanId,
            PrepareHandoffApiRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductionPlanHandoffResult.Success(
                HandoffId,
                productionPlanId,
                "PP-TEST-001",
                "prepared",
                1u,
                DateTimeOffset.UtcNow));
    }

    private sealed class FailingHandoffService(string failureCode) : IProductionPlanHandoffService
    {
        public Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
            ProductionPlanWriteOperationContext operationContext,
            Guid productionPlanId,
            PrepareHandoffApiRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductionPlanHandoffResult.Fail(failureCode, "Stub failure."));
    }
}
