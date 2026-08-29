using CrevaFactOS.Api.Erp;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Procurement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CrevaFactOS.Api.Procurement;

internal static class ProcurementDeliveryRiskEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProcurementDeliveryRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/procurement/delivery-risks").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/{assessmentId:guid}", GetAssessmentAsync);
        group.MapGet("/{assessmentId:guid}/history", GetHistoryAsync);
        group.MapPost("/evaluate", EvaluateAsync);
        group.MapPost("/evaluate-all", EvaluateAllAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? supplierReference,
        [FromQuery] string? status,
        [FromQuery] string? purchaseOrderReference,
        [FromQuery] string? sourceFreshness,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IProcurementDeliveryRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await HasManageAccessAsync(facilityId!.Value, facilityAccessResolver, cancellationToken);
        var workspace = await repository.GetWorkspaceAsync(
            context,
            new ProcurementDeliveryRiskListFilters(
                supplierReference,
                status,
                purchaseOrderReference,
                sourceFreshness,
                page,
                pageSize),
            canManage,
            cancellationToken);

        return TypedResults.Ok(new
        {
            ruleSetVersion = workspace.RuleSetVersion,
            automationDisclaimer = workspace.AutomationDisclaimer,
            kpis = new
            {
                overdueCount = workspace.OverdueCount,
                deliveryRiskCount = workspace.DeliveryRiskCount,
                onTrackCount = workspace.OnTrackCount,
                insufficientDataCount = workspace.InsufficientDataCount,
                sourceConflictCount = workspace.SourceConflictCount,
                latestEvaluatedAtUtc = workspace.LatestEvaluatedAtUtc,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
            },
            assessments = workspace.Assessments.Select(ProcurementDeliveryRiskApiMapper.MapRow).ToList(),
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcurementDeliveryRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        return assessment is null
            ? MapFailure(ProcurementDeliveryRiskFailureCodes.NotFound, "Delivery risk assessment not found.")
            : TypedResults.Ok(ProcurementDeliveryRiskApiMapper.MapDetail(assessment));
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int limit = 10,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IProcurementDeliveryRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var current = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        if (current is null)
        {
            return MapFailure(ProcurementDeliveryRiskFailureCodes.NotFound, "Delivery risk assessment not found.");
        }

        var history = await repository.ListAssessmentHistoryAsync(
            context,
            current.PurchaseOrderLineId,
            limit,
            cancellationToken);

        return TypedResults.Ok(history.Select(ProcurementDeliveryRiskApiMapper.MapSummary).ToList());
    }

    private static async Task<IResult> EvaluateAsync(
        [FromBody] EvaluateProcurementDeliveryRiskRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcurementDeliveryRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateAsync(
            context,
            new EvaluateProcurementDeliveryRiskCommand(request.PurchaseOrderLineId, request.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> EvaluateAllAsync(
        [FromBody] EvaluateAllProcurementDeliveryRiskRequest? request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcurementDeliveryRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateAllOpenLinesAsync(
            context,
            request?.IdempotencyKeyPrefix,
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<bool> HasManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, ErpPermissionKeys.Manage),
            cancellationToken);
        return accessResult.Succeeded && accessResult.Access is not null;
    }

    private static IResult MapWriteResult(ProcurementDeliveryRiskWriteResult result)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new
        {
            assessmentId = result.AssessmentId,
            version = result.Version,
            idempotentReplay = result.IdempotentReplay,
        });
    }

    private static IResult MapFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ProcurementDeliveryRiskFailureCodes.NotFound => TypedResults.NotFound(new { code = failureCode, message = reason }),
            ProcurementDeliveryRiskFailureCodes.AssessmentConcurrencyConflict => TypedResults.Conflict(new { code = failureCode, message = reason }),
            _ => TypedResults.BadRequest(new { code = failureCode, message = reason }),
        };
}

internal sealed record EvaluateProcurementDeliveryRiskRequest(
    Guid PurchaseOrderLineId,
    string? IdempotencyKey);

internal sealed record EvaluateAllProcurementDeliveryRiskRequest(string? IdempotencyKeyPrefix);

internal static class ProcurementDeliveryRiskApiMapper
{
    internal static object MapRow(ProcurementDeliveryRiskRowReadModel row) => new
    {
        id = row.Id,
        purchaseOrderId = row.PurchaseOrderId,
        purchaseOrderLineId = row.PurchaseOrderLineId,
        purchaseOrderNumber = row.PurchaseOrderNumber,
        externalSupplierCode = row.ExternalSupplierCode,
        supplierName = row.SupplierName,
        commitmentDateUtc = row.CommitmentDateUtc,
        receivedQuantity = row.ReceivedQuantity,
        openQuantity = row.OpenQuantity,
        unit = row.Unit,
        result = row.Result,
        sourceFreshness = row.SourceFreshness,
        evaluatedAtUtc = row.EvaluatedAtUtc,
        version = row.Version,
    };

    internal static object MapDetail(ProcurementDeliveryRiskDetailReadModel detail) => new
    {
        id = detail.Id,
        facilityId = detail.FacilityId,
        purchaseOrderId = detail.PurchaseOrderId,
        purchaseOrderLineId = detail.PurchaseOrderLineId,
        purchaseOrderNumber = detail.PurchaseOrderNumber,
        externalSupplierId = detail.ExternalSupplierId,
        externalSupplierCode = detail.ExternalSupplierCode,
        supplierName = detail.SupplierName,
        orderStatus = detail.OrderStatus,
        lineNumber = detail.LineNumber,
        externalProductId = detail.ExternalProductId,
        externalProductCode = detail.ExternalProductCode,
        lineDescription = detail.LineDescription,
        commitmentDateUtc = detail.CommitmentDateUtc,
        orderedQuantity = detail.OrderedQuantity,
        receivedQuantity = detail.ReceivedQuantity,
        openQuantity = detail.OpenQuantity,
        unit = detail.Unit,
        result = detail.Result,
        ruleSetVersion = detail.RuleSetVersion,
        inputFingerprint = detail.InputFingerprint,
        sourceUpdatedAtUtc = detail.SourceUpdatedAtUtc,
        sourceFreshness = detail.SourceFreshness,
        drivers = detail.Drivers.Select(d => new
        {
            code = d.Code,
            summary = d.Summary,
            reference = d.Reference,
            state = d.State,
        }).ToList(),
        evidence = detail.Evidence.Select(e => new
        {
            field = e.Field,
            value = e.Value,
            source = e.Source,
            recordedAtUtc = e.RecordedAtUtc,
            isUnknown = e.IsUnknown,
        }).ToList(),
        evaluatedAtUtc = detail.EvaluatedAtUtc,
        version = detail.Version,
    };

    internal static object MapSummary(ProcurementDeliveryRiskAssessmentSummaryReadModel summary) => new
    {
        id = summary.Id,
        result = summary.Result,
        evaluatedAtUtc = summary.EvaluatedAtUtc,
        ruleSetVersion = summary.RuleSetVersion,
    };
}

public static class ProcurementEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapProcurementDeliveryRiskEndpoints();
        return endpoints;
    }

    public static IServiceCollection AddCrevaFactOSProcurement(this IServiceCollection services)
    {
        services.AddScoped<IProcurementDeliveryRiskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetRequiredService<CrevaFactOS.Persistence.CrevaFactOSDbContext>();
            var tenantDatabaseSession = serviceProvider.GetRequiredService<CrevaFactOS.Persistence.Tenancy.TenantDatabaseSession>();
            return new ProcurementDeliveryRiskRepository(dbContext, tenantDatabaseSession);
        });

        return services;
    }
}
