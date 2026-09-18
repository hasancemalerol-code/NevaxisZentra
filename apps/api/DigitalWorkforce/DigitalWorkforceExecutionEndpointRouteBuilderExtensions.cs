using Microsoft.Extensions.Options;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks.Delivery;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.DigitalWorkforce;

public static class DigitalWorkforceExecutionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapDigitalWorkforceExecutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/digital-workforce/executions").RequireAuthorization();

        group.MapPost("/enqueue", EnqueueAsync);
        group.MapPost("/process", ProcessBatchAsync);
        group.MapPost("/reclaim", ReclaimLeasesAsync);
        group.MapGet("/", ListExecutionsAsync);
        group.MapGet("/workers/{workerId:guid}/health", GetWorkerHealthAsync);
        group.MapGet("/cases/{caseId:guid}/coordinator", GetCoordinatorSnapshotAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraDigitalWorkforceExecutionRuntime(this IServiceCollection services)
    {
        services.AddScoped<IDigitalWorkforceExecutionRuntimeService, DigitalWorkforceExecutionRuntimeService>();
        services.AddScoped<IDigitalWorkforceCoordinatorRuntime, DigitalWorkforceCoordinatorRuntime>();
        services.Configure<DigitalWorkforceExecutionOptions>(options => options.WorkerEnabled = true);
        services.AddHostedService<DigitalWorkforceExecutionBackgroundService>();
        return services;
    }

    private static async Task<IResult> EnqueueAsync(
        EnqueueDigitalWorkExecutionRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        IDigitalWorkforceExecutionRuntimeService executionRuntime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var result = await executionRuntime.EnqueueAsync(
                request,
                authorizationContext.Current!.IdentitySubjectId.ToString(),
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ProcessBatchAsync(
        int? batchSize,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceExecutionRuntimeService executionRuntime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await executionRuntime.ProcessBatchAsync(
            Environment.MachineName,
            batchSize ?? 25,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ReclaimLeasesAsync(
        int? batchSize,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceExecutionRuntimeService executionRuntime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await executionRuntime.ReclaimExpiredLeasesAsync(
            Environment.MachineName,
            batchSize ?? 25,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ListExecutionsAsync(
        Guid? caseId,
        Guid? workerId,
        string? status,
        int? limit,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceExecutionRuntimeService executionRuntime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var rows = await executionRuntime.ListExecutionsAsync(caseId, workerId, status, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetWorkerHealthAsync(
        Guid workerId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceExecutionRuntimeService executionRuntime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var health = await executionRuntime.GetWorkerHealthAsync(workerId, cancellationToken);
            return TypedResults.Ok(health);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetCoordinatorSnapshotAsync(
        Guid caseId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IDigitalWorkforceCoordinatorRuntime coordinatorRuntime,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await DigitalWorkforceEndpointHelpers.ResolveAccessAsync(
            facilityId, DigitalWorkforcePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var snapshot = await coordinatorRuntime.EvaluateCaseAsync(caseId, cancellationToken);
            return TypedResults.Ok(snapshot);
        }
        catch (DigitalWorkforceException ex)
        {
            return DigitalWorkforceEndpointHelpers.MapException(ex);
        }
    }
}

public sealed class DigitalWorkforceExecutionOptions
{
    public bool WorkerEnabled { get; set; } = true;
    public int BatchSize { get; set; } = 25;
}

internal sealed class DigitalWorkforceExecutionBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<DigitalWorkforceExecutionOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetService<NevaxisZentra.Persistence.NevaxisZentraDbContext>();
                if (db is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    continue;
                }

                var discoverer = scope.ServiceProvider.GetRequiredService<IAiTaskDeliveryWorkTenantDiscoverer>();
                var tenantAccessor = scope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
                var executionRuntime = scope.ServiceProvider.GetRequiredService<IDigitalWorkforceExecutionRuntimeService>();
                var contexts = await discoverer.DiscoverWorkerContextsAsync(stoppingToken);

                foreach (var tenantContext in contexts)
                {
                    tenantAccessor.Set(tenantContext);
                    await executionRuntime.ReclaimExpiredLeasesAsync(
                        Environment.MachineName, options.Value.BatchSize, stoppingToken);
                    await executionRuntime.ProcessBatchAsync(
                        Environment.MachineName, options.Value.BatchSize, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Runtime failures are surfaced via execution records and worker health.
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
