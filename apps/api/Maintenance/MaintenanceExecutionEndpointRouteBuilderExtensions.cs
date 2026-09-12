using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Maintenance;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Maintenance;

public static class MaintenanceExecutionEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMaintenanceExecutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/work-orders/{workOrderId:guid}/notes", ListWorkNotesAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/notes", CreateWorkNoteAsync);
        group.MapGet("/work-orders/{workOrderId:guid}/timeline", GetWorkOrderTimelineAsync);
        group.MapGet("/work-orders/{workOrderId:guid}/parts", ListPartUsagesAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/parts", CreatePartUsageAsync);
        group.MapGet("/work-orders/{workOrderId:guid}/cost-summary", GetCostSummaryAsync);

        group.MapGet("/preventive-dues", ListPreventiveDuesAsync);
        group.MapGet("/preventive-dues/{dueId:guid}", GetPreventiveDueAsync);
        group.MapPost("/preventive-plans/{planId:guid}/generate-next-due", GenerateNextDueAsync);
        group.MapPost("/preventive-dues/{dueId:guid}/create-work-order", CreateWorkOrderFromDueAsync);

        group.MapGet("/assets/{assetReference}/history", GetAssetHistoryAsync);

        return group;
    }

    private static async Task<IResult> ListWorkNotesAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkNoteRepository noteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var notes = await noteRepository.ListByWorkOrderAsync(context, workOrderId, cancellationToken);
        return TypedResults.Ok(notes.Select(MaintenanceApiMapper.MapWorkNote).ToList());
    }

    private static async Task<IResult> CreateWorkNoteAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMaintenanceWorkNoteApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkNoteRepository noteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await noteRepository.CreateAsync(
            context,
            new CreateMaintenanceWorkNoteCommand(workOrderId, body.NoteType, body.Text, body.IsImportant),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> GetWorkOrderTimelineAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceTimelineRepository timelineRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var timeline = await timelineRepository.GetWorkOrderTimelineAsync(context, workOrderId, cancellationToken);
        return TypedResults.Ok(timeline.Select(MaintenanceApiMapper.MapTimelineEntry).ToList());
    }

    private static async Task<IResult> ListPartUsagesAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePartUsageRepository partUsageRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var parts = await partUsageRepository.ListByWorkOrderAsync(context, workOrderId, cancellationToken);
        return TypedResults.Ok(parts.Select(MaintenanceApiMapper.MapPartUsage).ToList());
    }

    private static async Task<IResult> CreatePartUsageAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMaintenancePartUsageApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePartUsageRepository partUsageRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await partUsageRepository.CreateAsync(
            context,
            new CreateMaintenancePartUsageCommand(
                workOrderId,
                body.PartReference,
                body.PartName,
                body.Quantity,
                body.Unit,
                body.SourceSystem,
                body.SourceReference,
                body.EstimatedUnitCost,
                body.ActualUnitCost),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> GetCostSummaryAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceCostSummaryRepository costSummaryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var summary = await costSummaryRepository.GetByWorkOrderAsync(context, workOrderId, cancellationToken);
        if (summary is null)
        {
            return TypedResults.Problem(
                title: "Maintenance work order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaintenanceEndpointHelpers.FailureExt(MaintenanceApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaintenanceApiMapper.MapCostSummary(summary));
    }

    private static async Task<IResult> ListPreventiveDuesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenanceDueRepository dueRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var dues = await dueRepository.ListAsync(context, status, cancellationToken);
        return TypedResults.Ok(dues.Select(MaintenanceApiMapper.MapPreventiveDueSummary).ToList());
    }

    private static async Task<IResult> GetPreventiveDueAsync(
        Guid dueId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenanceDueRepository dueRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var due = await dueRepository.GetByIdAsync(context, dueId, cancellationToken);
        if (due is null)
        {
            return TypedResults.Problem(
                title: "Preventive maintenance due not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaintenanceEndpointHelpers.FailureExt(MaintenanceApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaintenanceApiMapper.MapPreventiveDueDetail(due));
    }

    private static async Task<IResult> GenerateNextDueAsync(
        Guid planId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenanceDueRepository dueRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await dueRepository.GenerateNextDueAsync(context, planId, cancellationToken);
        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> CreateWorkOrderFromDueAsync(
        Guid dueId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateWorkOrderFromDueApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenanceDueRepository dueRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await dueRepository.CreateWorkOrderFromDueAsync(context, dueId, body.ExpectedVersion, cancellationToken);
        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> GetAssetHistoryAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceAssetHistoryRepository assetHistoryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var history = await assetHistoryRepository.GetByAssetReferenceAsync(context, assetReference, cancellationToken);
        if (history is null)
        {
            return TypedResults.Problem(
                title: "Asset maintenance history not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaintenanceEndpointHelpers.FailureExt(MaintenanceApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaintenanceApiMapper.MapAssetHistory(history));
    }
}

internal sealed record CreateMaintenanceWorkNoteApiRequest(string NoteType, string Text, bool IsImportant);

internal sealed record CreateMaintenancePartUsageApiRequest(
    string PartReference,
    string? PartName,
    decimal Quantity,
    string Unit,
    string? SourceSystem,
    string? SourceReference,
    decimal? EstimatedUnitCost,
    decimal? ActualUnitCost);

internal sealed record CreateWorkOrderFromDueApiRequest(uint ExpectedVersion);

public static class MaintenanceExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSMaintenanceExecution(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceWorkNoteRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceWorkNoteRepository()
                : new MaintenanceWorkNoteRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IMaintenanceTimelineRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceTimelineRepository()
                : new MaintenanceTimelineRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IPreventiveMaintenanceDueRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailablePreventiveMaintenanceDueRepository()
                : new PreventiveMaintenanceDueRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IMaintenancePartUsageRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenancePartUsageRepository()
                : new MaintenancePartUsageRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IMaintenanceCostSummaryRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceCostSummaryRepository()
                : new MaintenanceCostSummaryRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IMaintenanceAssetHistoryRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceAssetHistoryRepository()
                : new MaintenanceAssetHistoryRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
