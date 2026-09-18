using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NevaxisFactOS.Configuration;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Tenancy;
using NevaxisFactOS.Api.Tenancy;
using NevaxisFactOS.Api.Authentication;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using NevaxisFactOS.Api.Audit;
using NevaxisFactOS.Api.Development;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Api.Export;
using NevaxisFactOS.Api.Production;
using NevaxisFactOS.Api.ProductionPlanning;
using NevaxisFactOS.Api.Maintenance;
using NevaxisFactOS.Api.Quality;
using NevaxisFactOS.Api.Material;
using NevaxisFactOS.Api.Mold;
using NevaxisFactOS.Api.ProcessOee;
using NevaxisFactOS.Api.Procurement;
using NevaxisFactOS.Api.ShiftWorkforce;
using NevaxisFactOS.Api.ShiftAssignment;
using NevaxisFactOS.Api.ShiftRisk;
using NevaxisFactOS.Api.Energy;
using NevaxisFactOS.Api.SaasCommercial;
using NevaxisFactOS.Api.SaasIdentity;
using NevaxisFactOS.Api.SaasSubscription;
using NevaxisFactOS.Api.SaasBilling;
using NevaxisFactOS.Api.AdminAccess;
using NevaxisFactOS.Api.Erp;
using NevaxisFactOS.Api.Integration;
using NevaxisFactOS.Api.DigitalWorkforce;
using NevaxisFactOS.Api.Edge;
using NevaxisFactOS.Api.IotPlc;
using NevaxisFactOS.Api.OperationalRisk;
using NevaxisFactOS.Api.CommercialIntelligence;
using NevaxisFactOS.Api.AiTasks;
using NevaxisFactOS.Api.Teams;
using NevaxisFactOS.Api.ShiftHandover;
using NevaxisFactOS.Api.ShiftOperations;
using NevaxisFactOS.Api.FactoryOrchestration;
using NevaxisFactOS.Api.FactoryDecisionCenter;
using NevaxisFactOS.Api.FactoryIntelligence;
using NevaxisFactOS.Api.MixedProduction;
using NevaxisFactOS.Api.ThroughputIntelligence;
using NevaxisFactOS.Api.ContextKnowledge;
using NevaxisFactOS.Api.ModelGateway;
using NevaxisFactOS.Api.ThroughputOptimizer;
using NevaxisFactOS.Api.RealFactoryPilot;
using NevaxisFactOS.Api.BulkDataOnboarding;
using NevaxisFactOS.Api.FactoryOutcomeExecution;
using NevaxisFactOS.Api.ProductionManager;
using NevaxisFactOS.Api.FactoryLearning;
using NevaxisFactOS.Api.AdaptiveProductionPlanning;
using NevaxisFactOS.Api.FactoryNetworkOptimization;
using NevaxisFactOS.Api.EnterpriseReliability;
using NevaxisFactOS.Api.GoldenDemoP1;
using NevaxisFactOS.Api.GoldenDemoP2;
using NevaxisFactOS.Api.GoldenDemoP2B;
using NevaxisFactOS.Api.GoldenDemoP3;
using NevaxisFactOS.Api.GoldenDemoCustomsFile;
using NevaxisFactOS.Api.MarketRates;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.AiDecisions;
using NevaxisFactOS.Persistence.Outbox;
using NevaxisFactOS.Persistence.Development;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Application:Name"] ??= "NevaxisFactOS API";
ValidateConfiguration(builder.Configuration);

builder.Services.AddSingleton<DevelopmentPilotBootstrapState>();
builder.Services.AddHealthChecks()
    .AddCheck<DevelopmentPilotBootstrapHealthCheck>("development-pilot-bootstrap");
builder.Services.AddInfrastructureOptions(builder.Configuration);
builder.Services.AddNevaxisFactOSPersistence(builder.Configuration, builder.Environment);
builder.Services.AddNevaxisFactOSAuthentication(
    builder.Configuration,
    builder.Environment);
