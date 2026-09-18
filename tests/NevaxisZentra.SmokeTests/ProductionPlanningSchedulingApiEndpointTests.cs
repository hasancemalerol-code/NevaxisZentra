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

public sealed class ProductionPlanningSchedulingApiEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000005-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000005-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000005-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000005-0000-4000-8000-000000000004");
    private static readonly Guid PlanId = Guid.Parse("99000005-0000-4000-8000-000000000005");

    private static readonly DateTimeOffset ValidStart = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidEnd = new(2026, 9, 1, 14, 0, 0, TimeSpan.Zero);

    // ── Authentication / authorization ────────────────────────────────────────

    [Fact]
    public async Task ScheduleWithoutAuthenticationReturns401()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleWithoutFacilityIdReturns400()
    {
        await using var factory = CreateFactory(new GrantingScheduleAccessResolver());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidRequest, problem!.FailureCode);
    }

    [Fact]
    public async Task ScheduleWithoutMembershipReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.FacilityAccessDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.FacilityAccessDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task ScheduleWithoutSchedulePermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    // ── Success and workflow failures ─────────────────────────────────────────

    [Fact]
    public async Task ScheduleWithValidCommandReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingScheduleAccessResolver(),
            new SucceedingScheduleService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleSuccessResponseContainsSchedulingFields()
    {
        await using var factory = CreateFactory(
            new GrantingScheduleAccessResolver(),
            new SucceedingScheduleService());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);
        var result = await response.Content.ReadFromJsonAsync<ScheduleSuccessResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("scheduled", result.Status);
        Assert.NotEqual(default, result.PlannedStartAtUtc);
        Assert.NotEqual(default, result.PlannedEndAtUtc);
        Assert.NotEqual(default, result.ScheduledAtUtc);
    }

    [Fact]
    public async Task ScheduleWithInvalidTransitionReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingScheduleAccessResolver(),
            new FailingScheduleService(ProductionPlanWorkflowFailureCodes.InvalidTransition));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ScheduleWithBlockingRiskReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingScheduleAccessResolver(),
            new FailingScheduleService(ProductionPlanWorkflowFailureCodes.ScheduleBlockedByRisk));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ScheduleBlockedByRisk, problem!.FailureCode);
    }

    [Fact]
    public async Task ScheduleWithConcurrencyConflictReturns409()
    {
        await using var factory = CreateFactory(
            new GrantingScheduleAccessResolver(),
            new FailingScheduleService(ProductionPlanWorkflowFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/schedule?facilityId={FacilityId:D}",
            new { expectedVersion = 1, plannedStartAtUtc = ValidStart, plannedEndAtUtc = ValidEnd });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    // ── Factory / helpers ─────────────────────────────────────────────────────

    private static WebApplicationFactory<Program> CreateFactory(
        IFacilityAccessResolver? facilityResolver = null,
        IProductionPlanWorkflowService? workflowService = null)
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

                    if (workflowService is not null)
                    {
                        services.RemoveAll<IProductionPlanWorkflowService>();
                        services.AddSingleton(workflowService);
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

    private sealed record ScheduleSuccessResponse(
        Guid Id,
        string ReferenceCode,
        string PreviousStatus,
        string Status,
        uint Version,
        DateTimeOffset UpdatedAtUtc,
        string ApprovalStatus,
        DateTimeOffset PlannedStartAtUtc,
        DateTimeOffset PlannedEndAtUtc,
        DateTimeOffset ScheduledAtUtc);

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

    private sealed class GrantingScheduleAccessResolver : IFacilityAccessResolver
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
                ProductionPlanningPermissionKeys.Schedule,
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

    private sealed class SucceedingScheduleService : IProductionPlanWorkflowService
    {
        private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

        private static ProductionPlanWorkflowApiResult BuildScheduleSuccess() =>
            new(
                Succeeded: true,
                ProductionPlanId: PlanId,
                ReferenceCode: "PP-TEST-SCHED-001",
                PreviousStatus: "approved",
                Status: "scheduled",
                Version: 2u,
                UpdatedAtUtc: Now,
                ApprovalStatus: "approved",
                PlannedStartAtUtc: ValidStart,
                PlannedEndAtUtc: ValidEnd,
                ScheduledAtUtc: Now,
                FailureCode: null,
                FailureReason: null,
                FieldErrors: null);

        public Task<ProductionPlanWorkflowApiResult> SubmitForReviewAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, SubmitProductionPlanForReviewRequest req,
            CancellationToken ct = default) =>
            Task.FromResult(new ProductionPlanWorkflowApiResult(false, null, null, null, null, null, null, null, null, null, null, "stub", "stub", null));

        public Task<ProductionPlanWorkflowApiResult> RequestRevisionAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, RequestProductionPlanRevisionRequest req,
            CancellationToken ct = default) =>
            Task.FromResult(new ProductionPlanWorkflowApiResult(false, null, null, null, null, null, null, null, null, null, null, "stub", "stub", null));

        public Task<ProductionPlanWorkflowApiResult> ApproveAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, ApproveProductionPlanRequest req,
            CancellationToken ct = default) =>
            Task.FromResult(new ProductionPlanWorkflowApiResult(false, null, null, null, null, null, null, null, null, null, null, "stub", "stub", null));

        public Task<ProductionPlanWorkflowApiResult> RejectAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, RejectProductionPlanRequest req,
            CancellationToken ct = default) =>
            Task.FromResult(new ProductionPlanWorkflowApiResult(false, null, null, null, null, null, null, null, null, null, null, "stub", "stub", null));

        public Task<ProductionPlanWorkflowApiResult> ScheduleAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, ScheduleProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildScheduleSuccess());
    }

    private sealed class FailingScheduleService(string failureCode) : IProductionPlanWorkflowService
    {
        private ProductionPlanWorkflowApiResult BuildFailure() =>
            new(false, null, null, null, null, null, null, null, null, null, null, failureCode, "Stub failure.", null);

        public Task<ProductionPlanWorkflowApiResult> SubmitForReviewAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, SubmitProductionPlanForReviewRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildFailure());

        public Task<ProductionPlanWorkflowApiResult> RequestRevisionAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, RequestProductionPlanRevisionRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildFailure());

        public Task<ProductionPlanWorkflowApiResult> ApproveAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, ApproveProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildFailure());

        public Task<ProductionPlanWorkflowApiResult> RejectAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, RejectProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildFailure());

        public Task<ProductionPlanWorkflowApiResult> ScheduleAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, ScheduleProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildFailure());
    }
}
