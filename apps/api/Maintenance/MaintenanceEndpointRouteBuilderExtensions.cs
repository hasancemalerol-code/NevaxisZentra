using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Maintenance;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Maintenance;

public static class MaintenanceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/maintenance").RequireAuthorization();

        group.MapGet("/requests", ListRequestsAsync);
        group.MapPost("/requests", CreateRequestAsync);
        group.MapGet("/requests/{requestId:guid}", GetRequestAsync);
        group.MapPut("/requests/{requestId:guid}", UpdateRequestAsync);
        group.MapPost("/requests/{requestId:guid}/convert-to-work-order", ConvertRequestToWorkOrderAsync);

        group.MapGet("/work-orders", ListWorkOrdersAsync);
        group.MapPost("/work-orders", CreateWorkOrderAsync);
        group.MapGet("/work-orders/{workOrderId:guid}", GetWorkOrderAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/assign", AssignWorkOrderAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/start", StartWorkOrderAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/wait", WaitWorkOrderAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/resume", ResumeWorkOrderAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/complete", CompleteWorkOrderAsync);
        group.MapPost("/work-orders/{workOrderId:guid}/cancel", CancelWorkOrderAsync);

        group.MapGet("/preventive-plans", ListPreventivePlansAsync);
        group.MapPost("/preventive-plans", CreatePreventivePlanAsync);
        group.MapGet("/preventive-plans/{planId:guid}", GetPreventivePlanAsync);
        group.MapPut("/preventive-plans/{planId:guid}", UpdatePreventivePlanAsync);
        group.MapPost("/preventive-plans/{planId:guid}/create-work-order", CreateWorkOrderFromPlanAsync);

        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);

        group.MapMaintenanceExecutionEndpoints();

        return endpoints;
    }

    // ─────────────────────────── Requests ───────────────────────────

    private static async Task<IResult> ListRequestsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceRequestRepository requestRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var requests = await requestRepository.ListAsync(context, status, cancellationToken);
        return TypedResults.Ok(requests.Select(MaintenanceApiMapper.MapRequestSummary).ToList());
    }

    private static async Task<IResult> GetRequestAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceRequestRepository requestRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var request = await requestRepository.GetByIdAsync(context, requestId, cancellationToken);
        if (request is null)
        {
            return TypedResults.Problem(
                title: "Maintenance request not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaintenanceEndpointHelpers.FailureExt(MaintenanceApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaintenanceApiMapper.MapRequestDetail(request));
    }

    private static async Task<IResult> CreateRequestAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMaintenanceRequestApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceRequestRepository requestRepository,
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

        var result = await requestRepository.CreateAsync(
            context,
            new CreateMaintenanceRequestCommand(
                body.AssetType,
                body.AssetId,
                body.AssetReference,
                body.AssetName,
                body.ProductionOrderId,
                body.Type,
                body.Priority,
                body.Title,
                body.Description,
                body.Source),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateRequestAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateMaintenanceRequestApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceRequestRepository requestRepository,
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

        var result = await requestRepository.UpdateAsync(
            context,
            new UpdateMaintenanceRequestCommand(
                requestId,
                body.ExpectedVersion,
                body.Title,
                body.Description,
                body.Priority,
                body.Status),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ConvertRequestToWorkOrderAsync(
        Guid requestId,
        [FromQuery] Guid? facilityId,
        [FromBody] ConvertMaintenanceRequestApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceRequestRepository requestRepository,
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

        var result = await requestRepository.ConvertToWorkOrderAsync(
            context,
            new ConvertMaintenanceRequestCommand(
                requestId,
                body.ExpectedVersion,
                body.AssignedTechnicianSubjectId,
                body.PlannedStartUtc,
                body.PlannedEndUtc),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    // ─────────────────────────── Work Orders ───────────────────────────

    private static async Task<IResult> ListWorkOrdersAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var workOrders = await workOrderRepository.ListAsync(context, status, priority, cancellationToken);
        return TypedResults.Ok(workOrders.Select(MaintenanceApiMapper.MapWorkOrderSummary).ToList());
    }

    private static async Task<IResult> GetWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var workOrder = await workOrderRepository.GetByIdAsync(context, workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return TypedResults.Problem(
                title: "Maintenance work order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaintenanceEndpointHelpers.FailureExt(MaintenanceApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaintenanceApiMapper.MapWorkOrderDetail(workOrder));
    }

    private static async Task<IResult> CreateWorkOrderAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMaintenanceWorkOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
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

        var result = await workOrderRepository.CreateAsync(
            context,
            new CreateMaintenanceWorkOrderCommand(
                body.AssetType,
                body.AssetId,
                body.AssetReference,
                body.AssetName,
                body.ProductionOrderId,
                body.MaintenanceRequestId,
                body.Type,
                body.Priority,
                body.Title,
                body.Description,
                body.AssignedTechnicianSubjectId,
                body.PlannedStartUtc,
                body.PlannedEndUtc,
                body.InitialStatus),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> AssignWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] AssignMaintenanceWorkOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await workOrderRepository.AssignAsync(
            context,
            new AssignMaintenanceWorkOrderCommand(
                workOrderId,
                body.ExpectedVersion,
                body.AssignedTechnicianSubjectId),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> StartWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] MaintenanceWorkOrderLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await workOrderRepository.StartAsync(
            context,
            new MaintenanceWorkOrderLifecycleCommand(workOrderId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> WaitWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] MaintenanceWorkOrderLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await workOrderRepository.WaitAsync(
            context,
            new MaintenanceWorkOrderLifecycleCommand(workOrderId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ResumeWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] MaintenanceWorkOrderLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await workOrderRepository.ResumeAsync(
            context,
            new MaintenanceWorkOrderLifecycleCommand(workOrderId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CompleteWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteMaintenanceWorkOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await workOrderRepository.CompleteAsync(
            context,
            new CompleteMaintenanceWorkOrderCommand(
                workOrderId,
                body.ExpectedVersion,
                body.CompletionNote,
                body.ResolutionCode),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CancelWorkOrderAsync(
        Guid workOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] MaintenanceWorkOrderLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveExecuteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await workOrderRepository.CancelAsync(
            context,
            new MaintenanceWorkOrderLifecycleCommand(workOrderId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    // ─────────────────────────── Preventive Plans ───────────────────────────

    private static async Task<IResult> ListPreventivePlansAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenancePlanRepository planRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var plans = await planRepository.ListAsync(context, cancellationToken);
        return TypedResults.Ok(plans.Select(MaintenanceApiMapper.MapPreventivePlanSummary).ToList());
    }

    private static async Task<IResult> GetPreventivePlanAsync(
        Guid planId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenancePlanRepository planRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var plan = await planRepository.GetByIdAsync(context, planId, cancellationToken);
        if (plan is null)
        {
            return TypedResults.Problem(
                title: "Preventive maintenance plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaintenanceEndpointHelpers.FailureExt(MaintenanceApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaintenanceApiMapper.MapPreventivePlanDetail(plan));
    }

    private static async Task<IResult> CreatePreventivePlanAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreatePreventiveMaintenancePlanApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenancePlanRepository planRepository,
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

        var result = await planRepository.CreateAsync(
            context,
            new CreatePreventiveMaintenancePlanCommand(
                body.Code,
                body.Name,
                body.AssetType,
                body.AssetId,
                body.AssetReference,
                body.IntervalType,
                body.IntervalValue,
                body.NextDueAtUtc,
                body.Status,
                body.Description),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdatePreventivePlanAsync(
        Guid planId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdatePreventiveMaintenancePlanApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenancePlanRepository planRepository,
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

        var result = await planRepository.UpdateAsync(
            context,
            new UpdatePreventiveMaintenancePlanCommand(
                planId,
                body.ExpectedVersion,
                body.Code,
                body.Name,
                body.AssetType,
                body.AssetId,
                body.AssetReference,
                body.IntervalType,
                body.IntervalValue,
                body.NextDueAtUtc,
                body.Status,
                body.Description),
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CreateWorkOrderFromPlanAsync(
        Guid planId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateWorkOrderFromPlanApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IPreventiveMaintenancePlanRepository planRepository,
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

        var result = await planRepository.CreateWorkOrderFromPlanAsync(
            context,
            planId,
            body.ExpectedVersion,
            cancellationToken);

        return MaintenanceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    // ─────────────────────────── Workspace ───────────────────────────

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceWorkOrderRepository workOrderRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var summary = await workOrderRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        return TypedResults.Ok(MaintenanceApiMapper.MapWorkspaceSummary(summary));
    }
}

internal sealed record CreateMaintenanceRequestApiRequest(
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    Guid? ProductionOrderId,
    string Type,
    string Priority,
    string Title,
    string Description,
    string? Source);

internal sealed record UpdateMaintenanceRequestApiRequest(
    uint ExpectedVersion,
    string? Title,
    string? Description,
    string? Priority,
    string? Status);

internal sealed record ConvertMaintenanceRequestApiRequest(
    uint ExpectedVersion,
    string? AssignedTechnicianSubjectId,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc);

internal sealed record CreateMaintenanceWorkOrderApiRequest(
    string AssetType,
    string? AssetId,
    string AssetReference,
    string? AssetName,
    Guid? ProductionOrderId,
    Guid? MaintenanceRequestId,
    string Type,
    string Priority,
    string Title,
    string Description,
    string? AssignedTechnicianSubjectId,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc,
    string InitialStatus);

internal sealed record AssignMaintenanceWorkOrderApiRequest(
    uint ExpectedVersion,
    string? AssignedTechnicianSubjectId);

internal sealed record MaintenanceWorkOrderLifecycleApiRequest(
    uint ExpectedVersion,
    string? Note);

internal sealed record CompleteMaintenanceWorkOrderApiRequest(
    uint ExpectedVersion,
    string CompletionNote,
    string? ResolutionCode);

internal sealed record CreatePreventiveMaintenancePlanApiRequest(
    string Code,
    string Name,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string IntervalType,
    int IntervalValue,
    DateTimeOffset? NextDueAtUtc,
    string Status,
    string? Description);

internal sealed record UpdatePreventiveMaintenancePlanApiRequest(
    uint ExpectedVersion,
    string Code,
    string Name,
    string AssetType,
    string? AssetId,
    string AssetReference,
    string IntervalType,
    int IntervalValue,
    DateTimeOffset? NextDueAtUtc,
    string Status,
    string? Description);

internal sealed record CreateWorkOrderFromPlanApiRequest(uint ExpectedVersion);

public static class MaintenanceServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSMaintenance(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceRequestRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceRequestRepository()
                : new MaintenanceRequestRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IMaintenanceWorkOrderRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceWorkOrderRepository()
                : new MaintenanceWorkOrderRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IPreventiveMaintenancePlanRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailablePreventiveMaintenancePlanRepository()
                : new PreventiveMaintenancePlanRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddCrevaFactOSMaintenanceExecution();

        return services;
    }
}