builder.Services.AddAuthorization(PermissionPolicies.AddInfrastructurePolicies);
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddNevaxisFactOSFacilityAccess();
builder.Services.AddNevaxisFactOSProductionPlanning();
builder.Services.AddNevaxisFactOSProductionExecution();
builder.Services.AddNevaxisFactOSQuality();
builder.Services.AddNevaxisFactOSMold();
builder.Services.AddNevaxisFactOSMaterial();
builder.Services.AddNevaxisFactOSProcessOee();
builder.Services.AddNevaxisFactOSShiftWorkforce();
builder.Services.AddNevaxisFactOSShiftAssignment();
builder.Services.AddNevaxisFactOSShiftRisk();
builder.Services.AddNevaxisFactOSEnergy();
builder.Services.AddNevaxisFactOSEnergyIntelligence();
builder.Services.AddNevaxisFactOSSaasCommercial();
builder.Services.AddNevaxisFactOSSaasSubscriptionApi();
builder.Services.AddNevaxisFactOSSaasBillingApi(builder.Configuration);
builder.Services.AddNevaxisFactOSSaasIdentity();
builder.Services.AddNevaxisFactOSAdminAccess();
builder.Services.AddNevaxisFactOSMaintenance();
builder.Services.AddNevaxisFactOSExport();
builder.Services.AddNevaxisFactOSErp();
builder.Services.AddNevaxisFactOSIntegration();
builder.Services.AddNevaxisFactOSFactoryEventSpine();
builder.Services.AddNevaxisFactOSDigitalWorkforce();
builder.Services.AddNevaxisFactOSProcurement();
builder.Services.AddNevaxisFactOSEdge();
builder.Services.AddNevaxisFactOSIotPlc();
builder.Services.AddNevaxisFactOSIotPlcIntelligence();
builder.Services.AddNevaxisFactOSOperationalRisk();
builder.Services.AddNevaxisFactOSAiTasks();
builder.Services.AddNevaxisFactOSAiTaskDelivery(builder.Configuration);
builder.Services.AddAiTaskSecureAccessRateLimiting();
builder.Services.AddNevaxisFactOSTeams();
builder.Services.AddNevaxisFactOSShiftHandover();
builder.Services.AddNevaxisFactOSShiftOperations();
builder.Services.AddNevaxisFactOSFactoryOrchestration();
builder.Services.AddNevaxisFactOSFactoryDecisionCenter();
builder.Services.AddNevaxisFactOSFactoryIntelligence();
builder.Services.AddNevaxisFactOSMixedProduction();
builder.Services.AddNevaxisFactOSThroughputIntelligence();
builder.Services.AddNevaxisFactOSContextKnowledge();
builder.Services.AddNevaxisFactOSModelGateway();
builder.Services.AddNevaxisFactOSThroughputOptimizer();
builder.Services.AddNevaxisFactOSRealFactoryPilot();
builder.Services.AddNevaxisFactOSBulkDataOnboardingApi();
builder.Services.AddNevaxisFactOSProductionManager();
builder.Services.AddNevaxisFactOSFactoryLearning();
builder.Services.AddNevaxisFactOSAdaptivePlanning();
builder.Services.AddNevaxisFactOSFactoryNetwork();
builder.Services.AddNevaxisFactOSEnterpriseReliabilityApi();
builder.Services.AddNevaxisFactOSGoldenDemoP1Api();
builder.Services.AddNevaxisFactOSGoldenDemoP2Api();
builder.Services.AddNevaxisFactOSGoldenDemoP2BApi();
builder.Services.AddNevaxisFactOSGoldenDemoP3Api();
builder.Services.AddNevaxisFactOSGoldenDemoCustomsFileApi();
builder.Services.AddNevaxisFactOSMarketRates(builder.Configuration);
builder.Services.AddNevaxisFactOSCommercialIntelligence();
builder.Services.AddNevaxisFactOSCommercialSalesPrice();
builder.Services.AddNevaxisFactOSCommercialQuotationRisk();
builder.Services.AddNevaxisFactOSCommercialProfitability();
builder.Services.AddNevaxisFactOSCommercialProfitabilityAlerts();
builder.Services.AddNevaxisFactOSCommercialPricingScenarios();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, UniformAuthorizationResultHandler>();

NevaxisFactOS.Persistence.Development.DevelopmentPilotServiceCollectionExtensions
    .ValidateDevelopmentPilotProductionGuard(
        builder.Configuration,
        builder.Environment);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddNevaxisFactOSDevelopmentPilot(
        builder.Configuration,
        builder.Environment);
    builder.Services.AddNevaxisFactOSDevelopmentPilotHostedBootstrap(
        builder.Environment);
    builder.Services.AddSingleton<
        NevaxisFactOS.Persistence.ProductionPlanning.IProductionPlanTransferAdapter,
        NevaxisFactOS.Persistence.ProductionPlanning.FakeProductionPlanTransferAdapter>();
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
