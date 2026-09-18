using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Material;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Material;

public static class MaterialLotEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMaterialLotEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/materials").RequireAuthorization();

        group.MapGet("/lots", ListLotsAsync);
        group.MapGet("/lots/workspace-summary", GetLotWorkspaceSummaryAsync);
        group.MapGet("/storage-locations", ListStorageLocationsAsync);
        group.MapPost("/lots", CreateLotAsync);
        group.MapGet("/lots/{lotId:guid}", GetLotAsync);
        group.MapGet("/lots/{lotId:guid}/timeline", GetLotTimelineAsync);
        group.MapGet("/lots/{lotId:guid}/traceability", GetLotTraceabilityAsync);
        group.MapPost("/lots/{lotId:guid}/receipt-observations", ObserveReceiptAsync);
        group.MapPost("/lots/{lotId:guid}/transfers", TransferLotAsync);
        group.MapPost("/lots/{lotId:guid}/status-transitions", TransitionLotStatusAsync);
        group.MapPost("/lots/{lotId:guid}/quality-status", RecordLotQualityStatusAsync);
        group.MapPost("/lots/{lotId:guid}/certificates", LinkLotCertificateAsync);

        return endpoints;
    }

    private static async Task<IResult> ListLotsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? search,
        [FromQuery] string? supplierLotSearch,
        [FromQuery] string? warehouseReference,
        [FromQuery] string? locationReference,
        [FromQuery] string? status,
        [FromQuery] string? qualityStatus,
        [FromQuery] string? expiryState,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IMaterialLotRepository materialLotRepository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var lots = await materialLotRepository.ListAsync(
            context,
            new MaterialLotListFilters(
                materialId,
                search,
                supplierLotSearch,
                warehouseReference,
                locationReference,
                status,
                qualityStatus,
                expiryState,
                page,
                pageSize),
            cancellationToken);

        return TypedResults.Ok(lots.Select(MaterialLotApiMapper.MapSummary).ToList());
    }

    private static async Task<IResult> GetLotWorkspaceSummaryAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var summary = await materialLotRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        if (materialId.HasValue)
        {
            var materialLots = await materialLotRepository.ListAsync(
                context,
                new MaterialLotListFilters(materialId, null, null, null, null, null, null, null, 1, 100),
                cancellationToken);
            summary = new MaterialLotWorkspaceSummaryReadModel(
                materialLots.Count,
                materialLots.Count(l => l.Status == "available"),
                materialLots.Count(l => l.Status == "quality-hold"),
                materialLots.Count(l => l.Status == "blocked"),
                materialLots.Count(l => l.IsExpiryApproaching));
        }

        return TypedResults.Ok(MaterialLotApiMapper.MapWorkspaceSummary(summary));
    }

    private static async Task<IResult> ListStorageLocationsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var locations = await materialLotRepository.ListStorageLocationsAsync(context, cancellationToken);
        return TypedResults.Ok(locations.Select(MaterialLotApiMapper.MapStorageLocation).ToList());
    }

    private static async Task<IResult> GetLotAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var lot = await materialLotRepository.GetByIdAsync(context, lotId, cancellationToken);
        if (lot is null)
        {
            return TypedResults.Problem(
                title: "Material lot not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaterialEndpointHelpers.FailureExt(MaterialApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MaterialLotApiMapper.MapDetail(lot));
    }

    private static async Task<IResult> GetLotTimelineAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var lot = await materialLotRepository.GetByIdAsync(context, lotId, cancellationToken);
        if (lot is null)
        {
            return TypedResults.Problem(
                title: "Material lot not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaterialEndpointHelpers.FailureExt(MaterialApiFailureCodes.NotFound));
        }

        var timeline = await materialLotRepository.GetTimelineAsync(context, lotId, cancellationToken);
        return TypedResults.Ok(timeline.Select(MaterialLotApiMapper.MapTimelineEntry).ToList());
    }

    private static async Task<IResult> GetLotTraceabilityAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var lot = await materialLotRepository.GetByIdAsync(context, lotId, cancellationToken);
        if (lot is null)
        {
            return TypedResults.Problem(
                title: "Material lot not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: MaterialEndpointHelpers.FailureExt(MaterialApiFailureCodes.NotFound));
        }

        var backward = await materialLotRepository.GetBackwardTraceabilityAsync(context, lotId, cancellationToken);
        var forward = await materialLotRepository.GetForwardTraceabilityAsync(context, lotId, cancellationToken);
        return TypedResults.Ok(MaterialLotApiMapper.MapTraceability(backward, forward));
    }

    private static async Task<IResult> CreateLotAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMaterialLotApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialLotRepository.CreateAsync(
            context,
            new CreateMaterialLotCommand(
                body.MaterialId,
                body.LotNumber,
                body.SupplierLotNumber,
                body.ManufacturerLotNumber,
                body.SupplierReference,
                body.PurchaseOrderReference,
                body.GoodsReceiptReference,
                body.ExternalSystem,
                body.ExternalLotReference,
                body.ManufactureDate,
                body.ExpiryDate,
                body.RetestDate,
                body.InitialQuantity,
                body.UnitOfMeasure,
                body.QualityStatus,
                body.StorageLocationId,
                body.SourceType,
                body.SourceReference,
                body.DataRecordedAtUtc,
                body.Notes,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapLotWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ObserveReceiptAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        [FromBody] ObserveMaterialLotReceiptApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialLotRepository.ObserveReceiptAsync(
            context,
            new ObserveMaterialLotReceiptCommand(
                lotId,
                body.ExpectedVersion,
                body.ReceivedAtUtc,
                body.ObservedQuantity,
                body.GoodsReceiptReference,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapLotWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> TransferLotAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransferMaterialLotApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialLotRepository.TransferAsync(
            context,
            new TransferMaterialLotCommand(
                lotId,
                body.ExpectedVersion,
                body.TargetStorageLocationId,
                body.Reason,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapLotWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> TransitionLotStatusAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionMaterialLotStatusApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialLotRepository.TransitionStatusAsync(
            context,
            new TransitionMaterialLotStatusCommand(
                lotId,
                body.ExpectedVersion,
                body.TargetStatus,
                body.Reason,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapLotWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> RecordLotQualityStatusAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        [FromBody] RecordMaterialLotQualityStatusApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialLotRepository.RecordQualityStatusAsync(
            context,
            new RecordMaterialLotQualityStatusCommand(
                lotId,
                body.ExpectedVersion,
                body.QualityStatus,
                body.Reason,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapLotWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> LinkLotCertificateAsync(
        Guid lotId,
        [FromQuery] Guid? facilityId,
        [FromBody] LinkMaterialLotCertificateApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialLotRepository materialLotRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await materialLotRepository.LinkCertificateAsync(
            context,
            new LinkMaterialLotCertificateCommand(
                lotId,
                body.CertificateType,
                body.DocumentReference,
                body.DocumentNumber,
                body.Issuer,
                body.IssuedDate,
                body.ExpiryDate,
                body.SourceType,
                body.SourceReference,
                body.IdempotencyKey),
            cancellationToken);

        return MaterialEndpointHelpers.MapLotWriteResult(result, isCreate: false);
    }
}

internal sealed record CreateMaterialLotApiRequest(
    Guid MaterialId,
    string LotNumber,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    string? SupplierReference,
    string? PurchaseOrderReference,
    string? GoodsReceiptReference,
    string? ExternalSystem,
    string? ExternalLotReference,
    DateOnly? ManufactureDate,
    DateOnly? ExpiryDate,
    DateOnly? RetestDate,
    decimal? InitialQuantity,
    string UnitOfMeasure,
    string QualityStatus,
    Guid? StorageLocationId,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? Notes,
    string? IdempotencyKey);

internal sealed record ObserveMaterialLotReceiptApiRequest(
    uint ExpectedVersion,
    DateTimeOffset ReceivedAtUtc,
    decimal ObservedQuantity,
    string? GoodsReceiptReference,
    string? SourceReference,
    string IdempotencyKey);

internal sealed record TransferMaterialLotApiRequest(
    uint ExpectedVersion,
    Guid TargetStorageLocationId,
    string? Reason,
    string? SourceReference,
    string IdempotencyKey);

internal sealed record TransitionMaterialLotStatusApiRequest(
    uint ExpectedVersion,
    string TargetStatus,
    string Reason,
    string? SourceReference,
    string IdempotencyKey);

internal sealed record RecordMaterialLotQualityStatusApiRequest(
    uint ExpectedVersion,
    string QualityStatus,
    string? Reason,
    string? SourceReference,
    string IdempotencyKey);

internal sealed record LinkMaterialLotCertificateApiRequest(
    string CertificateType,
    string DocumentReference,
    string? DocumentNumber,
    string? Issuer,
    DateOnly? IssuedDate,
    DateOnly? ExpiryDate,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);

internal static class MaterialLotApiMapper
{
    internal static object MapSummary(MaterialLotSummaryReadModel item) => new
    {
        id = item.Id,
        materialId = item.MaterialId,
        materialCode = item.MaterialCode,
        materialName = item.MaterialName,
        lotNumber = item.LotNumber,
        supplierLotNumber = item.SupplierLotNumber,
        manufacturerLotNumber = item.ManufacturerLotNumber,
        status = item.Status,
        qualityStatus = item.QualityStatus,
        currentWarehouseReference = item.CurrentWarehouseReference,
        currentLocationReference = item.CurrentLocationReference,
        currentObservedQuantity = item.CurrentObservedQuantity,
        unitOfMeasure = item.UnitOfMeasure,
        expiryDate = item.ExpiryDate,
        retestDate = item.RetestDate,
        isExpiryApproaching = item.IsExpiryApproaching,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapDetail(MaterialLotDetailReadModel item) => new
    {
        id = item.Id,
        facilityId = item.FacilityId,
        materialId = item.MaterialId,
        materialCode = item.MaterialCode,
        materialName = item.MaterialName,
        lotNumber = item.LotNumber,
        supplierLotNumber = item.SupplierLotNumber,
        manufacturerLotNumber = item.ManufacturerLotNumber,
        supplierReference = item.SupplierReference,
        purchaseOrderReference = item.PurchaseOrderReference,
        goodsReceiptReference = item.GoodsReceiptReference,
        externalSystem = item.ExternalSystem,
        externalLotReference = item.ExternalLotReference,
        manufactureDate = item.ManufactureDate,
        receivedAtUtc = item.ReceivedAtUtc,
        expiryDate = item.ExpiryDate,
        retestDate = item.RetestDate,
        initialQuantity = item.InitialQuantity,
        currentObservedQuantity = item.CurrentObservedQuantity,
        unitOfMeasure = item.UnitOfMeasure,
        status = item.Status,
        qualityStatus = item.QualityStatus,
        currentStorageLocationId = item.CurrentStorageLocationId,
        currentWarehouseReference = item.CurrentWarehouseReference,
        currentLocationReference = item.CurrentLocationReference,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        notes = item.Notes,
        createdAtUtc = item.CreatedAtUtc,
        updatedAtUtc = item.UpdatedAtUtc,
        version = item.Version,
    };

    internal static object MapWorkspaceSummary(MaterialLotWorkspaceSummaryReadModel summary) => new
    {
        totalCount = summary.TotalCount,
        availableCount = summary.AvailableCount,
        qualityHoldCount = summary.QualityHoldCount,
        blockedCount = summary.BlockedCount,
        expiryApproachingCount = summary.ExpiryApproachingCount,
    };

    internal static object MapStorageLocation(MaterialStorageLocationReadModel item) => new
    {
        id = item.Id,
        warehouseCode = item.WarehouseCode,
        warehouseName = item.WarehouseName,
        locationCode = item.LocationCode,
        locationName = item.LocationName,
        isActive = item.IsActive,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        dataRecordedAtUtc = item.DataRecordedAtUtc,
        version = item.Version,
    };

    internal static object MapTimelineEntry(MaterialLotTimelineEntryReadModel item) => new
    {
        id = item.Id,
        eventType = item.EventType,
        eventAtUtc = item.EventAtUtc,
        quantity = item.Quantity,
        unitOfMeasure = item.UnitOfMeasure,
        fromWarehouseReference = item.FromWarehouseReference,
        fromLocationReference = item.FromLocationReference,
        toWarehouseReference = item.ToWarehouseReference,
        toLocationReference = item.ToLocationReference,
        statusBefore = item.StatusBefore,
        statusAfter = item.StatusAfter,
        qualityStatusBefore = item.QualityStatusBefore,
        qualityStatusAfter = item.QualityStatusAfter,
        reason = item.Reason,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        actorDisplayName = item.ActorDisplayName,
        createdAtUtc = item.CreatedAtUtc,
        version = item.Version,
    };

    internal static object MapCertificate(MaterialLotCertificateReadModel item) => new
    {
        id = item.Id,
        certificateType = item.CertificateType,
        documentReference = item.DocumentReference,
        documentNumber = item.DocumentNumber,
        issuer = item.Issuer,
        issuedDate = item.IssuedDate,
        expiryDate = item.ExpiryDate,
        sourceType = item.SourceType,
        sourceReference = item.SourceReference,
        createdAtUtc = item.CreatedAtUtc,
        version = item.Version,
    };

    internal static object MapTraceability(
        MaterialLotBackwardTraceabilityReadModel? backward,
        MaterialLotForwardTraceabilityReadModel? forward) =>
        new
        {
            backward = backward is null ? null : new
            {
                lotId = backward.LotId,
                lotNumber = backward.LotNumber,
                materialId = backward.MaterialId,
                materialCode = backward.MaterialCode,
                materialName = backward.MaterialName,
                supplierLotNumber = backward.SupplierLotNumber,
                manufacturerLotNumber = backward.ManufacturerLotNumber,
                supplierReference = backward.SupplierReference,
                purchaseOrderReference = backward.PurchaseOrderReference,
                goodsReceiptReference = backward.GoodsReceiptReference,
                externalSystem = backward.ExternalSystem,
                externalLotReference = backward.ExternalLotReference,
                initialWarehouseReference = backward.InitialWarehouseReference,
                initialLocationReference = backward.InitialLocationReference,
                certificates = backward.Certificates.Select(MapCertificate).ToList(),
                sourceEvents = backward.SourceEvents.Select(MapTimelineEntry).ToList(),
            },
            forward = forward is null ? null : new
            {
                lotId = forward.LotId,
                lotNumber = forward.LotNumber,
                currentWarehouseReference = forward.CurrentWarehouseReference,
                currentLocationReference = forward.CurrentLocationReference,
                status = forward.Status,
                qualityStatus = forward.QualityStatus,
                locationTransfers = forward.LocationTransfers.Select(MapTimelineEntry).ToList(),
                statusChanges = forward.StatusChanges.Select(MapTimelineEntry).ToList(),
                qualityChanges = forward.QualityChanges.Select(MapTimelineEntry).ToList(),
                certificates = forward.Certificates.Select(MapCertificate).ToList(),
            },
        };
}
