using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Quality;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Quality;

public static class QualityNcrEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapQualityNcrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/quality");

        group.MapGet("/ncrs", ListNcrsAsync);
        group.MapPost("/ncrs", CreateNcrAsync);
        group.MapGet("/ncrs/{ncrId:guid}", GetNcrAsync);
        group.MapPut("/ncrs/{ncrId:guid}", UpdateNcrAsync);
        group.MapPost("/ncrs/{ncrId:guid}/resolve", ResolveNcrAsync);
        group.MapPost("/ncrs/{ncrId:guid}/close", CloseNcrAsync);
        group.MapPost("/ncrs/{ncrId:guid}/hold", PlaceHoldAsync);
        group.MapPost("/ncrs/{ncrId:guid}/hold/release", ReleaseHoldAsync);
        group.MapGet("/ncrs/{ncrId:guid}/hold", GetHoldAsync);
        group.MapPost("/ncrs/{ncrId:guid}/disposition", DecideDispositionAsync);
        group.MapGet("/ncrs/{ncrId:guid}/corrective-actions", ListCorrectiveActionsAsync);
        group.MapPost("/ncrs/{ncrId:guid}/corrective-actions", CreateCorrectiveActionAsync);
        group.MapPost("/corrective-actions/{actionId:guid}/start", StartCorrectiveActionAsync);
        group.MapPost("/corrective-actions/{actionId:guid}/complete", CompleteCorrectiveActionAsync);
        group.MapGet("/production-orders/{productionOrderId:guid}/signal", GetProductionQualitySignalAsync);
        group.MapGet("/inspection-results/{resultId:guid}/nonconformance", GetLinkedNonconformanceAsync);

        group.MapQualityNcrFollowUpEndpoints();

        return endpoints;
    }

    private static async Task<IResult> ListNcrsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] bool? holdActive,
        [FromQuery] Guid? productionOrderId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var items = await ncrRepository.ListAsync(
            context, status, severity, holdActive, productionOrderId, cancellationToken);
        return TypedResults.Ok(items.Select(QualityNcrApiMapper.MapSummary).ToList());
    }

    private static async Task<IResult> CreateNcrAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateNcrApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.NcrManage, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.CreateAsync(
            context,
            new CreateQualityNonconformanceCommand(
                body.ProductionOrderId,
                body.QualityInspectionId,
                body.QualityInspectionResultId,
                body.ProductReference,
                body.OperationReference,
                body.Title,
                body.Description,
                body.Category,
                body.Severity,
                body.OwnerSubjectId),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> GetNcrAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var ncr = await ncrRepository.GetByIdAsync(context, ncrId, cancellationToken);
        if (ncr is null)
        {
            return TypedResults.Problem(
                title: "Nonconformance not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(QualityNcrApiMapper.MapDetail(ncr));
    }

    private static async Task<IResult> UpdateNcrAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateNcrApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.NcrManage, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.UpdateAsync(
            context,
            new UpdateQualityNonconformanceCommand(
                ncrId, body.ExpectedVersion, body.Title, body.Description, body.Category,
                body.Severity, body.Status, body.OwnerSubjectId),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> PlaceHoldAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] PlaceHoldApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.Hold, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.PlaceHoldAsync(
            context,
            new PlaceQualityHoldCommand(ncrId, body.ExpectedVersion, body.Scope, body.Reason),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ReleaseHoldAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] ReleaseHoldApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.Hold, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.ReleaseHoldAsync(
            context,
            new ReleaseQualityHoldCommand(ncrId, body.ExpectedVersion, body.ReleaseReason),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> GetHoldAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var hold = await ncrRepository.GetHoldAsync(context, ncrId, cancellationToken);
        return hold is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(QualityNcrApiMapper.MapHold(hold));
    }

    private static async Task<IResult> DecideDispositionAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] DecideDispositionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.Disposition, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.DecideDispositionAsync(
            context,
            new DecideQualityDispositionCommand(ncrId, body.ExpectedVersion, body.DispositionType, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListCorrectiveActionsAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var actions = await ncrRepository.ListCorrectiveActionsAsync(context, ncrId, cancellationToken);
        return TypedResults.Ok(actions.Select(QualityNcrApiMapper.MapCorrectiveAction).ToList());
    }

    private static async Task<IResult> CreateCorrectiveActionAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCorrectiveActionApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.NcrManage, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.CreateCorrectiveActionAsync(
            context,
            new CreateQualityCorrectiveActionCommand(
                ncrId, body.Title, body.Description, body.OwnerSubjectId, body.DueAtUtc),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> StartCorrectiveActionAsync(
        Guid actionId,
        [FromQuery] Guid? facilityId,
        [FromBody] CorrectiveActionLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.NcrManage, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.StartCorrectiveActionAsync(
            context,
            new QualityCorrectiveActionLifecycleCommand(actionId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CompleteCorrectiveActionAsync(
        Guid actionId,
        [FromQuery] Guid? facilityId,
        [FromBody] CorrectiveActionLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.NcrManage, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.CompleteCorrectiveActionAsync(
            context,
            new QualityCorrectiveActionLifecycleCommand(actionId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ResolveNcrAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] NcrLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.Release, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.ResolveAsync(
            context,
            new QualityNonconformanceLifecycleCommand(ncrId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> CloseNcrAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        [FromBody] NcrLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.Release, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await ncrRepository.CloseAsync(
            context,
            new QualityNonconformanceLifecycleCommand(ncrId, body.ExpectedVersion, body.Note),
            cancellationToken);

        return QualityEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> GetProductionQualitySignalAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionQualitySignalReader signalReader,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var signal = await signalReader.ReadForProductionOrderAsync(context, productionOrderId, cancellationToken);
        return TypedResults.Ok(QualityNcrApiMapper.MapSignal(signal));
    }

    private static async Task<IResult> GetLinkedNonconformanceAsync(
        Guid resultId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository ncrRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var linked = await ncrRepository.FindByInspectionResultAsync(context, resultId, cancellationToken);
        return linked is null ? TypedResults.NotFound() : TypedResults.Ok(linked);
    }
}

internal static class QualityNcrApiMapper
{
    public static object MapSummary(QualityNonconformanceSummaryReadModel model) => new
    {
        id = model.Id,
        ncrNumber = model.NcrNumber,
        title = model.Title,
        severity = model.Severity,
        status = model.Status,
        holdStatus = model.HoldStatus,
        dispositionStatus = model.DispositionStatus,
        dispositionType = model.DispositionType,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productReference = model.ProductReference,
        ownerSubjectId = model.OwnerSubjectId,
        detectedAtUtc = model.DetectedAtUtc,
        createdAtUtc = model.CreatedAtUtc,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapDetail(QualityNonconformanceDetailReadModel model) => new
    {
        id = model.Id,
        ncrNumber = model.NcrNumber,
        title = model.Title,
        description = model.Description,
        category = model.Category,
        severity = model.Severity,
        status = model.Status,
        holdStatus = model.HoldStatus,
        dispositionStatus = model.DispositionStatus,
        dispositionType = model.DispositionType,
        dispositionNote = model.DispositionNote,
        dispositionDecidedBySubjectId = model.DispositionDecidedBySubjectId,
        dispositionDecidedAtUtc = model.DispositionDecidedAtUtc,
        productionOrderId = model.ProductionOrderId,
        productionContext = model.ProductionContext is null ? null : new
        {
            productionOrderId = model.ProductionContext.ProductionOrderId,
            referenceCode = model.ProductionContext.ReferenceCode,
            productReference = model.ProductionContext.ProductReference,
            status = model.ProductionContext.Status,
            plannedQuantity = model.ProductionContext.PlannedQuantity,
            goodQuantity = model.ProductionContext.GoodQuantity,
            scrapQuantity = model.ProductionContext.ScrapQuantity,
            available = model.ProductionContext.Available,
        },
        qualityInspectionId = model.QualityInspectionId,
        qualityInspectionNumber = model.QualityInspectionNumber,
        qualityInspectionResultId = model.QualityInspectionResultId,
        productReference = model.ProductReference,
        operationReference = model.OperationReference,
        detectedBySubjectId = model.DetectedBySubjectId,
        ownerSubjectId = model.OwnerSubjectId,
        detectedAtUtc = model.DetectedAtUtc,
        closedAtUtc = model.ClosedAtUtc,
        closedBySubjectId = model.ClosedBySubjectId,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        activeHold = model.ActiveHold is null ? null : MapHold(model.ActiveHold),
        correctiveActions = model.CorrectiveActions.Select(MapCorrectiveAction).ToList(),
    };

    public static object MapHold(QualityHoldReadModel model) => new
    {
        id = model.Id,
        scope = model.Scope,
        status = model.Status,
        reason = model.Reason,
        placedAtUtc = model.PlacedAtUtc,
        placedBySubjectId = model.PlacedBySubjectId,
        releasedAtUtc = model.ReleasedAtUtc,
        releasedBySubjectId = model.ReleasedBySubjectId,
        releaseReason = model.ReleaseReason,
        version = model.Version,
    };

    public static object MapCorrectiveAction(QualityCorrectiveActionReadModel model) => new
    {
        id = model.Id,
        title = model.Title,
        description = model.Description,
        status = model.Status,
        ownerSubjectId = model.OwnerSubjectId,
        dueAtUtc = model.DueAtUtc,
        completedAtUtc = model.CompletedAtUtc,
        completionNote = model.CompletionNote,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapSignal(ProductionQualitySignalReadModel model) => new
    {
        available = model.Available,
        qualityHoldActive = model.QualityHoldActive,
        blocking = model.Blocking,
        openNcrCount = model.OpenNcrCount,
        criticalNcrCount = model.CriticalNcrCount,
        releaseStatus = model.ReleaseStatus,
        reason = model.Reason,
    };
}

internal sealed record CreateNcrApiRequest(
    Guid? ProductionOrderId,
    Guid? QualityInspectionId,
    Guid? QualityInspectionResultId,
    string? ProductReference,
    string? OperationReference,
    string Title,
    string Description,
    string? Category,
    string Severity,
    string? OwnerSubjectId);

internal sealed record UpdateNcrApiRequest(
    uint ExpectedVersion,
    string Title,
    string Description,
    string? Category,
    string Severity,
    string Status,
    string? OwnerSubjectId);

internal sealed record NcrLifecycleApiRequest(uint ExpectedVersion, string? Note);

internal sealed record PlaceHoldApiRequest(uint ExpectedVersion, string Scope, string Reason);

internal sealed record ReleaseHoldApiRequest(uint ExpectedVersion, string ReleaseReason);

internal sealed record DecideDispositionApiRequest(uint ExpectedVersion, string DispositionType, string? Note);

internal sealed record CreateCorrectiveActionApiRequest(
    string Title,
    string Description,
    string? OwnerSubjectId,
    DateTimeOffset? DueAtUtc);

internal sealed record CorrectiveActionLifecycleApiRequest(uint ExpectedVersion, string? Note);
