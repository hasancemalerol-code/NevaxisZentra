using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NevaxisZentra.Configuration;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Bootstrap;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Api.Tenancy;
using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using NevaxisZentra.Api.Audit;
using NevaxisZentra.Api.Development;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.Export;
using NevaxisZentra.Api.Production;
using NevaxisZentra.Api.ProductionPlanning;
using NevaxisZentra.Api.Maintenance;
using NevaxisZentra.Api.Quality;
using NevaxisZentra.Api.Material;
using NevaxisZentra.Api.Mold;
using NevaxisZentra.Api.ProcessOee;
using NevaxisZentra.Api.Procurement;
using NevaxisZentra.Api.ShiftWorkforce;
using NevaxisZentra.Api.ShiftAssignment;
using NevaxisZentra.Api.ShiftRisk;
using NevaxisZentra.Api.Energy;
using NevaxisZentra.Api.SaasCommercial;
using NevaxisZentra.Api.SaasIdentity;
using NevaxisZentra.Api.SaasSubscription;
using NevaxisZentra.Api.SaasBilling;
using NevaxisZentra.Api.AdminAccess;
using NevaxisZentra.Api.Erp;
using NevaxisZentra.Api.Integration;
using NevaxisZentra.Api.DigitalWorkforce;
using NevaxisZentra.Api.Edge;
using NevaxisZentra.Api.IotPlc;
using NevaxisZentra.Api.OperationalRisk;
using NevaxisZentra.Api.CommercialIntelligence;
using NevaxisZentra.Api.AiTasks;
using NevaxisZentra.Api.Teams;
using NevaxisZentra.Api.ShiftHandover;
using NevaxisZentra.Api.ShiftOperations;
using NevaxisZentra.Api.FactoryOrchestration;
using NevaxisZentra.Api.FactoryDecisionCenter;
using NevaxisZentra.Api.FactoryIntelligence;
using NevaxisZentra.Api.MixedProduction;
using NevaxisZentra.Api.ThroughputIntelligence;
using NevaxisZentra.Api.ContextKnowledge;
using NevaxisZentra.Api.ModelGateway;
using NevaxisZentra.Api.ThroughputOptimizer;
using NevaxisZentra.Api.ScenarioRecovery;
using NevaxisZentra.Api.RealFactoryPilot;
using NevaxisZentra.Api.BulkDataOnboarding;
using NevaxisZentra.Api.FactoryOutcomeExecution;
using NevaxisZentra.Api.ProductionManager;
using NevaxisZentra.Api.FactoryLearning;
using NevaxisZentra.Api.AdaptiveProductionPlanning;
using NevaxisZentra.Api.FactoryNetworkOptimization;
using NevaxisZentra.Api.EnterpriseReliability;
using NevaxisZentra.Api.GoldenDemoP1;
using NevaxisZentra.Api.GoldenDemoP2;
using NevaxisZentra.Api.GoldenDemoP2B;
using NevaxisZentra.Api.GoldenDemoP3;
using NevaxisZentra.Api.GoldenDemoCustomsFile;
using NevaxisZentra.Api.FactoryCopilot;
using NevaxisZentra.Api.ContinuousOptimization;
using NevaxisZentra.Api.IntelligenceRuntime;
using NevaxisZentra.Api.OperationalValue;
using NevaxisZentra.Api.MarketRates;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.AiDecisions;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.FactoryCopilot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Application:Name"] ??= "Nevaxis Zentra API";
ValidateConfiguration(builder.Configuration);

builder.Services.AddSingleton<DevelopmentPilotBootstrapState>();
builder.Services.AddHealthChecks()
    .AddCheck<DevelopmentPilotBootstrapHealthCheck>("development-pilot-bootstrap");
builder.Services.AddInfrastructureOptions(builder.Configuration);
builder.Services.AddNevaxisZentraPersistence(builder.Configuration, builder.Environment);
builder.Services.AddFirstAccountEnrollment();
builder.Services.AddNevaxisZentraAuthentication(
    builder.Configuration,
    builder.Environment);
