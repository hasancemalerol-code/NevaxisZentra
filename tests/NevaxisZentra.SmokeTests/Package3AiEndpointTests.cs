using System.Net;
using System.Net.Http.Json;
using NevaxisZentra.Api.AiTasks;
using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Api.DigitalWorkforce;
using NevaxisZentra.Api.Export;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.Maintenance;
using NevaxisZentra.Api.SaasSubscription;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.AiUsage;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Maintenance;
using NevaxisZentra.Persistence.SaasSubscription;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.SmokeTests;

/// <summary>RC3 Package 3: module entitlement at the API, accept-recommendation authority and Production defaults.</summary>
public sealed class Package3AiEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000031-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000031-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000031-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000031-0000-4000-8000-000000000004");

    [Theory]
    [InlineData("/api/ai-tasks", "/ai-gorevleri")]
    [InlineData("/api/ai-tasks/00000000-0000-0000-0000-000000000001/ai/analyze", "/ai-gorevleri")]
    [InlineData("/api/ai-task-deliveries/00000000-0000-0000-0000-000000000001", "/ai-gorevleri")]
    [InlineData("/api/ai-task-secure-access/x/revoke", "/ai-gorevleri")]
    [InlineData("/api/export-cases/00000000-0000-0000-0000-000000000001/ai/analyze", "/ihracat")]
    [InlineData("/api/ai/usage/current", "/ihracat")]
    [InlineData("/api/factory-copilot/query", "/fabrika")]
    [InlineData("/api/model-gateway/invoke", "/fabrika")]
    [InlineData("/api/intelligence-runtime/control-center", "/fabrika")]
    [InlineData("/api/throughput-optimizer/lines/x/observe", "/fabrika")]
    [InlineData("/api/throughput-intelligence/lines/x", "/fabrika")]
    [InlineData("/api/production-manager/workspace", "/uretim")]
    [InlineData("/api/maintenance/assets/ASSET-1/ai-drafts/generate", "/bakim")]
    public void ShippingAiApiRoutesMapToTheirModule(string apiPath, string expectedRoute)
    {
        Assert.Equal(expectedRoute, TenantSubscriptionAccessMiddleware.MapApiPathToModuleRoute(apiPath));
    }

    [Theory]
    [InlineData("/ai-gorevleri", SaasSubscriptionModuleEntitlements.EntitlementCodes.AiTaskCenter)]
    [InlineData("/ihracat", SaasSubscriptionModuleEntitlements.EntitlementCodes.Export)]
    [InlineData("/fabrika", SaasSubscriptionModuleEntitlements.EntitlementCodes.Production)]
    public void TheMappedApiModulesResolveToTheOwnerApprovedEntitlements(string route, string expectedCode)
    {
        Assert.True(SaasSubscriptionModuleEntitlements.TryResolveEntitlementCode(route, out var code));
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("/api/ai-tasks?facilityId=x")]
    [InlineData("/api/factory-copilot/suggested-questions")]
    public async Task ATenantWithoutTheModuleIsRefusedByTheApiBeforeAnyHandlerRuns(string path)
    {
        var evaluator = new ModuleDenyingEvaluator();
        await using var factory = CreateFactory(evaluator, repository: null, resolver: new GrantingAccessResolver());
        using var client = factory.CreateClient();

        using var request = CreateRequest(path.Contains("suggested", StringComparison.Ordinal) ? HttpMethod.Post : HttpMethod.Get, path);
        if (request.Method == HttpMethod.Post)
        {
            request.Content = JsonContent.Create(new { context = new { entryPoint = "LINE" } });
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("module", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Single(evaluator.ModuleRoutes);
    }

    [Fact]
    public async Task AcceptingARecommendationNeedsThePermissionOfItsMutationNotOnlyFeedback()
    {
        var repository = new RecordingDecisionRepository();
        var resolver = new SelectiveAccessResolver(AiTaskPermissionKeys.AiFeedback);
        await using var factory = CreateFactory(new AllowAllEvaluator(), repository, resolver);
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/ai-tasks/{Guid.NewGuid():D}/ai/recommendations/{Guid.NewGuid():D}/accept?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(new { expectedVersion = 1, reason = "ok", editedPayloadJson = (string?)null });
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(repository.LastAccept);
        var check = repository.LastAccept!.HasPermissionAsync;
        Assert.NotNull(check);

        // Feedback alone was granted: every mutating permission is refused, the granted one is accepted.
        Assert.True(await check!(AiTaskPermissionKeys.AiFeedback, CancellationToken.None));
        Assert.False(await check(AiTaskPermissionKeys.Assign, CancellationToken.None));
        Assert.False(await check(AiTaskPermissionKeys.Manage, CancellationToken.None));
        Assert.False(await check(AiTaskPermissionKeys.Escalate, CancellationToken.None));
        Assert.False(await check(AiTaskPermissionKeys.PlaybookRun, CancellationToken.None));
    }

    [Fact]
    public async Task AcceptingWithoutFeedbackPermissionNeverReachesTheRepository()
    {
        var repository = new RecordingDecisionRepository();
        await using var factory = CreateFactory(new AllowAllEvaluator(), repository, new SelectiveAccessResolver());
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/ai-tasks/{Guid.NewGuid():D}/ai/recommendations/{Guid.NewGuid():D}/accept?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(new { expectedVersion = 1 });
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(repository.LastAccept);
    }

    [Fact]
    public void RulesBasedAssistantsAreUnavailableInProductionByDefault()
    {
        var production = BuildAssistantProvider("Production", null);

        Assert.IsType<UnavailableExportAiProvider>(production.GetRequiredService<IExportAiProvider>());
        Assert.IsType<UnavailableMaintenanceAiProvider>(production.GetRequiredService<IMaintenanceAiProvider>());
        var aiTask = production.GetRequiredService<IAiTaskDecisionProvider>();
        Assert.IsType<UnavailableAiTaskDecisionProvider>(aiTask);
        Assert.False(aiTask.IsAvailable);
    }

    [Fact]
    public void RulesBasedAssistantsAreAvailableInProductionOnlyWhenTheOperatorEnablesThem()
    {
        var production = BuildAssistantProvider("Production", "true");
        var development = BuildAssistantProvider("Development", null);

        Assert.IsType<DeterministicExportAiProvider>(production.GetRequiredService<IExportAiProvider>());
        Assert.IsType<DeterministicMaintenanceAiProvider>(production.GetRequiredService<IMaintenanceAiProvider>());
        Assert.IsType<DeterministicAiTaskDecisionProvider>(production.GetRequiredService<IAiTaskDecisionProvider>());
        Assert.IsType<DeterministicExportAiProvider>(development.GetRequiredService<IExportAiProvider>());
    }

    [Theory]
    [InlineData("Production", null, false)]
    [InlineData("Production", "false", false)]
    [InlineData("Production", "true", true)]
    [InlineData("Development", null, true)]
    public void DigitalWorkforceExecutionWorkerIsOffInProductionByDefault(string environment, string? setting, bool expected)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new StubEnvironment(environment));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(setting is null
                ? []
                : new Dictionary<string, string?> { [DigitalWorkforceExecutionSettings.ConfigurationKey] = setting })
            .Build());
        services.AddNevaxisZentraDigitalWorkforceExecutionRuntime();
        using var provider = services.BuildServiceProvider();

        Assert.Equal(expected, provider.GetRequiredService<IOptions<DigitalWorkforceExecutionOptions>>().Value.WorkerEnabled);
    }

    private static ServiceProvider BuildAssistantProvider(string environment, string? rulesBasedSetting)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new StubEnvironment(environment));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(rulesBasedSetting is null
                ? []
                : new Dictionary<string, string?> { ["RulesBasedAi:Enabled"] = rulesBasedSetting })
            .Build());
        services.AddNevaxisZentraExport();
        services.AddNevaxisZentraMaintenanceAi();
        services.AddNevaxisZentraAiTasks();
        return services.BuildServiceProvider();
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-NevaxisZentra-Tenant", "tenant-a");
        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory(
        ITenantEntitlementEvaluator evaluator,
        IAiTaskDecisionIntelligenceRepository? repository,
        IFacilityAccessResolver resolver) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
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
                services.RemoveAll<ITenantEntitlementEvaluator>();
                services.AddSingleton(evaluator);
                services.RemoveAll<IFacilityAccessResolver>();
                services.AddSingleton(resolver);
                services.RemoveAll<IIdentitySubjectResolver>();
                services.AddSingleton<IIdentitySubjectResolver>(new StubIdentityResolver());
                services.RemoveAll<ITenantMembershipResolver>();
                services.AddSingleton<ITenantMembershipResolver>(new StubMembershipResolver());
                if (repository is not null)
                {
                    services.RemoveAll<IAiTaskDecisionIntelligenceRepository>();
                    services.AddSingleton(repository);
                }
            });
        });

    private sealed class StubEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
            string externalIssuer, string externalSubject, CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(
                externalIssuer == "local-tests" && externalSubject == "active-subject"
                    ? new AuthenticatedIdentity(IdentityId, externalIssuer, externalSubject)
                    : null);
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(
            Guid identitySubjectId, string tenantIdentifier, CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantContext?>(
                identitySubjectId == IdentityId && tenantIdentifier == "tenant-a"
                    ? new TenantContext(TenantId, "tenant-a", MembershipId)
                    : null);
    }

    private sealed class GrantingAccessResolver : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(FacilityAccessResolutionResult.Grant(
                new FacilityAccessContext(TenantId, request.FacilityId, MembershipId, IdentityId, request.PermissionKey, "test", true),
                DateTimeOffset.UtcNow));
    }

    /// <summary>Grants only the listed permission keys; everything else is a permission denial.</summary>
    private sealed class SelectiveAccessResolver(params string[] granted) : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(granted.Contains(request.PermissionKey, StringComparer.Ordinal)
                ? FacilityAccessResolutionResult.Grant(
                    new FacilityAccessContext(TenantId, request.FacilityId, MembershipId, IdentityId, request.PermissionKey, "test", true),
                    DateTimeOffset.UtcNow)
                : FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.PermissionDenied,
                    "Denied in test.",
                    DateTimeOffset.UtcNow,
                    facilityId: request.FacilityId,
                    permissionKey: request.PermissionKey));
    }

    private sealed class RecordingDecisionRepository : IAiTaskDecisionIntelligenceRepository
    {
        public AcceptAiTaskRecommendationCommand? LastAccept { get; private set; }

        public Task<AiTaskDecisionWriteResult> AcceptRecommendationAsync(
            AiTaskOperationContext context, AcceptAiTaskRecommendationCommand command, CancellationToken cancellationToken = default)
        {
            LastAccept = command;
            return Task.FromResult(AiTaskDecisionWriteResult.Success(command.RecommendationId, 2));
        }

        public Task<AiTaskDecisionWorkspaceReadModel> GetWorkspaceAsync(AiTaskOperationContext context, Guid taskId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");

        public Task<AiTaskDecisionWriteResult> AnalyzeAsync(AiTaskOperationContext context, AnalyzeAiTaskDecisionCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");

        public Task<AiTaskDecisionWriteResult> RejectRecommendationAsync(AiTaskOperationContext context, RejectAiTaskRecommendationCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");

        public Task<AiTaskDecisionWriteResult> ConfirmRelationshipAsync(AiTaskOperationContext context, ConfirmAiTaskRelationshipCommand command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");

        public Task<AiTaskDecisionClosureReviewResult?> ReviewClosureAsync(AiTaskOperationContext context, Guid taskId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");
    }

    private class AllowAllEvaluator : ITenantEntitlementEvaluator
    {
        protected static TenantEntitlementEvaluationReadModel Allowed() => TenantEntitlementEvaluationReadModel.Allow("active", null);

        public virtual Task<TenantEntitlementEvaluationReadModel> EvaluatePaidApplicationAccessAsync(CancellationToken cancellationToken = default) => Task.FromResult(Allowed());
        public virtual Task<TenantEntitlementEvaluationReadModel> EvaluateModuleAccessAsync(string routePath, CancellationToken cancellationToken = default) => Task.FromResult(Allowed());
        public Task<TenantEntitlementEvaluationReadModel> EvaluateFeatureAccessAsync(string featureCode, CancellationToken cancellationToken = default) => Task.FromResult(Allowed());
        public Task<TenantEntitlementEvaluationReadModel> EvaluateLimitAsync(string limitEntitlementCode, CancellationToken cancellationToken = default) => Task.FromResult(Allowed());
        public Task<TenantEntitlementEvaluationReadModel> EvaluateEntitlementAsync(string entitlementCode, CancellationToken cancellationToken = default) => Task.FromResult(Allowed());
        public Task<TenantEntitlementEvaluationReadModel> EvaluateUserLimitForAdditionalSeatsAsync(int additionalSeats, CancellationToken cancellationToken = default) => Task.FromResult(Allowed());
    }

    private sealed class ModuleDenyingEvaluator : AllowAllEvaluator
    {
        public List<string> ModuleRoutes { get; } = [];

        public override Task<TenantEntitlementEvaluationReadModel> EvaluateModuleAccessAsync(string routePath, CancellationToken cancellationToken = default)
        {
            ModuleRoutes.Add(routePath);
            return Task.FromResult(TenantEntitlementEvaluationReadModel.Deny(
                "saas-subscription.entitlement.module-not-entitled",
                "module-not-entitled"));
        }
    }
}
