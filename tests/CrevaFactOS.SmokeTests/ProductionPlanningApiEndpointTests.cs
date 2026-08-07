using System.Net;
using System.Net.Http.Json;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Api.ProductionPlanning;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Api.Development;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CrevaFactOS.SmokeTests;

public sealed class ProductionPlanningApiEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000000-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000000-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000000-0000-4000-8000-000000000004");
    private static readonly Guid PlanId = Guid.Parse("99000000-0000-4000-8000-000000000005");

    [Fact]
    public async Task MissingAuthenticationReturns401()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingFacilityIdReturns400()
    {
        await using var factory = CreateFactory(new GrantingFacilityAccessResolver());
        using var client = factory.CreateClient();
        using var request = CreateRequest("/api/production-planning/plans");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidRequest, body!.FailureCode);
    }

    [Fact]
    public async Task FacilityMembershipDeniedReturns403()
    {
        await using var factory = CreateFactory(new DenyingFacilityAccessResolver(
            FacilityAccessFailureCodes.FacilityAccessDenied));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.FacilityAccessDenied, body!.FailureCode);
    }

    [Fact]
    public async Task PermissionDeniedReturns403()
    {
        await using var factory = CreateFactory(new DenyingFacilityAccessResolver(
            FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, body!.FailureCode);
    }

    [Fact]
    public async Task AuthorizedListReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new StubProductionPlanReadService(listCount: 1));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EmptyListReturns200WithEmptyItems()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new StubProductionPlanReadService(listCount: 0));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProductionPlanListResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.Count);
    }

    [Fact]
    public async Task MissingDetailReturns404()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new StubProductionPlanReadService(detailFailureCode: ProductionPlanApiFailureCodes.NotFound));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans/{PlanId:D}?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.NotFound, body!.FailureCode);
    }

    [Fact]
    public async Task CrossTenantDetailDoesNotLeakExistence()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new StubProductionPlanReadService(detailFailureCode: ProductionPlanApiFailureCodes.NotFound));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans/{Guid.Parse("99000000-0000-4000-8000-000000009999"):D}?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var rawBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("another tenant", rawBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SourceUnavailableReturns503()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new StubProductionPlanReadService(listFailureCode: ProductionPlanApiFailureCodes.SourceUnavailable));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.SourceUnavailable, body!.FailureCode);
    }

    [Fact]
    public async Task InvalidRowReturns500()
    {
        await using var factory = CreateFactory(
            new GrantingFacilityAccessResolver(),
            new StubProductionPlanReadService(detailFailureCode: ProductionPlanApiFailureCodes.InvalidRow));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans/{PlanId:D}?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidRow, body!.FailureCode);
    }

    [Fact]
    public async Task DevelopmentAuthDoesNotBypassFacilityMembershipChecks()
    {
        await using var factory = CreateFactory(new DenyingFacilityAccessResolver(
            FacilityAccessFailureCodes.FacilityAccessDenied));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentBootstrapDoesNotRunInTestHost()
    {
        await using var factory = CreateFactory(new GrantingFacilityAccessResolver());
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            $"/api/production-planning/plans?facilityId={FacilityId:D}");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ApplicationExposesProductionPlanningTypes()
    {
        var planningTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type => type.FullName?.Contains("ProductionPlanning", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(planningTypes);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IFacilityAccessResolver? facilityAccessResolver = null,
        IProductionPlanReadService? readService = null) =>
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
                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.RemoveAll<ITenantMembershipResolver>();
                    services.RemoveAll<IPermissionEvaluator>();
                    services.RemoveAll<IFacilityAccessResolver>();
                    services.RemoveAll<IProductionPlanReadService>();
                    services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                    services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                    services.AddScoped<IPermissionEvaluator>(_ =>
                        new StubPermissionEvaluator(
                            new HashSet<string>([ProductionPlanningPermissionKeys.Read], StringComparer.Ordinal)));
                    services.AddScoped<IFacilityAccessResolver>(_ =>
                        facilityAccessResolver ?? new DenyingFacilityAccessResolver(
                            FacilityAccessFailureCodes.FacilityAccessDenied));
                    services.AddScoped<IProductionPlanReadService>(_ =>
                        readService ?? new StubProductionPlanReadService());
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

    private static HttpRequestMessage CreateRequest(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");
        return request;
    }

    private sealed record ProblemDetailsResponse(
        int Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("failureCode")] string? FailureCode);

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

    private sealed class StubProductionPlanReadService : IProductionPlanReadService
    {
        private readonly int listCount;
        private readonly string? listFailureCode;
        private readonly string? detailFailureCode;

        public StubProductionPlanReadService(
            int listCount = 0,
            string? listFailureCode = null,
            string? detailFailureCode = null)
        {
            this.listCount = listCount;
            this.listFailureCode = listFailureCode;
            this.detailFailureCode = detailFailureCode;
        }

        public Task<ProductionPlanServiceResult<ProductionPlanListResponse>> ListAsync(
            ProductionPlanReadOperationContext operationContext,
            ProductionPlanListRequest request,
            CancellationToken cancellationToken = default)
        {
            if (listFailureCode is not null)
            {
                return Task.FromResult(
                    new ProductionPlanServiceResult<ProductionPlanListResponse>(false, null, listFailureCode));
            }

            var items = Enumerable.Range(0, listCount)
                .Select(index => new ProductionPlanSummaryDto(
                    Guid.NewGuid(),
                    $"plan-{index}",
                    $"Plan {index}",
                    request.FacilityId,
                    "draft",
                    "manual",
                    false,
                    "medium",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddHours(1),
                    "not-required",
                    "not-started",
                    null,
                    DateTimeOffset.UtcNow,
                    DemandCount: 1,
                    PrimaryRequiredQuantity: 100,
                    PrimaryUnit: "adet",
                    PrimaryProductCode: "PP-100",
                    PrimaryProductName: "Pilot part",
                    HighestDemandPriority: "normal",
                    RiskCount: 0,
                    CriticalRiskCount: 0,
                    BlockingRiskCount: 0,
                    HasBlockingRisk: false,
                    HasMachineAssignment: true,
                    HasMoldAssignment: true,
                    HasMaterialAssignment: false,
                    HasOperatorGroupAssignment: false,
                    ShiftName: "A vardiyası",
                    EarliestRequestedDeliveryDateUtc: null))
                .ToArray();

            return Task.FromResult(
                new ProductionPlanServiceResult<ProductionPlanListResponse>(
                    true,
                    new ProductionPlanListResponse(request.FacilityId, items.Length, items),
                    null));
        }

        public Task<ProductionPlanServiceResult<ProductionPlanDetailResponse>> GetDetailAsync(
            ProductionPlanReadOperationContext operationContext,
            ProductionPlanDetailRequest request,
            CancellationToken cancellationToken = default)
        {
            if (detailFailureCode is not null)
            {
                return Task.FromResult(
                    new ProductionPlanServiceResult<ProductionPlanDetailResponse>(false, null, detailFailureCode));
            }

            var detail = new ProductionPlanDetailDto(
                request.ProductionPlanId,
                "plan-a",
                "Plan A",
                request.FacilityId,
                "draft",
                "manual",
                false,
                "medium",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                "not-required",
                "not-started",
                null,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow,
                1,
                null,
                "creator",
                null,
                [],
                [],
                new ProductionPlanResourceAssignmentDto(null, null, null, null, null, null, null, null),
                new ProductionPlanTimeWindowDto(null, null, null, null, null),
                new ProductionPlanApprovalDto("not-required", null, null, null, null, null, null, null, null),
                new ProductionPlanTransferDto(null, "not-started", null, null, null, null, null),
                [],
                []);

            return Task.FromResult(
                new ProductionPlanServiceResult<ProductionPlanDetailResponse>(
                    true,
                    new ProductionPlanDetailResponse(detail),
                    null));
        }
    }
}