builder.Services.AddAuthorization(PermissionPolicies.AddInfrastructurePolicies);
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddNevaxisZentraFacilityAccess();
builder.Services.AddNevaxisZentraProductionPlanning();
builder.Services.AddNevaxisZentraProductionExecution();
builder.Services.AddNevaxisZentraQuality();
builder.Services.AddNevaxisZentraMold();
builder.Services.AddNevaxisZentraMaterial();
builder.Services.AddNevaxisZentraProcessOee();
builder.Services.AddNevaxisZentraShiftWorkforce();
builder.Services.AddNevaxisZentraShiftAssignment();
builder.Services.AddNevaxisZentraShiftRisk();
builder.Services.AddNevaxisZentraEnergy();
builder.Services.AddNevaxisZentraEnergyIntelligence();
builder.Services.AddNevaxisZentraSaasCommercial();
builder.Services.AddNevaxisZentraSaasSubscriptionApi();
builder.Services.AddNevaxisZentraSaasBillingApi(builder.Configuration);
builder.Services.AddNevaxisZentraSaasIdentity();
builder.Services.AddNevaxisZentraAdminAccess();
builder.Services.AddNevaxisZentraMaintenance();
builder.Services.AddNevaxisZentraExport();
builder.Services.AddNevaxisZentraErp();
builder.Services.AddNevaxisZentraIntegration();
builder.Services.AddNevaxisZentraFactoryEventSpine();
builder.Services.AddNevaxisZentraDigitalWorkforce();
builder.Services.AddNevaxisZentraProcurement();
builder.Services.AddNevaxisZentraEdge();
builder.Services.AddNevaxisZentraIotPlc();
builder.Services.AddNevaxisZentraIotPlcIntelligence();
builder.Services.AddNevaxisZentraOperationalRisk();
builder.Services.AddNevaxisZentraAiTasks();
builder.Services.AddNevaxisZentraAiTaskDelivery(builder.Configuration);
builder.Services.AddAiTaskSecureAccessRateLimiting();
builder.Services.AddNevaxisZentraTeams();
builder.Services.AddNevaxisZentraShiftHandover();
builder.Services.AddNevaxisZentraShiftOperations();
builder.Services.AddNevaxisZentraFactoryOrchestration();
builder.Services.AddNevaxisZentraFactoryDecisionCenter();
builder.Services.AddNevaxisZentraFactoryIntelligence();
builder.Services.AddNevaxisZentraMixedProduction();
builder.Services.AddNevaxisZentraThroughputIntelligence();
builder.Services.AddNevaxisZentraContextKnowledge();
builder.Services.AddNevaxisZentraModelGateway(allowSyntheticProviders: builder.Environment.IsDevelopment());
builder.Services.AddNevaxisZentraThroughputOptimizer();
builder.Services.AddNevaxisZentraScenarioRecovery();
builder.Services.AddNevaxisZentraFactoryCopilot();
builder.Services.AddNevaxisZentraContinuousOptimizationApi();
builder.Services.AddNevaxisZentraIntelligenceRuntimeApi();
builder.Services.AddNevaxisZentraOperationalValueApi();
builder.Services.AddNevaxisZentraRealFactoryPilot();
builder.Services.AddNevaxisZentraBulkDataOnboardingApi();
builder.Services.AddNevaxisZentraProductionManager(scenarioEnginesEnabled: builder.Environment.IsDevelopment());
builder.Services.AddNevaxisZentraFactoryLearning();
builder.Services.AddNevaxisZentraAdaptivePlanning();
builder.Services.AddNevaxisZentraFactoryNetwork();
builder.Services.AddNevaxisZentraEnterpriseReliabilityApi();
builder.Services.AddNevaxisZentraGoldenDemoP1Api();
builder.Services.AddNevaxisZentraGoldenDemoP2Api();
builder.Services.AddNevaxisZentraGoldenDemoP2BApi();
builder.Services.AddNevaxisZentraGoldenDemoP3Api();
builder.Services.AddNevaxisZentraGoldenDemoCustomsFileApi();
builder.Services.AddNevaxisZentraMarketRates(builder.Configuration);
builder.Services.AddNevaxisZentraCommercialIntelligence();
builder.Services.AddNevaxisZentraCommercialSalesPrice();
builder.Services.AddNevaxisZentraCommercialQuotationRisk();
builder.Services.AddNevaxisZentraCommercialProfitability();
builder.Services.AddNevaxisZentraCommercialProfitabilityAlerts();
builder.Services.AddNevaxisZentraCommercialPricingScenarios();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, UniformAuthorizationResultHandler>();

NevaxisZentra.Persistence.Development.DevelopmentPilotServiceCollectionExtensions
    .ValidateDevelopmentPilotProductionGuard(
        builder.Configuration,
        builder.Environment);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddNevaxisZentraDevelopmentPilot(
        builder.Configuration,
        builder.Environment);
    builder.Services.AddNevaxisZentraDevelopmentPilotHostedBootstrap(
        builder.Environment);
    builder.Services.AddSingleton<
        NevaxisZentra.Persistence.ProductionPlanning.IProductionPlanTransferAdapter,
        NevaxisZentra.Persistence.ProductionPlanning.FakeProductionPlanTransferAdapter>();
}

