using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Production;

public static class ProductionExecutionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionExecutionReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production/orders").RequireAuthorization();
        group.MapGet("/", ListProductionOrdersAsync);
        group.MapGet("/{productionOrderId:guid}", GetProductionOrderDetailAsync);
        return endpoints;
    }

    public static IEndpointRouteBuilder MapProductionExecutionWriteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production").RequireAuthorization();
        group.MapPost("/handoffs/{handoffId:guid}/consume", ConsumeHandoffAsync);
        group.MapPost("/orders/{productionOrderId:guid}/start", StartProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/pause", PauseProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/resume", ResumeProductionOrderAsync);
        group.MapPost("/orders/{productionOrderId:guid}/complete", CompleteProductionOrderAsync);
        return endpoints;
    }

    // ─────────────────────────── Read: List ───────────────────────────

    private static async Task<IResult> ListProductionOrdersAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderReadRepository readRepository,
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
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);

        var result = await readRepository.ListAsync(context, status, search, limit, cancellationToken);

        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, null);
        }

        var dtos = result.Items!.Select(o => new ProductionOrderSummaryDto(
            o.Id,
            o.FacilityId,
            o.SourceProductionPlanId,
            o.ReferenceCode,
            o.Title,
            o.Status,
            o.PlannedStartAtUtc,
            o.PlannedEndAtUtc,
            o.ActualStartAtUtc,
            o.MachineReference,
            o.MoldReference,
            o.ShiftName,
            o.Version,
            o.UpdatedAtUtc)).ToArray();

        return TypedResults.Ok(dtos);
    }

    // ─────────────────────────── Read: Detail ───────────────────────────

    private static async Task<IResult> GetProductionOrderDetailAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderReadRepository readRepository,
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
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);

        var result = await readRepository.FindByIdAsync(context, productionOrderId, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.NotFound)
            {
                return TypedResults.Problem(
                    title: "Production order not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(ProductionExecutionApiFailureCodes.NotFound));
            }

            return MapWriteFailure(result.FailureCode!, null);
        }

        var o = result.Order!;
        return TypedResults.Ok(new ProductionOrderDetailDto(
            o.Id,
            o.FacilityId,
            o.ProductionPlanHandoffId,
            o.SourceProductionPlanId,
            o.SourceProductionPlanVersion,
            o.ReferenceCode,
            o.Title,
            o.Status,
            o.PlannedStartAtUtc,
            o.PlannedEndAtUtc,
            o.ActualStartAtUtc,
            o.ActualEndAtUtc,
            o.ShiftName,
            o.MachineReference,
            o.MoldReference,
            o.ProductReference,
            o.CreatedBySubjectId,
            o.LastModifiedBySubjectId,
            o.Version,
            o.CreatedAtUtc,
            o.UpdatedAtUtc));
    }

    // ─────────────────────────── Write: Consume Handoff ───────────────────────────

    private static async Task<IResult> ConsumeHandoffAsync(
        Guid handoffId,
        [FromQuery] Guid? facilityId,
        [FromBody] ConsumeHandoffApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
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
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Write),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return MapFacilityAccessFailure(accessResult);
        }

        var context = BuildContext(accessResult.Access, correlationContext);

        var result = await writeRepository.ConsumeHandoffAsync(
            context,
            new ConsumeHandoffCommand(handoffId, body.ExpectedHandoffVersion),
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Created(
            (string?)null,
            new ProductionOrderWriteSuccessDto(
                result.ProductionOrderId!.Value,
                result.ReferenceCode!,
                result.Status!,
                result.Version!.Value,
                result.CreatedAtUtc!.Value,
                result.UpdatedAtUtc!.Value));
    }

    // ─────────────────────────── Write: Start ───────────────────────────

    private static async Task<IResult> StartProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] StartProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Start,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.StartAsync(
            context, new StartProductionOrderCommand(productionOrderId, body.ExpectedVersion), cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Write: Pause ───────────────────────────

    private static async Task<IResult> PauseProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] PauseProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Pause,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.PauseAsync(
            context,
            new PauseProductionOrderCommand(productionOrderId, body.ExpectedVersion, body.Reason ?? string.Empty),
            cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Write: Resume ───────────────────────────

    private static async Task<IResult> ResumeProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] ResumeProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Start,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.ResumeAsync(
            context, new ResumeProductionOrderCommand(productionOrderId, body.ExpectedVersion), cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Write: Complete ───────────────────────────

    private static async Task<IResult> CompleteProductionOrderAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        [FromBody] CompleteProductionOrderApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionOrderWriteRepository writeRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ResolveWrite(
            facilityId, ProductionExecutionPermissionKeys.Complete,
            facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var result = await writeRepository.CompleteAsync(
            context, new CompleteProductionOrderCommand(productionOrderId, body.ExpectedVersion), cancellationToken);

        return MapLifecycleResult(result);
    }

    // ─────────────────────────── Helpers ───────────────────────────

    private static async Task<(ProductionOrderOperationContext? context, IResult? failResult)> ResolveWrite(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    private static ProductionOrderOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    private static IResult MapLifecycleResult(ProductionOrderWriteResult result)
    {
        if (!result.Succeeded)
        {
            return MapWriteFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new ProductionOrderWriteSuccessDto(
            result.ProductionOrderId!.Value,
            result.ReferenceCode!,
            result.Status!,
            result.Version!.Value,
            result.CreatedAtUtc!.Value,
            result.UpdatedAtUtc!.Value));
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(ProductionExecutionApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string facilityFailureCode, string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.FacilityAccessDenied));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapWriteFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ProductionOrderFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidRequest)),
            ProductionOrderFailureCodes.HandoffNotFound => TypedResults.Problem(
                title: "Production plan handoff not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffNotFound)),
            ProductionOrderFailureCodes.HandoffNotReady => TypedResults.Problem(
                title: reason ?? "Handoff is not in a ready state.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffNotReady)),
            ProductionOrderFailureCodes.HandoffAlreadyConsumed => TypedResults.Problem(
                title: reason ?? "This handoff has already been consumed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffAlreadyConsumed)),
            ProductionOrderFailureCodes.HandoffInvalid => TypedResults.Problem(
                title: reason ?? "Handoff payload is invalid.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.HandoffInvalid)),
            ProductionOrderFailureCodes.NotFound => TypedResults.Problem(
                title: "Production order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.NotFound)),
            ProductionOrderFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This lifecycle transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.InvalidTransition)),
            ProductionOrderFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The production order was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.ConcurrencyConflict)),
            ProductionOrderFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Production order service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ProductionExecutionApiFailureCodes.UnexpectedError)),
        };

    private static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };
}

public static class ProductionExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSProductionExecution(this IServiceCollection services)
    {
        services.AddScoped<IProductionOrderReadRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionOrderReadRepository()
                : new ProductionOrderReadRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionOrderWriteRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionOrderWriteRepository()
                : new ProductionOrderWriteRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
