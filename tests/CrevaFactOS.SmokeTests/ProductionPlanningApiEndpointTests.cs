using System.Net;
using System.Net.Http.Json;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Api.Development;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Api.ProductionPlanning;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CrevaFactOS.SmokeTests;

public sealed class ProductionPlanningApiEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000002-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000002-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000002-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000002-0000-4000-8000-000000000004");
    private static readonly Guid PlanId = Guid.Parse("99000002-0000-4000-8000-000000000005");

    // ── Submit for review ─────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitWithoutAuthenticationReturns401()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitWithoutFacilityIdReturns400()
    {
        await using var factory = CreateFactory(new GrantingReviewAccessResolver());
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidRequest, problem!.FailureCode);
    }

    [Fact]
    public async Task SubmitWithoutMembershipReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.FacilityAccessDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.FacilityAccessDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task SubmitWithoutReviewPermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task SubmitWithReviewPermissionReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new SucceedingWorkflowService("ready-for-approval"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitStaleVersionReturns409ConcurrencyConflict()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    [Fact]
    public async Task SubmitApprovedPlanReturns409InvalidTransition()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.InvalidTransition));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidTransition, problem!.FailureCode);
    }

    // ── Request revision ─────────────────────────────────────────────────────

    [Fact]
    public async Task RevisionWithoutReviewPermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/request-revision?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Needs more detail." });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task RevisionWithReviewPermissionReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new SucceedingWorkflowService("needs-review"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/request-revision?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Needs more detail." });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RevisionWithEmptyReasonReturns400()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.ValidationFailed));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/request-revision?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "" });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ValidationFailed, problem!.FailureCode);
    }

    [Fact]
    public async Task RevisionDraftPlanReturns409InvalidTransition()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.InvalidTransition));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/request-revision?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Needs more detail." });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidTransition, problem!.FailureCode);
    }

    [Fact]
    public async Task RevisionStaleVersionReturns409ConcurrencyConflict()
    {
        await using var factory = CreateFactory(
            new GrantingReviewAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/request-revision?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Needs more detail." });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveWithoutApprovePermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task ApproveWithApprovePermissionReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new SucceedingWorkflowService("approved"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApproveDraftPlanReturns409InvalidTransition()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.InvalidTransition));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidTransition, problem!.FailureCode);
    }

    [Fact]
    public async Task ApproveNeedsReviewPlanReturns409InvalidTransition()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.InvalidTransition));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidTransition, problem!.FailureCode);
    }

    [Fact]
    public async Task ApproveStaleVersionReturns409ConcurrencyConflict()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    [Fact]
    public async Task ApproveWithSourceUnavailableReturns503()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.SourceUnavailable));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.SourceUnavailable, problem!.FailureCode);
    }

    // ── Maker-checker: self-approval blocked ──────────────────────────────────

    [Fact]
    public async Task ApproveSelfEditedPlanReturns409SelfApprovalNotAllowed()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.SelfApprovalNotAllowed));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("self-approval-not-allowed", problem!.FailureCode);
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectWithoutApprovePermissionReturns403()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/reject?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Not sufficient." });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.PermissionDenied, problem!.FailureCode);
    }

    [Fact]
    public async Task RejectWithApprovePermissionReturns200()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new SucceedingWorkflowService("rejected"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/reject?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Not sufficient." });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RejectWithMissingReasonReturns400()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.ValidationFailed));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/reject?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "" });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ValidationFailed, problem!.FailureCode);
    }

    [Fact]
    public async Task RejectDraftPlanReturns409InvalidTransition()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.InvalidTransition));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/reject?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Not sufficient." });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.InvalidTransition, problem!.FailureCode);
    }

    [Fact]
    public async Task RejectStaleVersionReturns409ConcurrencyConflict()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.ConcurrencyConflict));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/reject?facilityId={FacilityId:D}",
            new { expectedVersion = 1, reason = "Not sufficient." });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.ConcurrencyConflict, problem!.FailureCode);
    }

    // ── Permission separation ─────────────────────────────────────────────────

    [Fact]
    public async Task ReviewPermissionCannotApprove()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied),
            workflowService: new SucceedingWorkflowService("approved"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WritePermissionAloneCannotSubmit()
    {
        await using var factory = CreateFactory(
            new DenyingFacilityAccessResolver(FacilityAccessFailureCodes.PermissionDenied),
            workflowService: new SucceedingWorkflowService("ready-for-approval"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/submit-for-review?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NotFoundReturns404()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.NotFound));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProductionPlanApiFailureCodes.NotFound, problem!.FailureCode);
    }

    [Fact]
    public async Task ApproveSuccessBodyContainsApprovedStatus()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new SucceedingWorkflowService("approved"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var result = await response.Content.ReadFromJsonAsync<WorkflowSuccessResponse>();

        Assert.Equal("approved", result!.Status);
    }

    [Fact]
    public async Task ApproveSuccessBodyContainsIncreasedVersion()
    {
        await using var factory = CreateFactory(
            new GrantingApproveAccessResolver(),
            workflowService: new SucceedingWorkflowService("approved"));
        using var client = factory.CreateClient();
        using var body = CreateRequest(
            HttpMethod.Post,
            $"/api/production-planning/plans/{PlanId:D}/approve?facilityId={FacilityId:D}",
            new { expectedVersion = 1 });

        using var response = await client.SendAsync(body);
        var result = await response.Content.ReadFromJsonAsync<WorkflowSuccessResponse>();

        Assert.True(result!.Version >= 1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WebApplicationFactory<Program> CreateFactory(
        IFacilityAccessResolver? facilityAccessResolver = null,
        IProductionPlanWorkflowService? workflowService = null) =>
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
                    services.RemoveAll<IProductionPlanWorkflowService>();
                    services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                    services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                    services.AddScoped<IPermissionEvaluator>(_ =>
                        new StubPermissionEvaluator(
                            new HashSet<string>(
                                [ProductionPlanningPermissionKeys.Review, ProductionPlanningPermissionKeys.Approve],
                                StringComparer.Ordinal)));
                    services.AddScoped<IFacilityAccessResolver>(_ =>
                        facilityAccessResolver ?? new DenyingFacilityAccessResolver(
                            FacilityAccessFailureCodes.FacilityAccessDenied));
                    services.AddScoped<IProductionPlanWorkflowService>(_ =>
                        workflowService ?? new FailingWorkflowService(ProductionPlanWorkflowFailureCodes.SourceUnavailable));
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
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");
        return request;
    }

    private sealed record ProblemDetailsResponse(
        int Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("failureCode")] string? FailureCode);

    private sealed record WorkflowSuccessResponse(
        Guid Id,
        string ReferenceCode,
        string PreviousStatus,
        string Status,
        uint Version,
        DateTimeOffset UpdatedAtUtc,
        string ApprovalStatus);

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

    private sealed class GrantingReviewAccessResolver : IFacilityAccessResolver
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
                ProductionPlanningPermissionKeys.Review,
                "test",
                true);
            return Task.FromResult(FacilityAccessResolutionResult.Grant(access, DateTimeOffset.UtcNow));
        }
    }

    private sealed class GrantingApproveAccessResolver : IFacilityAccessResolver
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
                ProductionPlanningPermissionKeys.Approve,
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

    private sealed class SucceedingWorkflowService(string newStatus) : IProductionPlanWorkflowService
    {
        private ProductionPlanWorkflowApiResult BuildSuccess() =>
            new(
                Succeeded: true,
                ProductionPlanId: PlanId,
                ReferenceCode: "PP-TEST-WF-001",
                PreviousStatus: "draft",
                Status: newStatus,
                Version: 2u,
                UpdatedAtUtc: DateTimeOffset.UtcNow,
                ApprovalStatus: newStatus,
                PlannedStartAtUtc: null,
                PlannedEndAtUtc: null,
                ScheduledAtUtc: null,
                FailureCode: null,
                FailureReason: null,
                FieldErrors: null);

        public Task<ProductionPlanWorkflowApiResult> SubmitForReviewAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, SubmitProductionPlanForReviewRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildSuccess());

        public Task<ProductionPlanWorkflowApiResult> RequestRevisionAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, RequestProductionPlanRevisionRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildSuccess());

        public Task<ProductionPlanWorkflowApiResult> ApproveAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, ApproveProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildSuccess());

        public Task<ProductionPlanWorkflowApiResult> RejectAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, RejectProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildSuccess());

        public Task<ProductionPlanWorkflowApiResult> ScheduleAsync(
            ProductionPlanWriteOperationContext ctx, Guid id, ScheduleProductionPlanRequest req,
            CancellationToken ct = default) => Task.FromResult(BuildSuccess());
    }

    private sealed class FailingWorkflowService(string failureCode) : IProductionPlanWorkflowService
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