var app = builder.Build();

app.UseRouting();
app.UseMiddleware<CorrelationContextMiddleware>();
app.UseAuthentication();
app.UseMiddleware<SaasIdentityCsrfMiddleware>();
app.UseMiddleware<IdentityResolutionMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<TenantSubscriptionAccessMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapProductionPlanningReadEndpoints();
app.MapProductionPlanningWriteEndpoints();
app.MapProductionPlanningWorkflowEndpoints();
app.MapProductionPlanningLogisticsEndpoints();

app.MapProductionExecutionReadEndpoints();
app.MapProductionExecutionWriteEndpoints();
app.MapProductionResourceExecutionReadEndpoints();
app.MapProductionReportingEndpoints();

app.MapQualityEndpoints();
app.MapMaterialEndpoints();
app.MapProcessOeeEndpoints();
app.MapShiftWorkforceEndpoints();
app.MapShiftAssignmentEndpoints();
app.MapShiftRiskEndpoints();
app.MapEnergyEndpoints();
app.MapEnergyIntelligenceEndpoints();
app.MapSaasCommercialEndpoints();
app.MapSaasSubscriptionEndpoints();
app.MapSaasBillingEndpoints();
app.MapSaasBillingPaymentEndpoints();
app.MapSaasIdentityEndpoints();
app.MapAdminAccessEndpoints();
app.MapMoldEndpoints();
app.MapMoldHistoryEndpoints();
app.MapMoldCompatibilityEndpoints();
app.MapMaintenanceEndpoints();
app.MapErpEndpoints();
app.MapIntegrationEndpoints();
app.MapIntegrationWebhookEndpoints();
app.MapFactoryEventSpineEndpoints();
app.MapDigitalWorkforceEndpoints();
app.MapDigitalWorkforceExecutionEndpoints();
app.MapDigitalWorkforceWorksEndpoints();
app.MapProcurementEndpoints();
app.MapEdgeEndpoints();
app.MapIotPlcEndpoints();
app.MapIotPlcIntelligenceEndpoints();
app.MapOperationalRiskEndpoints();
app.MapAiTaskEndpoints();
app.MapAiTaskOperationsEndpoints();
app.MapAiTaskClosureEndpoints();
app.MapAiTaskAutomationEndpoints();
app.MapAiTaskPlaybookEndpoints();
app.MapAiTaskDecisionIntelligenceEndpoints();
app.MapAiTaskDeliveryEndpoints();
app.MapAiTaskSecureAccessEndpoints();
app.MapWhatsAppWebhookEndpoints();
app.MapTeamsEndpoints();
app.MapTeamsApprovalEndpoints();
app.MapTeamsHandoverEndpoints();
app.MapShiftHandoverEndpoints();
app.MapShiftOperationsEndpoints();
app.MapFactoryOrchestrationEndpoints();
app.MapFactoryDecisionCenterEndpoints();
app.MapFactoryIntelligenceEndpoints();
app.MapMixedProductionEndpoints();
app.MapThroughputIntelligenceEndpoints();
app.MapContextKnowledgeEndpoints();
app.MapModelGatewayEndpoints();
app.MapThroughputOptimizerEndpoints();
app.MapScenarioRecoveryEndpoints();
app.MapFactoryCopilotEndpoints();
app.MapContinuousOptimizationEndpoints();
app.MapIntelligenceRuntimeEndpoints();
app.MapOperationalValueEndpoints();
app.MapRealFactoryPilotEndpoints();
app.MapBulkDataOnboardingEndpoints();
app.MapFactoryOutcomeExecutionEndpoints();
app.MapProductionManagerEndpoints();
app.MapFactoryLearningEndpoints();
app.MapAdaptivePlanningEndpoints();
app.MapFactoryNetworkEndpoints();
app.MapEnterpriseReliabilityEndpoints();
app.MapGoldenDemoP1Endpoints();
app.MapGoldenDemoP2Endpoints();
app.MapGoldenDemoP2BEndpoints();
app.MapGoldenDemoP3Endpoints();
app.MapGoldenDemoCustomsFileEndpoints();
app.MapTeamsWorkInboxEndpoints();
app.MapTeamsDirectMessageEndpoints();
app.MapTeamsNotificationEndpoints();
app.MapMarketRatesEndpoints();
app.MapCommercialIntelligenceEndpoints();
app.MapCommercialSalesPriceEndpoints();
app.MapCommercialQuotationRiskEndpoints();
app.MapCommercialProfitabilityEndpoints();
app.MapCommercialProfitabilityAlertEndpoints();
app.MapCommercialPricingScenarioEndpoints();

