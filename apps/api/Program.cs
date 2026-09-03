using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CrevaFactOS.Configuration;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Api.Tenancy;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using CrevaFactOS.Api.Audit;
using CrevaFactOS.Api.Development;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Api.Export;
using CrevaFactOS.Api.Production;
using CrevaFactOS.Api.ProductionPlanning;
using CrevaFactOS.Api.Maintenance;
using CrevaFactOS.Api.Quality;
using CrevaFactOS.Api.Material;
using CrevaFactOS.Api.Mold;
using CrevaFactOS.Api.ProcessOee;
using CrevaFactOS.Api.Procurement;
using CrevaFactOS.Api.ShiftWorkforce;
using CrevaFactOS.Api.ShiftAssignment;
using CrevaFactOS.Api.ShiftRisk;
using CrevaFactOS.Api.Energy;
using CrevaFactOS.Api.SaasCommercial;
using CrevaFactOS.Api.SaasIdentity;
using CrevaFactOS.Api.SaasSubscription;
using CrevaFactOS.Api.SaasBilling;
using CrevaFactOS.Api.AdminAccess;
using CrevaFactOS.Api.Erp;
using CrevaFactOS.Api.Edge;
using CrevaFactOS.Api.IotPlc;
using CrevaFactOS.Api.OperationalRisk;
using CrevaFactOS.Api.CommercialIntelligence;
using CrevaFactOS.Api.AiTasks;
using CrevaFactOS.Api.Teams;
using CrevaFactOS.Api.ShiftHandover;
using CrevaFactOS.Api.MarketRates;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.AiDecisions;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Development;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Application:Name"] ??= "CrevaFactOS API";
ValidateConfiguration(builder.Configuration);

builder.Services.AddSingleton<DevelopmentPilotBootstrapState>();
builder.Services.AddHealthChecks()
    .AddCheck<DevelopmentPilotBootstrapHealthCheck>("development-pilot-bootstrap");
builder.Services.AddInfrastructureOptions(builder.Configuration);
builder.Services.AddCrevaFactOSPersistence(builder.Configuration, builder.Environment);
builder.Services.AddCrevaFactOSAuthentication(
    builder.Configuration,
    builder.Environment);
builder.Services.AddAuthorization(PermissionPolicies.AddInfrastructurePolicies);
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddCrevaFactOSFacilityAccess();
builder.Services.AddCrevaFactOSProductionPlanning();
builder.Services.AddCrevaFactOSProductionExecution();
builder.Services.AddCrevaFactOSQuality();
builder.Services.AddCrevaFactOSMold();
builder.Services.AddCrevaFactOSMaterial();
builder.Services.AddCrevaFactOSProcessOee();
builder.Services.AddCrevaFactOSShiftWorkforce();
builder.Services.AddCrevaFactOSShiftAssignment();
builder.Services.AddCrevaFactOSShiftRisk();
builder.Services.AddCrevaFactOSEnergy();
builder.Services.AddCrevaFactOSEnergyIntelligence();
builder.Services.AddCrevaFactOSSaasCommercial();
builder.Services.AddCrevaFactOSSaasSubscriptionApi();
builder.Services.AddCrevaFactOSSaasBillingApi(builder.Configuration);
builder.Services.AddCrevaFactOSSaasIdentity();
builder.Services.AddCrevaFactOSAdminAccess();
builder.Services.AddCrevaFactOSMaintenance();
builder.Services.AddCrevaFactOSExport();
builder.Services.AddCrevaFactOSErp();
builder.Services.AddCrevaFactOSProcurement();
builder.Services.AddCrevaFactOSEdge();
builder.Services.AddCrevaFactOSIotPlc();
builder.Services.AddCrevaFactOSIotPlcIntelligence();
builder.Services.AddCrevaFactOSOperationalRisk();
builder.Services.AddCrevaFactOSAiTasks();
builder.Services.AddCrevaFactOSTeams();
builder.Services.AddCrevaFactOSShiftHandover();
builder.Services.AddCrevaFactOSMarketRates(builder.Configuration);
builder.Services.AddCrevaFactOSCommercialIntelligence();
builder.Services.AddCrevaFactOSCommercialSalesPrice();
builder.Services.AddCrevaFactOSCommercialQuotationRisk();
builder.Services.AddCrevaFactOSCommercialProfitability();
builder.Services.AddCrevaFactOSCommercialProfitabilityAlerts();
builder.Services.AddCrevaFactOSCommercialPricingScenarios();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, UniformAuthorizationResultHandler>();

CrevaFactOS.Persistence.Development.DevelopmentPilotServiceCollectionExtensions
    .ValidateDevelopmentPilotProductionGuard(
        builder.Configuration,
        builder.Environment);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCrevaFactOSDevelopmentPilot(
        builder.Configuration,
        builder.Environment);
    builder.Services.AddCrevaFactOSDevelopmentPilotHostedBootstrap(
        builder.Environment);
    builder.Services.AddSingleton<
        CrevaFactOS.Persistence.ProductionPlanning.IProductionPlanTransferAdapter,
        CrevaFactOS.Persistence.ProductionPlanning.FakeProductionPlanTransferAdapter>();
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
app.MapProcurementEndpoints();
app.MapEdgeEndpoints();
app.MapIotPlcEndpoints();
app.MapIotPlcIntelligenceEndpoints();
app.MapOperationalRiskEndpoints();
app.MapAiTaskEndpoints();
app.MapAiTaskOperationsEndpoints();
app.MapAiTaskClosureEndpoints();
app.MapAiTaskAutomationEndpoints();
app.MapTeamsEndpoints();
app.MapTeamsApprovalEndpoints();
app.MapTeamsHandoverEndpoints();
app.MapShiftHandoverEndpoints();
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
