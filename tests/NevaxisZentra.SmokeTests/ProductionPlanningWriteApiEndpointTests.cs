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

public sealed class ProductionPlanningWriteApiEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000001-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000001-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000001-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000001-0000-4000-8000-000000000004");
    private static readonly Guid PlanId = Guid.Parse("99000001-0000-4000-8000-000000000005");

    [Fact]
    public async Task CreateWithoutAuthenticationReturns401()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/production-planning/plans",
            ValidCreateBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateWithMissingFacilityIdReturns400()
    {
        await using var factory = CreateFactory(new GrantingFacilityAccessResolver());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            new { facilityId = Guid.Empty, referenceCode = "PP-001", title = "Plan" });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidRequest, problem!.FailureCode);
    }

    [Fact]
    public async Task CreateWithoutFacilityMembershipReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.FacilityAccessDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.FacilityAccessDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task CreateWithReadPermissionOnlyReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task CreateWithWritePermissionReturns201()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new SucceedingDraftWriteService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateSuccessBodyContainsStatusDraft()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new SucceedingDraftWriteService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var result = await response.Content.ReadFromJsonAsync<WriteSuccessResponse>();

        Assert.Equal("draft", result!.Status);
    }

    [Fact]
    public async Task CreateSuccessBodyContainsVersionGreaterThanZero()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new SucceedingDraftWriteService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var result = await response.Content.ReadFromJsonAsync<WriteSuccessResponse>();

        Assert.True(result!.Version >= 1);
    }

    [Fact]
    public async Task CreateDuplicateReferenceReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new FailingDraftWriteService(ProductionPlanWriteFailureCodes.ReferenceCodeConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ReferenceCodeConflict, problem!.FailureCode);
    }

    [Fact]
    public async Task CreateValidationFailedReturns400()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new FailingDraftWriteService(ProductionPlanWriteFailureCodes.ValidationFailed));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ValidationFailed, problem!.FailureCode);
    }

    [Fact]
    public async Task UpdateAuthorizedDraftReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new SucceedingDraftWriteService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Put,
            $"/api/production-planning/plans/{PlanId:D}",
            ValidUpdateBody());

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateApprovedPlanReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new FailingDraftWriteService(ProductionPlanWriteFailureCodes.PlanNotEditable));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Put,
            $"/api/production-planning/plans/{PlanId:D}",
            ValidUpdateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PlanNotEditable, problem!.FailureCode);
    }

    [Fact]
    public async Task UpdateUnknownPlanReturns404()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new FailingDraftWriteService(ProductionPlanWriteFailureCodes.NotFound));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Put,
            $"/api/production-planning/plans/{PlanId:D}",
            ValidUpdateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.NotFound, problem!.FailureCode);
    }

    [Fact]
    public async Task UpdateStaleVersionReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new FailingDraftWriteService(ProductionPlanWriteFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Put,
            $"/api/production-planning/plans/{PlanId:D}",
            ValidUpdateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    [Fact]
    public async Task SourceUnavailableReturns503()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new FailingDraftWriteService(ProductionPlanWriteFailureCodes.SourceUnavailable));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            "/api/production-planning/plans",
            ValidCreateBody());

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.SourceUnavailable, problem!.FailureCode);
    }

    [Fact]
    public async Task PatchRouteDoesNotExist()
    {
        await using var factory = CreateFactory(new GrantingFacilityAccessResolver());
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Patch,
            $"/api/production-planning/plans/{PlanId:D}",
            new { });

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRouteDoesNotExist()
    {
        await using var factory = CreateFactory(new GrantingFacilityAccessResolver());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/production-planning/plans/{PlanId:D}");
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-NevaxisZentra-Tenant", "tenant-a");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IFacilityAccessResolver? facilityAccessResolver = null,
        IProductionPlanDraftWriteService? writeService = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Authentication:Mode"] = "Development",
                            ["Persistence:Enabled"] = "false",
                            ["DevelopmentPilot:Enabled"] = "false",
                            ["DevelopmentPilot:ProductionPlanSeedEnabled"] = "false",
                        });
                });
                builder.ConfigureServices(services =>
                {
                    RemoveHostedService<DevelopmentPilotHostedBootstrapService>(services);
                    services.ReplaceEntitlementEvaluatorWithAllowStub();
                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.RemoveAll<ITenantMembershipResolver>();
                    services.RemoveAll<IPermissionEvaluator>();
                    services.RemoveAll<IFacilityAccessResolver>();
                    services.RemoveAll<IProductionPlanDraftWriteService>();
                    services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                    services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                    services.AddScoped<IPermissionEvaluator>(_ =>
                        new StubPermissionEvaluator(
                            new HashSet<string>([ProductionPlanningPermissionKeys.Write], StringComparer.Ordinal)));
                    services.AddScoped<IFacilityAccessResolver>(_ =>
                        facilityAccessResolver ?? new DenyingFacilityAccessResolver(
                            FacilityAccessFailureCodes.FacilityAccessDenied));
                    services.AddScoped<IProductionPlanDraftWriteService>(_ =>
                        writeService ?? new FailingDraftWriteService(ProductionPlanWriteFailureCodes.SourceUnavailable));
                });
            });

    private static void RemoveHostedService<TImplementation>(IServiceCollection services)
        where TImplementation : class, IHostedService
    {
        var descriptors = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(TImplementation))
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
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

    private static object ValidCreateBody() =>
        new
        {
            facilityId = FacilityId,
            referenceCode = "PP-TEST-001",
            title = "Test Plan",
            origin = "manual",
            aiGenerated = false,
            plannedStartAtUtc = "2026-09-01T06:00:00Z",
            plannedEndAtUtc = "2026-09-01T14:00:00Z",
            demands = new[]
            {
                new
                {
                    id = "d1",
                    productId = "PROD-100",
                    productName = "Product A",
                    requiredQuantity = 100,
                    unit = "pcs",
                    priority = "normal",
                },
            },
            risks = Array.Empty<object>(),
            resources = new { },
            sourceReferences = Array.Empty<object>(),
        };

    private static object ValidUpdateBody() =>
        new
        {
            facilityId = FacilityId,
            expectedVersion = 1,
            referenceCode = "PP-TEST-001",
            title = "Updated Plan",
            origin = "manual",
            aiGenerated = false,
            plannedStartAtUtc = "2026-09-01T06:00:00Z",
            plannedEndAtUtc = "2026-09-01T14:00:00Z",
            demands = new[]
            {
                new
                {
                    id = "d1",
                    productId = "PROD-100",
                    productName = "Product A",
                    requiredQuantity = 100,
                    unit = "pcs",
                    priority = "normal",
                },
            },
            risks = Array.Empty<object>(),
            resources = new { },
            sourceReferences = Array.Empty<object>(),
        };

    private sealed record ProblemDetailsResponse(
        int Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("failureCode")] string? FailureCode);

    private sealed record WriteSuccessResponse(
        Guid Id,
        string ReferenceCode,
        string Status,
        uint Version,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

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
                grantedPermissions.Contains(permissionKey)
                    ? PermissionCheckResult.Grant()
                    : PermissionCheckResult.Deny("Not assigned."));
    }

    private sealed class GrantingFacilityAccessResolver : IFacilityAccessResolver
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
                request.PermissionKey,
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

    private sealed class SucceedingDraftWriteService : IProductionPlanDraftWriteService
    {
        public Task<ProductionPlanWriteApiResult> CreateDraftAsync(
            ProductionPlanWriteOperationContext operationContext,
            CreateProductionPlanDraftRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductionPlanWriteApiResult(
                true,
                Guid.NewGuid(),
                "PP-TEST-001",
                "draft",
                1u,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null, null, null));

        public Task<ProductionPlanWriteApiResult> UpdateDraftAsync(
            ProductionPlanWriteOperationContext operationContext,
            Guid productionPlanId,
            UpdateProductionPlanDraftRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductionPlanWriteApiResult(
                true,
                productionPlanId,
                "PP-TEST-001",
                "draft",
                2u,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow,
                null, null, null));
    }

    private sealed class FailingDraftWriteService(string failureCode) : IProductionPlanDraftWriteService
    {
        public Task<ProductionPlanWriteApiResult> CreateDraftAsync(
            ProductionPlanWriteOperationContext operationContext,
            CreateProductionPlanDraftRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductionPlanWriteApiResult(
                false, null, null, null, null, null, null,
                failureCode, "Test failure.", null));

        public Task<ProductionPlanWriteApiResult> UpdateDraftAsync(
            ProductionPlanWriteOperationContext operationContext,
            Guid productionPlanId,
            UpdateProductionPlanDraftRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductionPlanWriteApiResult(
                false, null, null, null, null, null, null,
                failureCode, "Test failure.", null));
    }
}