app.MapExportEndpoints();
app.MapExport3LEndpoints();
app.MapExport4LEndpoints();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = static _ => false,
    }).AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/internal/tenant-context", (
        HttpContext httpContext,
        ICurrentIdentityAccessor currentIdentity,
        ITenantContextAccessor tenantContext) => new
    {
        authenticated = httpContext.User.Identity?.IsAuthenticated == true,
        identityResolved = currentIdentity.IsResolved,
        tenantContextAvailable = tenantContext.IsAvailable,
        identitySubjectId = currentIdentity.Current?.IdentitySubjectId,
        tenantId = tenantContext.Current?.TenantId,
        membershipId = tenantContext.Current?.MembershipId,
    }).RequireAuthorization(PermissionPolicies.TenantContextRead);

    app.MapGet("/internal/authorization-context", (
        HttpContext httpContext,
        IAuthorizationContext authorizationContext) => new
    {
        authenticated = httpContext.User.Identity?.IsAuthenticated == true,
        identitySubjectId = authorizationContext.Current?.IdentitySubjectId,
        tenantId = authorizationContext.Current?.TenantId,
        membershipId = authorizationContext.Current?.MembershipId,
    }).RequireAuthorization(PermissionPolicies.AuthorizationDiagnosticsRead);

    app.MapGet("/internal/permission-check/{permissionKey}", async (
        string permissionKey,
        IAuthorizationContext authorizationContext,
        IPermissionEvaluator evaluator,
        CancellationToken cancellationToken) =>
    {
        var current = authorizationContext.Current!;
        var result = await evaluator.EvaluateAsync(
            current.IdentitySubjectId,
            current.TenantId,
            current.MembershipId,
            permissionKey,
            cancellationToken);
        return new
        {
            authenticated = true,
            identitySubjectId = current.IdentitySubjectId,
            tenantId = current.TenantId,
            membershipId = current.MembershipId,
            requestedPermissionKey = permissionKey,
            granted = result.Granted,
        };
    }).RequireAuthorization(PermissionPolicies.AuthorizationDiagnosticsRead);

    app.MapGet("/internal/audit/recent", async (
        IAuditReader auditReader,
        CancellationToken cancellationToken) =>
        await auditReader.ReadRecentAsync(25, cancellationToken))
        .RequireAuthorization(PermissionPolicies.SystemAuditRead);

    app.MapPost("/internal/ai-decisions", async (
        AiDecisionWriteRequest request,
        IAiDecisionLedgerWriter writer,
        CancellationToken cancellationToken) =>
    {
        var result = await writer.WriteAsync(request, cancellationToken);
        return result.Succeeded
            ? Results.Created($"/internal/ai-decisions/{result.DecisionRecordId}", result)
            : Results.BadRequest(new { failureCode = result.FailureCode });
    }).RequireAuthorization(PermissionPolicies.SystemAiDecisionWrite);

    app.MapGet("/internal/ai-decisions/recent", async (
        IAiDecisionLedgerReader reader,
        CancellationToken cancellationToken) =>
        await reader.ReadRecentAsync(25, cancellationToken))
        .RequireAuthorization(PermissionPolicies.SystemAiDecisionRead);

    app.MapPost("/internal/outbox", async (
        OutboxProbeRequest request,
        IOutboxDiagnosticService outbox,
        CancellationToken cancellationToken) =>
    {
        var result = await outbox.AppendProbeAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { failureCode = result.FailureCode });
        }

        return result.Duplicate
            ? Results.Ok(result)
            : Results.Created($"/internal/outbox/{result.MessageId}", result);
    }).RequireAuthorization(PermissionPolicies.SystemOutboxWrite);

    app.MapGet("/internal/outbox/recent", async (
        IOutboxReader reader,
        CancellationToken cancellationToken) =>
        await reader.ReadRecentAsync(25, cancellationToken))
        .RequireAuthorization(PermissionPolicies.SystemOutboxRead);
}

app.Run();

static void ValidateConfiguration(IConfiguration configuration)
{
    if (string.IsNullOrWhiteSpace(configuration["Application:Name"]))
    {
        throw new InvalidOperationException("Application:Name must be configured.");
    }
}

public partial class Program;
