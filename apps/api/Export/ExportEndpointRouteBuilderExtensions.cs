using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Export;

public static class ExportEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/export/cases").RequireAuthorization();
        group.MapGet("/", ListExportCasesAsync);
        group.MapGet("/{exportCaseId:guid}", GetExportCaseAsync);
        group.MapGet("/by-order/{sourceOrderId:guid}", GetExportCaseByOrderAsync);
        group.MapPost("/from-order", EnsureExportCaseFromOrderAsync);
        group.MapGet("/{exportCaseId:guid}/readiness", GetExportReadinessAsync);
        group.MapGet("/{exportCaseId:guid}/documents", ListExportDocumentsAsync);
        group.MapPost("/{exportCaseId:guid}/documents", RegisterExportDocumentAsync);
        group.MapPost("/{exportCaseId:guid}/documents/{documentId:guid}/verify", VerifyExportDocumentAsync);
        group.MapPost("/{exportCaseId:guid}/documents/{documentId:guid}/reject", RejectExportDocumentAsync);
        group.MapGet("/{exportCaseId:guid}/activity", ListExportActivityAsync);

        // EXPORT-2L — packing
        group.MapGet("/{exportCaseId:guid}/packing", GetPackingAsync);
        group.MapPost("/{exportCaseId:guid}/packing/units", CreatePackingUnitAsync);
        group.MapPut("/{exportCaseId:guid}/packing/units/{packingUnitId:guid}", UpdatePackingUnitAsync);
        group.MapPost("/{exportCaseId:guid}/packing/units/{packingUnitId:guid}/items", AddPackingItemAsync);
        group.MapPost("/{exportCaseId:guid}/packing/complete", CompletePackingAsync);
        group.MapPost("/{exportCaseId:guid}/packing/verify", VerifyPackingAsync);
        group.MapGet("/{exportCaseId:guid}/packing/reconciliation", GetPackingReconciliationAsync);

        // EXPORT-2L — shipments
        group.MapGet("/{exportCaseId:guid}/shipments", ListShipmentsAsync);
        group.MapPost("/{exportCaseId:guid}/shipments", CreateShipmentAsync);
        group.MapGet("/{exportCaseId:guid}/shipments/{shipmentId:guid}", GetShipmentAsync);
        group.MapPut("/{exportCaseId:guid}/shipments/{shipmentId:guid}", UpdateShipmentAsync);
        group.MapPost("/{exportCaseId:guid}/shipments/{shipmentId:guid}/transition", TransitionShipmentAsync);
        group.MapGet("/{exportCaseId:guid}/shipments/{shipmentId:guid}/readiness", GetShipmentReadinessAsync);

        // EXPORT-2L — broker
        group.MapGet("/{exportCaseId:guid}/broker", GetBrokerWorkspaceAsync);
        group.MapPost("/{exportCaseId:guid}/broker", CreateBrokerWorkspaceAsync);
        group.MapPost("/{exportCaseId:guid}/broker/package/prepare", PreparePackageAsync);
        group.MapPost("/{exportCaseId:guid}/broker/package/send", SendPackageAsync);
        group.MapPost("/{exportCaseId:guid}/broker/acknowledge", AcknowledgeBrokerAsync);
        group.MapPost("/{exportCaseId:guid}/broker/corrections", CreateBrokerCorrectionAsync);
        group.MapPost("/{exportCaseId:guid}/broker/corrections/{correctionId:guid}/resolve", ResolveBrokerCorrectionAsync);

        return endpoints;
    }

    private static async Task<IResult> ListExportCasesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? readiness,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await readRepository.ListAsync(
            context, status, readiness, search, limit, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!.Select(MapSummary).ToArray());
    }

    private static async Task<IResult> GetExportCaseAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await readRepository.FindByIdAsync(context, exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Export case not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(MapDetail(result.Case!));
    }

    private static async Task<IResult> GetExportCaseByOrderAsync(
        Guid sourceOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await readRepository.FindBySourceOrderIdAsync(context, sourceOrderId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Export case not found for order.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(MapDetail(result.Case!));
    }

    private static async Task<IResult> EnsureExportCaseFromOrderAsync(
        [FromBody] EnsureExportCaseFromOrderRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (request.FacilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(request.FacilityId, ExportPermissionKeys.Write),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await writeRepository.EnsureExportCaseForOrderAsync(
            context,
            new EnsureExportCaseForOrderCommand(
                request.SourceOrderId,
                request.SourceOrderReference,
                request.CustomerName,
                request.IsExport,
                request.DestinationCountryCode,
                request.OriginCountryCode,
                request.IncotermCode),
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new ExportCaseWriteSuccessDto(
            result.ExportCaseId!.Value,
            result.CaseNumber!,
            result.Status!,
            result.ReadinessStatus!,
            result.ReadinessPercent!.Value,
            result.Version!.Value,
            result.CreatedAtUtc!.Value,
            result.UpdatedAtUtc!.Value,
            result.AlreadyExisted);

        return result.AlreadyExisted
            ? TypedResults.Ok(dto)
            : TypedResults.Created($"/api/export/cases/{dto.ExportCaseId}?facilityId={request.FacilityId}", dto);
    }

    private static async Task<IResult> GetExportReadinessAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await readRepository.GetReadinessAsync(context, exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Export case not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        var r = result.Readiness!;
        return TypedResults.Ok(new ExportReadinessDto(
            r.ExportCaseId,
            r.ReadinessStatus,
            r.ReadinessPercent,
            r.BlockingIssueCount,
            r.MissingRequirementCount,
            r.SuggestedCaseStatus,
            r.Requirements.Select(x => new ExportRequirementDto(
                x.Key, x.Label, x.IsBlocking, x.IsSatisfied, x.Status)).ToArray()));
    }

    private static async Task<IResult> ListExportDocumentsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await readRepository.ListDocumentsAsync(context, exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Export case not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!.Select(MapDocument).ToArray());
    }

    private static async Task<IResult> RegisterExportDocumentAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] RegisterExportDocumentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.DocumentManage),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var initialStatus = ExportDocumentStatus.Draft;
        if (!string.IsNullOrWhiteSpace(request.InitialStatus))
        {
            try
            {
                initialStatus = ExportValueConversions.DocumentStatusFromStorage(
                    request.InitialStatus.Trim().ToLowerInvariant());
            }
            catch (InvalidOperationException)
            {
                return MapFailure(
                    ExportFailureCodes.InvalidDocumentStatus,
                    "Initial document status is invalid.");
            }
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await writeRepository.RegisterDocumentAsync(
            context,
            new RegisterExportDocumentCommand(
                exportCaseId,
                request.DocumentType,
                request.DisplayName,
                request.FileReference,
                request.DocumentNumber,
                request.IssuedAtUtc,
                request.ExpiresAtUtc,
                request.Source ?? "manual",
                initialStatus),
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Created(
            $"/api/export/cases/{exportCaseId}/documents?facilityId={facilityId}",
            new ExportDocumentWriteSuccessDto(
                result.DocumentId!.Value,
                result.DocumentVersion!.Value,
                result.CaseVersion!.Value));
    }

    private static async Task<IResult> VerifyExportDocumentAsync(
        Guid exportCaseId,
        Guid documentId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyExportDocumentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.DocumentVerify),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await writeRepository.VerifyDocumentAsync(
            context,
            new VerifyExportDocumentCommand(exportCaseId, documentId, request.ExpectedVersion),
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new ExportDocumentWriteSuccessDto(
            result.DocumentId!.Value,
            result.DocumentVersion!.Value,
            result.CaseVersion!.Value));
    }

    private static async Task<IResult> RejectExportDocumentAsync(
        Guid exportCaseId,
        Guid documentId,
        [FromQuery] Guid? facilityId,
        [FromBody] RejectExportDocumentRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.DocumentVerify),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await writeRepository.RejectDocumentAsync(
            context,
            new RejectExportDocumentCommand(
                exportCaseId,
                documentId,
                request.ExpectedVersion,
                request.Reason),
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new ExportDocumentWriteSuccessDto(
            result.DocumentId!.Value,
            result.DocumentVersion!.Value,
            result.CaseVersion!.Value));
    }

    private static async Task<IResult> ListExportActivityAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IExportCaseReadRepository readRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await readRepository.ListActivityAsync(context, exportCaseId, limit, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Export case not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!.Select(a => new ExportActivityDto(
            a.Id,
            a.ExportCaseId,
            a.EventType,
            a.Title,
            a.Summary,
            a.OccurredAtUtc,
            a.SourceType,
            a.ActorSubjectId,
            a.RelatedEntityType,
            a.RelatedEntityId,
            a.CorrelationId)).ToArray());
    }

    private static ExportOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    private static ExportCaseSummaryDto MapSummary(ExportCaseListReadModel o) =>
        new(
            o.Id,
            o.FacilityId,
            o.SourceOrderId,
            o.CaseNumber,
            o.SourceOrderReference,
            o.CustomerName,
            o.DestinationCountryCode,
            o.OriginCountryCode,
            o.IncotermCode,
            o.Status,
            o.ReadinessStatus,
            o.ReadinessPercent,
            o.BlockingIssueCount,
            o.MissingRequirementCount,
            o.Version,
            o.UpdatedAtUtc);

    private static ExportCaseDetailDto MapDetail(ExportCaseDetailReadModel o) =>
        new(
            o.Id,
            o.FacilityId,
            o.SourceOrderId,
            o.CaseNumber,
            o.SourceOrderReference,
            o.CustomerName,
            o.DestinationCountryCode,
            o.OriginCountryCode,
            o.IncotermCode,
            o.Status,
            o.ReadinessStatus,
            o.ReadinessPercent,
            o.BlockingIssueCount,
            o.MissingRequirementCount,
            o.CreatedBySubjectId,
            o.LastModifiedBySubjectId,
            o.Version,
            o.CreatedAtUtc,
            o.UpdatedAtUtc);

    // ─── EXPORT-2L — Packing handlers ────────────────────────────────────────────

    private static async Task<IResult> GetPackingAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingReadRepository packingReadRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await packingReadRepository.GetPackingAsync(context, exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(title: "Export case not found.", statusCode: StatusCodes.Status404NotFound, extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Packing!);
    }

    private static async Task<IResult> GetPackingReconciliationAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromQuery] decimal? orderedQuantityTotal,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingReadRepository packingReadRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await packingReadRepository.GetReconciliationAsync(context, exportCaseId, orderedQuantityTotal, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(title: "Export case not found.", statusCode: StatusCodes.Status404NotFound, extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Reconciliation!);
    }

    private static async Task<IResult> CreatePackingUnitAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreatePackingUnitRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingWriteRepository packingWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.PackingManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new CreateExportPackingUnitCommand(
            exportCaseId,
            body.UnitCode,
            body.UnitType,
            body.ParentPackingUnitId,
            body.GrossWeight,
            body.NetWeight,
            body.WeightUnit,
            body.Length,
            body.Width,
            body.Height,
            body.DimensionUnit,
            body.MarksAndNumbers);

        var result = await packingWriteRepository.CreateUnitAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { packingUnitId = result.PackingUnitId, unitVersion = result.UnitVersion, caseVersion = result.CaseVersion });
    }

    private static async Task<IResult> UpdatePackingUnitAsync(
        Guid exportCaseId,
        Guid packingUnitId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdatePackingUnitRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingWriteRepository packingWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.PackingManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new UpdateExportPackingUnitCommand(
            exportCaseId,
            packingUnitId,
            body.ExpectedVersion,
            body.GrossWeight,
            body.NetWeight,
            body.WeightUnit,
            body.Length,
            body.Width,
            body.Height,
            body.DimensionUnit,
            body.MarksAndNumbers);

        var result = await packingWriteRepository.UpdateUnitAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { packingUnitId = result.PackingUnitId, unitVersion = result.UnitVersion, caseVersion = result.CaseVersion });
    }

    private static async Task<IResult> AddPackingItemAsync(
        Guid exportCaseId,
        Guid packingUnitId,
        [FromQuery] Guid? facilityId,
        [FromBody] AddPackingItemRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingWriteRepository packingWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.PackingManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new AddExportPackingItemCommand(
            exportCaseId,
            packingUnitId,
            body.ProductCode,
            body.ProductName,
            body.Quantity,
            body.UnitOfMeasure,
            body.SourceOrderLineId,
            body.LotBatchReference);

        var result = await packingWriteRepository.AddItemAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { itemId = result.ItemId, caseVersion = result.CaseVersion });
    }

    private static async Task<IResult> CompletePackingAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompletePackingRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingWriteRepository packingWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.PackingManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new CompleteExportPackingCommand(exportCaseId, body.ExpectedCaseVersion);
        var result = await packingWriteRepository.CompletePackingAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { caseVersion = result.CaseVersion, packingStatus = result.PackingStatus });
    }

    private static async Task<IResult> VerifyPackingAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] VerifyPackingRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportPackingWriteRepository packingWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.PackingVerify), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new VerifyExportPackingCommand(exportCaseId, body.ExpectedCaseVersion);
        var result = await packingWriteRepository.VerifyPackingAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { caseVersion = result.CaseVersion, packingStatus = result.PackingStatus });
    }

    // ─── EXPORT-2L — Shipment handlers ───────────────────────────────────────────

    private static async Task<IResult> ListShipmentsAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportShipmentReadRepository shipmentReadRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await shipmentReadRepository.ListAsync(context, exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Items!);
    }

    private static async Task<IResult> GetShipmentAsync(
        Guid exportCaseId,
        Guid shipmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportShipmentReadRepository shipmentReadRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await shipmentReadRepository.GetAsync(context, exportCaseId, shipmentId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(title: "Shipment not found.", statusCode: StatusCodes.Status404NotFound, extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Shipment!);
    }

    private static async Task<IResult> GetShipmentReadinessAsync(
        Guid exportCaseId,
        Guid shipmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportShipmentReadRepository shipmentReadRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await shipmentReadRepository.GetReadinessAsync(context, exportCaseId, shipmentId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(title: "Shipment not found.", statusCode: StatusCodes.Status404NotFound, extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Readiness!);
    }

    private static async Task<IResult> CreateShipmentAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateShipmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportShipmentWriteRepository shipmentWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.ShipmentManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new CreateExportShipmentCommand(
            exportCaseId,
            body.ShipmentReference,
            body.TransportMode,
            body.IncotermCode,
            body.DestinationCountryCode,
            body.DestinationCity,
            body.DeliveryAddress,
            body.ForwarderName,
            body.CarrierName,
            body.PlannedPickupAtUtc,
            body.PlannedDepartureAtUtc,
            body.PlannedArrivalAtUtc,
            body.BookingReference);

        var result = await shipmentWriteRepository.CreateAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { shipmentId = result.ShipmentId, shipmentVersion = result.ShipmentVersion, status = result.ShipmentStatus });
    }

    private static async Task<IResult> UpdateShipmentAsync(
        Guid exportCaseId,
        Guid shipmentId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateShipmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportShipmentWriteRepository shipmentWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.ShipmentManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new UpdateExportShipmentCommand(
            exportCaseId,
            shipmentId,
            body.ExpectedVersion,
            body.TransportMode,
            body.IncotermCode,
            body.DestinationCountryCode,
            body.DestinationCity,
            body.DeliveryAddress,
            body.ForwarderName,
            body.CarrierName,
            body.PlannedPickupAtUtc,
            body.PlannedDepartureAtUtc,
            body.PlannedArrivalAtUtc,
            body.ActualPickupAtUtc,
            body.ActualDepartureAtUtc,
            body.ActualArrivalAtUtc,
            body.BookingReference,
            body.TrackingReference,
            body.TotalGrossWeight,
            body.TotalNetWeight,
            body.TotalPackageCount);

        var result = await shipmentWriteRepository.UpdateAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { shipmentId = result.ShipmentId, shipmentVersion = result.ShipmentVersion, status = result.ShipmentStatus });
    }

    private static async Task<IResult> TransitionShipmentAsync(
        Guid exportCaseId,
        Guid shipmentId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionShipmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportShipmentWriteRepository shipmentWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        if (!ExportValueConversions.TryShipmentStatusFromStorage(body.TargetStatus, out var targetStatus))
        {
            return TypedResults.Problem(
                title: $"Unknown shipment status '{body.TargetStatus}'.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.ShipmentManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new TransitionExportShipmentCommand(
            exportCaseId, shipmentId, body.ExpectedVersion, targetStatus, body.CancellationReason);

        var result = await shipmentWriteRepository.TransitionAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { shipmentId = result.ShipmentId, shipmentVersion = result.ShipmentVersion, status = result.ShipmentStatus });
    }

    // ─── EXPORT-2L — Broker handlers ─────────────────────────────────────────────

    private static async Task<IResult> GetBrokerWorkspaceAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerReadRepository brokerReadRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.Read), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var result = await brokerReadRepository.GetWorkspaceAsync(context, exportCaseId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(title: "No active broker workspace.", statusCode: StatusCodes.Status404NotFound, extensions: FailureExt(ExportApiFailureCodes.NotFound));
            }

            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(result.Workspace!);
    }

    private static async Task<IResult> CreateBrokerWorkspaceAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateBrokerWorkspaceRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerWriteRepository brokerWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.BrokerManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new CreateExportBrokerWorkspaceCommand(
            exportCaseId,
            body.ShipmentId,
            body.BrokerCompanyName,
            body.BrokerContactName,
            body.BrokerEmail,
            body.BrokerPhone,
            body.Notes);

        var result = await brokerWriteRepository.CreateWorkspaceAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { workspaceId = result.WorkspaceId, workspaceVersion = result.WorkspaceVersion, status = result.WorkspaceStatus });
    }

    private static async Task<IResult> PreparePackageAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? workspaceId,
        [FromBody] PreparePackageRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerWriteRepository brokerWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        if (workspaceId is null || workspaceId == Guid.Empty)
        {
            return TypedResults.Problem(title: "workspaceId query parameter is required.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.BrokerManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new PrepareExportBrokerPackageCommand(
            exportCaseId, workspaceId.Value, body.ExpectedWorkspaceVersion, body.DocumentIds, body.Notes);

        var result = await brokerWriteRepository.PreparePackageAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { packageVersionId = result.PackageVersionId, versionNumber = result.VersionNumber, workspaceVersion = result.WorkspaceVersion });
    }

    private static async Task<IResult> SendPackageAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? workspaceId,
        [FromBody] SendPackageRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerWriteRepository brokerWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        if (workspaceId is null || workspaceId == Guid.Empty)
        {
            return TypedResults.Problem(title: "workspaceId query parameter is required.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.BrokerHandoff), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new SendExportBrokerPackageCommand(exportCaseId, workspaceId.Value, body.ExpectedWorkspaceVersion);
        var result = await brokerWriteRepository.SendPackageAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { packageVersionId = result.PackageVersionId, versionNumber = result.VersionNumber, workspaceVersion = result.WorkspaceVersion });
    }

    private static async Task<IResult> AcknowledgeBrokerAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? workspaceId,
        [FromBody] AcknowledgeBrokerRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerWriteRepository brokerWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        if (workspaceId is null || workspaceId == Guid.Empty)
        {
            return TypedResults.Problem(title: "workspaceId query parameter is required.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.BrokerManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new AcknowledgeExportBrokerCommand(exportCaseId, workspaceId.Value, body.ExpectedWorkspaceVersion, body.BrokerReference);
        var result = await brokerWriteRepository.AcknowledgeAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { workspaceId = result.WorkspaceId, workspaceVersion = result.WorkspaceVersion, status = result.WorkspaceStatus });
    }

    private static async Task<IResult> CreateBrokerCorrectionAsync(
        Guid exportCaseId,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? workspaceId,
        [FromBody] CreateCorrectionRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerWriteRepository brokerWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        if (workspaceId is null || workspaceId == Guid.Empty)
        {
            return TypedResults.Problem(title: "workspaceId query parameter is required.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.BrokerManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        if (!ExportValueConversions.TryCorrectionSeverityFromStorage(body.Severity, out var severity))
        {
            return TypedResults.Problem(title: $"Unknown severity '{body.Severity}'.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new CreateExportBrokerCorrectionCommand(
            exportCaseId, workspaceId.Value, body.Category, body.Title, body.Description, severity, body.RelatedDocumentId);

        var result = await brokerWriteRepository.CreateCorrectionAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { correctionId = result.CorrectionId });
    }

    private static async Task<IResult> ResolveBrokerCorrectionAsync(
        Guid exportCaseId,
        Guid correctionId,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? workspaceId,
        [FromBody] ResolveCorrectionRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IExportBrokerWriteRepository brokerWriteRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return MapAccessDenied(FacilityAccessFailureCodes.FacilityMissing, "A facility identifier is required.");
        }

        if (workspaceId is null || workspaceId == Guid.Empty)
        {
            return TypedResults.Problem(title: "workspaceId query parameter is required.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ExportPermissionKeys.BrokerManage), cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        if (!ExportValueConversions.TryCorrectionStatusFromStorage(body.Resolution, out var resolution))
        {
            return TypedResults.Problem(title: $"Unknown resolution status '{body.Resolution}'.", statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(ExportApiFailureCodes.ValidationFailed));
        }

        var context = BuildContext(accessResult.Access, correlationContext);
        var command = new ResolveExportBrokerCorrectionCommand(
            exportCaseId,
            workspaceId.Value,
            correctionId,
            body.ExpectedWorkspaceVersion,
            resolution,
            body.ResolutionNote);

        var result = await brokerWriteRepository.ResolveCorrectionAsync(context, command, cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new { correctionId = result.CorrectionId });
    }

    private static ExportDocumentDto MapDocument(ExportDocumentReadModel d) =>
        new(
            d.Id,
            d.ExportCaseId,
            d.DocumentType,
            d.DocumentStatus,
            d.DisplayName,
            d.FileReference,
            d.DocumentNumber,
            d.IssuedAtUtc,
            d.ExpiresAtUtc,
            d.Source,
            d.RejectionReason,
            d.VerifiedAtUtc,
            d.VerifiedBySubjectId,
            d.Version,
            d.CreatedAtUtc,
            d.UpdatedAtUtc);

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(ExportApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(ExportApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ExportApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ExportApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ExportApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ExportApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(
        string facilityFailureCode,
        string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ExportApiFailureCodes.FacilityAccessDenied));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFailure(
        string failureCode,
        string? reason) =>
        failureCode switch
        {
            ExportFailureCodes.InvalidScope
                or ExportFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.ValidationFailed)),
            ExportFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Export case not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ExportApiFailureCodes.NotFound)),
            ExportFailureCodes.DocumentNotFound => TypedResults.Problem(
                title: reason ?? "Export document not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ExportApiFailureCodes.DocumentNotFound)),
            ExportFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The export resource was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ExportApiFailureCodes.ConcurrencyConflict)),
            ExportFailureCodes.NotExportOrder => TypedResults.Problem(
                title: reason ?? "Order is not marked for export.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.NotExportOrder)),
            ExportFailureCodes.DestinationCountryRequired => TypedResults.Problem(
                title: reason ?? "Destination country is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.DestinationCountryRequired)),
            ExportFailureCodes.InvalidDestinationCountry => TypedResults.Problem(
                title: reason ?? "Destination country code is invalid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidDestinationCountry)),
            ExportFailureCodes.InvalidOriginCountry => TypedResults.Problem(
                title: reason ?? "Origin country code is invalid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidOriginCountry)),
            ExportFailureCodes.InvalidDocumentType => TypedResults.Problem(
                title: reason ?? "Document type is invalid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidDocumentType)),
            ExportFailureCodes.InvalidDocumentStatus => TypedResults.Problem(
                title: reason ?? "Document status is invalid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.InvalidDocumentStatus)),
            ExportFailureCodes.RejectionReasonRequired => TypedResults.Problem(
                title: reason ?? "Rejection reason is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ExportApiFailureCodes.RejectionReasonRequired)),
            ExportFailureCodes.CaseClosed => TypedResults.Problem(
                title: reason ?? "Export case is closed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ExportApiFailureCodes.CaseClosed)),
            ExportFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Export service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ExportApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ExportApiFailureCodes.UnexpectedError)),
        };

    private static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };
}

public static class ExportServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSExport(this IServiceCollection services)
    {
        services.AddScoped<IExportCaseReadRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableExportCaseReadRepository()
                : new ExportCaseReadRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IExportCaseWriteRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableExportCaseWriteRepository()
                : new ExportCaseWriteRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IExportComplianceWriteRepository>());
        });

        // EXPORT-2L repositories
        services.AddScoped<IExportPackingReadRepository>(serviceProvider =>
            new ExportPackingReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportPackingWriteRepository>(serviceProvider =>
            new ExportPackingWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportShipmentReadRepository>(serviceProvider =>
            new ExportShipmentReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportShipmentWriteRepository>(serviceProvider =>
            new ExportShipmentWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportBrokerReadRepository>(serviceProvider =>
            new ExportBrokerReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportBrokerWriteRepository>(serviceProvider =>
            new ExportBrokerWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        // EXPORT-3L repositories
        services.AddScoped<IExportCountryRulePackReadRepository>(serviceProvider =>
            new ExportCountryRulePackReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportCountryRulePackWriteRepository>(serviceProvider =>
            new ExportCountryRulePackWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportComplianceReadRepository>(serviceProvider =>
            new ExportComplianceReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportComplianceWriteRepository>(serviceProvider =>
            new ExportComplianceWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportCountryResearchReadRepository>(serviceProvider =>
            new ExportCountryResearchReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportCountryResearchWriteRepository>(serviceProvider =>
            new ExportCountryResearchWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportCountryKnowledgeReadRepository>(serviceProvider =>
            new ExportCountryKnowledgeReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportCountryKnowledgeWriteRepository>(serviceProvider =>
            new ExportCountryKnowledgeWriteRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        services.AddScoped<IExportTraceabilityReadRepository>(serviceProvider =>
            new ExportTraceabilityReadRepository(
                serviceProvider.GetRequiredService<CrevaFactOSDbContext>(),
                serviceProvider.GetRequiredService<TenantDatabaseSession>()));

        return services;
    }
}
