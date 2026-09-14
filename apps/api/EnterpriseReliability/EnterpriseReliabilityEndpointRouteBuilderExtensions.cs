using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.EnterpriseReliability;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.EnterpriseReliability;

public static class EnterpriseReliabilityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEnterpriseReliabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/system-status").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/metrics", GetMetricsAsync);
        group.MapGet("/diagnostics", ExportDiagnosticsAsync);
        group.MapPost("/connectors/outcome", RecordConnectorOutcomeAsync);
        group.MapPost("/edge/heartbeat", RecordEdgeHeartbeatAsync);
        group.MapPost("/reconcile", ReconcileAsync);
        group.MapPost("/quarantine/replay", ReplayQuarantineAsync);
        group.MapPost("/backup/verify", VerifyBackupAsync);

        endpoints.MapGet("/api/system-status/version", (IConfiguration configuration) => TypedResults.Ok(new
        {
            application = configuration["Application:Name"] ?? "NevaxisFactOS API",
            version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            buildSha = configuration["Application:BuildSha"]
                ?? Environment.GetEnvironmentVariable("NEVAXIS_FACTOS_BUILD_SHA")
                ?? "unknown",
            generatedAtUtc = DateTimeOffset.UtcNow,
        })).AllowAnonymous();
        endpoints.MapGet("/health", () => Results.Redirect("/health/live")).AllowAnonymous();

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSEnterpriseReliabilityApi(this IServiceCollection services)
    {
        services.AddNevaxisFactOSEnterpriseReliability();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        var snapshot = await tenantDatabaseSession.ExecuteReadAsync(
            ct => service.GetSystemStatusAsync(tenantContext.Current!.TenantId, facilityId, ct),
            cancellationToken);
        return TypedResults.Ok(MapSnapshot(snapshot));
    }

    private static async Task<IResult> GetMetricsAsync(
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        var metrics = await tenantDatabaseSession.ExecuteReadAsync(
            ct => service.GetMetricsAsync(tenantContext.Current!.TenantId, ct),
            cancellationToken);
        return TypedResults.Ok(metrics);
    }

    private static async Task<IResult> ExportDiagnosticsAsync(
        [FromQuery] Guid? facilityId,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        var bundle = await tenantDatabaseSession.ExecuteReadAsync(
            ct => service.ExportDiagnosticBundleAsync(tenantContext.Current!.TenantId, facilityId, ct),
            cancellationToken);
        return TypedResults.Ok(bundle);
    }

    private static async Task<IResult> RecordConnectorOutcomeAsync(
        [FromBody] RecordConnectorOutcomeRequest request,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await service.RecordConnectorOutcomeAsync(tenantContext.Current!.TenantId, request, ct);
                return true;
            },
            cancellationToken);
        return TypedResults.Ok(new { recorded = true });
    }

    private static async Task<IResult> RecordEdgeHeartbeatAsync(
        [FromBody] EdgeAgentHeartbeatRequest request,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                await service.RecordEdgeHeartbeatAsync(tenantContext.Current!.TenantId, request, ct);
                return true;
            },
            cancellationToken);
        return TypedResults.Ok(new { recorded = true });
    }

    private static async Task<IResult> ReconcileAsync(
        [FromBody] ReconcileRecoveryRequest request,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var correlationHeader = httpContext.Request.Headers["X-NevaxisFactOS-Correlation-Id"].FirstOrDefault();
            Guid? correlationId = Guid.TryParse(correlationHeader, out var parsed) ? parsed : null;
            var run = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.ReconcileAsync(
                    tenantContext.Current!.TenantId,
                    request,
                    subject,
                    correlationId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(run);
        }
        catch (EnterpriseReliabilityException ex)
        {
            return MapException(ex, service);
        }
    }

    private static async Task<IResult> ReplayQuarantineAsync(
        [FromBody] ReplayQuarantineRequest request,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var correlationHeader = httpContext.Request.Headers["X-NevaxisFactOS-Correlation-Id"].FirstOrDefault();
            Guid? correlationId = Guid.TryParse(correlationHeader, out var parsed) ? parsed : null;
            var audit = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.ReplayQuarantineAsync(
                    tenantContext.Current!.TenantId,
                    request,
                    subject,
                    correlationId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(audit);
        }
        catch (EnterpriseReliabilityException ex)
        {
            return MapException(ex, service);
        }
    }

    private static async Task<IResult> VerifyBackupAsync(
        [FromBody] BackupVerificationRequest request,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IEnterpriseReliabilityService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsAvailable)
        {
            return Results.Unauthorized();
        }

        var subject = httpContext.User.Identity?.Name ?? "system";
        var run = await tenantDatabaseSession.ExecuteWriteAsync(
            ct => service.VerifyBackupAsync(tenantContext.Current!.TenantId, request, subject, ct),
            cancellationToken);
        return TypedResults.Ok(run);
    }

    private static IResult MapException(EnterpriseReliabilityException ex, IEnterpriseReliabilityService service)
    {
        var customer = service.MapCustomerError(ex.FailureCode, ex.Message);
        return Results.Conflict(new
        {
            failureCode = ex.FailureCode,
            customer,
        });
    }

    private static object MapSnapshot(SystemStatusSnapshot snapshot) => new
    {
        snapshot.OperatingMode,
        snapshot.ApplicationVersion,
        snapshot.BuildSha,
        snapshot.GeneratedAtUtc,
        snapshot.ShowsFalseHealth,
        components = snapshot.Components,
        integrations = snapshot.Integrations,
        edgeAgents = snapshot.EdgeAgents,
        alerts = snapshot.Alerts,
        dependencies = snapshot.Dependencies,
    };
}
