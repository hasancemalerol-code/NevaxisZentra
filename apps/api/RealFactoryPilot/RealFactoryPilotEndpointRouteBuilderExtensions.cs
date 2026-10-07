using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.RealFactoryPilot;

namespace NevaxisZentra.Api.RealFactoryPilot;

public static class RealFactoryPilotEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRealFactoryPilotEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/real-factory-pilot").RequireAuthorization();

        // Read-only: never creates data. A tenant that never initialized the synthetic pilot gets an empty answer.
        group.MapGet("/current", GetCurrentAsync);
        // Mutating, permission-gated and audited. Synthetic fixtures exist only in Development/Demo/Presentation.
        group.MapPost("/initialize", InitializeAsync);
        group.MapGet("/runs/{pilotRunId:guid}/workspace", GetWorkspaceAsync);
        group.MapPost("/dev/golden-pilot", RunGoldenPilotAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraRealFactoryPilot(this IServiceCollection services)
    {
        services.AddScoped<IRealFactoryPilotService, RealFactoryPilotService>();
        return services;
    }

    /// <summary>Deployment channels where the synthetic pilot fixture may be created.</summary>
    public static bool SyntheticInitializationAllowed(IWebHostEnvironment environment, IConfiguration configuration) =>
        SyntheticPilotInitializationPolicy.IsAllowed(
            environment.EnvironmentName,
            configuration["NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL"]);

    private static async Task<IResult> GetCurrentAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var workspace = await service.GetCurrentWorkspaceAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(new
        {
            workspace,
            syntheticInitializationAvailable = SyntheticInitializationAllowed(environment, configuration),
        });
    }

    private static async Task<IResult> InitializeAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (!SyntheticInitializationAllowed(environment, configuration))
        {
            // Production never auto-seeds or offers synthetic pilot data in a customer tenant.
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.InitializePilotAsync(
                access.FacilityId,
                access.TenantId,
                access.IdentitySubjectId.ToString(),
                new RealFactoryPilotInitializationAudit(
                    access.IdentitySubjectId,
                    access.MembershipId,
                    RealFactoryPilotPermissionKeys.Manage,
                    correlationContext.IsAvailable ? correlationContext.CorrelationId : Guid.NewGuid(),
                    correlationContext.RequestId),
                cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (RealFactoryPilotException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid pilotRunId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, pilotRunId, cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (RealFactoryPilotException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> RunGoldenPilotAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.RunGoldenPilotAsync(
                access.FacilityId, access.TenantId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (RealFactoryPilotException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, TypedResults.Forbid());
        }

        return (accessResult.Access, null);
    }

    private static IResult MapException(RealFactoryPilotException ex) =>
        ex.FailureCode switch
        {
            RealFactoryPilotFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
        };
}

/// <summary>
/// Synthetic pilot data (sample line, PLC/MES connectors, a SYNTHETIC_PILOT_PASS run) may only be created in
/// Development/Testing hosts or DEMO/PRESENTATION deployment channels. Production, an unset channel and any unknown
/// channel fail closed.
/// </summary>
public static class SyntheticPilotInitializationPolicy
{
    public static bool IsAllowed(string? environmentName, string? deploymentChannel)
    {
        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var channel = deploymentChannel?.Trim();
        return string.Equals(channel, "DEMO", StringComparison.OrdinalIgnoreCase)
               || string.Equals(channel, "PRESENTATION", StringComparison.OrdinalIgnoreCase);
    }
}
