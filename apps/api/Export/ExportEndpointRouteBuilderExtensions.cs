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
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
