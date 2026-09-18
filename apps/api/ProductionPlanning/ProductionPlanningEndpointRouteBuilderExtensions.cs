using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.ProductionPlanning;

public static class ProductionPlanningEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionPlanningReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production-planning/plans")
            .RequireAuthorization();

        group.MapGet("/", ListProductionPlansAsync);
        group.MapGet("/{productionPlanId:guid}", GetProductionPlanDetailAsync);

        return endpoints;
    }

    public static IEndpointRouteBuilder MapProductionPlanningWriteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production-planning/plans")
            .RequireAuthorization();

        group.MapPost("/", CreateProductionPlanDraftAsync);
        group.MapPut("/{productionPlanId:guid}", UpdateProductionPlanDraftAsync);

        return endpoints;
    }

    public static IEndpointRouteBuilder MapProductionPlanningWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production-planning/plans")
            .RequireAuthorization();

        group.MapPost("/{productionPlanId:guid}/submit-for-review", SubmitProductionPlanForReviewAsync);
        group.MapPost("/{productionPlanId:guid}/request-revision", RequestProductionPlanRevisionAsync);
        group.MapPost("/{productionPlanId:guid}/approve", ApproveProductionPlanAsync);
        group.MapPost("/{productionPlanId:guid}/reject", RejectProductionPlanAsync);
        group.MapPost("/{productionPlanId:guid}/schedule", ScheduleProductionPlanAsync);
        group.MapPost("/{productionPlanId:guid}/transfer-requests", CreateTransferRequestAsync);
        group.MapPost("/{productionPlanId:guid}/transfer-requests/{transferRequestId:guid}/execute",
            ExecuteTransferAsync);
        group.MapPost("/{productionPlanId:guid}/handoff", PrepareHandoffAsync);

        return endpoints;
    }

    private static async Task<IResult> ListProductionPlansAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanReadService readService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Read));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Read),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = ProductionPlanApiResultMapper.CreateOperationContext(
            accessResult.Access,
            "production-planning.list",
            accessResult.EvaluatedAtUtc,
            productionPlanId: null,
            ReadCorrelationId(correlationContext));

        var serviceResult = await readService.ListAsync(
            operationContext,
            new ProductionPlanListRequest(facilityId.Value, status, search, limit),
            cancellationToken);

        return serviceResult.Succeeded
            ? TypedResults.Ok(serviceResult.Value)
            : ProductionPlanApiResultMapper.MapServiceFailure(serviceResult.FailureCode!);
    }

    private static async Task<IResult> GetProductionPlanDetailAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanReadService readService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Read));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Read),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = ProductionPlanApiResultMapper.CreateOperationContext(
            accessResult.Access,
            "production-planning.detail",
            accessResult.EvaluatedAtUtc,
            productionPlanId,
            ReadCorrelationId(correlationContext));

        var serviceResult = await readService.GetDetailAsync(
            operationContext,
            new ProductionPlanDetailRequest(facilityId.Value, productionPlanId),
            cancellationToken);

        return serviceResult.Succeeded
            ? TypedResults.Ok(serviceResult.Value)
            : ProductionPlanApiResultMapper.MapServiceFailure(serviceResult.FailureCode!);
    }

    private static async Task<IResult> CreateProductionPlanDraftAsync(
        [FromBody] CreateProductionPlanDraftRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanDraftWriteService writeService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (body is null || body.FacilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required in the request body.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Write));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(body.FacilityId, ProductionPlanningPermissionKeys.Write),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await writeService.CreateDraftAsync(operationContext, body, cancellationToken);

        return ProductionPlanApiResultMapper.MapWriteServiceResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateProductionPlanDraftAsync(
        Guid productionPlanId,
        [FromBody] UpdateProductionPlanDraftRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanDraftWriteService writeService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (body is null || body.FacilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required in the request body.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Write));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(body.FacilityId, ProductionPlanningPermissionKeys.Write),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await writeService.UpdateDraftAsync(
            operationContext,
            productionPlanId,
            body,
            cancellationToken);

        return ProductionPlanApiResultMapper.MapWriteServiceResult(result, isCreate: false);
    }

    private static async Task<IResult> SubmitProductionPlanForReviewAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] SubmitProductionPlanForReviewRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanWorkflowService workflowService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Review));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Review),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await workflowService.SubmitForReviewAsync(
            operationContext, productionPlanId, body, cancellationToken);

        return ProductionPlanApiResultMapper.MapWorkflowResult(result);
    }

    private static async Task<IResult> RequestProductionPlanRevisionAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] RequestProductionPlanRevisionRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanWorkflowService workflowService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Review));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Review),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await workflowService.RequestRevisionAsync(
            operationContext, productionPlanId, body, cancellationToken);

        return ProductionPlanApiResultMapper.MapWorkflowResult(result);
    }

    private static async Task<IResult> ApproveProductionPlanAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveProductionPlanRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanWorkflowService workflowService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Approve));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Approve),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await workflowService.ApproveAsync(
            operationContext, productionPlanId, body, cancellationToken);

        return ProductionPlanApiResultMapper.MapWorkflowResult(result);
    }

    private static async Task<IResult> RejectProductionPlanAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] RejectProductionPlanRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanWorkflowService workflowService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Approve));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Approve),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await workflowService.RejectAsync(
            operationContext, productionPlanId, body, cancellationToken);

        return ProductionPlanApiResultMapper.MapWorkflowResult(result);
    }

    private static async Task<IResult> ScheduleProductionPlanAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] ScheduleProductionPlanRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanWorkflowService workflowService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Schedule));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Schedule),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await workflowService.ScheduleAsync(operationContext, productionPlanId, body, cancellationToken);
        return ProductionPlanApiResultMapper.MapWorkflowResult(result);
    }

    private static async Task<IResult> CreateTransferRequestAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateTransferRequestApiRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanTransferService transferService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Transfer));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Transfer),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var (result, isCreated) = await transferService.CreateTransferRequestAsync(
            operationContext, productionPlanId, body, cancellationToken);

        if (!result.Succeeded)
        {
            return MapTransferRequestFailure(result.FailureCode!, result.FailureReason!);
        }

        var response = new TransferRequestCreatedResponse(
            result.TransferRequestId!.Value,
            result.ProductionPlanId!.Value,
            result.TargetSystem!,
            result.Status!,
            result.RequestedAtUtc!.Value,
            result.IsIdempotentDuplicate);

        return isCreated
            ? TypedResults.Created((string?)null, response)
            : TypedResults.Ok(response);
    }

    private static async Task<IResult> ExecuteTransferAsync(
        Guid productionPlanId,
        Guid transferRequestId,
        [FromQuery] Guid? facilityId,
        [FromBody] ExecuteTransferApiRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanTransferService transferService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Transfer));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Transfer),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await transferService.ExecuteTransferAsync(
            operationContext, productionPlanId, transferRequestId, body, cancellationToken);

        if (!result.Succeeded)
        {
            return MapTransferExecuteFailure(result.FailureCode!, result.FailureReason!);
        }

        return TypedResults.Ok(new TransferExecuteResponse(
            result.TransferRequestId!.Value,
            result.FinalStatus!,
            result.ExternalReference,
            result.Retryable,
            result.SafeFailureMessage,
            result.ReconciliationRequired));
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapTransferRequestFailure(string failureCode, string reason) =>
        failureCode switch
        {
            ProductionPlanTransferFailureCodes.InvalidScope
                or ProductionPlanTransferFailureCodes.ValidationFailed => TypedResults.Problem(
                    title: reason,
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest }),
            ProductionPlanTransferFailureCodes.NotFound => TypedResults.Problem(
                title: "Production plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.NotFound }),
            ProductionPlanTransferFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidTransition }),
            ProductionPlanTransferFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.ConcurrencyConflict }),
            ProductionPlanTransferFailureCodes.TransferAlreadyRequested => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.TransferAlreadyRequested }),
            ProductionPlanTransferFailureCodes.TransferAlreadySucceeded => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.TransferAlreadySucceeded }),
            ProductionPlanTransferFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Transfer service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.SourceUnavailable }),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.UnexpectedError }),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapTransferExecuteFailure(string failureCode, string reason) =>
        failureCode switch
        {
            ProductionPlanTransferFailureCodes.InvalidScope
                or ProductionPlanTransferFailureCodes.ValidationFailed => TypedResults.Problem(
                    title: reason,
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest }),
            ProductionPlanTransferFailureCodes.NotFound => TypedResults.Problem(
                title: "Transfer request or plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.NotFound }),
            ProductionPlanTransferFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidTransition }),
            ProductionPlanTransferFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.ConcurrencyConflict }),
            ProductionPlanTransferFailureCodes.TransferAlreadySucceeded => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.TransferAlreadySucceeded }),
            ProductionPlanTransferFailureCodes.TransferNotRetryable => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.TransferNotRetryable }),
            ProductionPlanTransferFailureCodes.AdapterNotFound => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.AdapterNotFound }),
            ProductionPlanTransferFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Transfer execution service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.SourceUnavailable }),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.UnexpectedError }),
        };

    private static async Task<IResult> PrepareHandoffAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        [FromBody] PrepareHandoffApiRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanHandoffService handoffService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Handoff));
        }

        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest });
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Handoff),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await handoffService.PrepareHandoffAsync(
            operationContext, productionPlanId, body, cancellationToken);

        if (!result.Succeeded)
        {
            return MapHandoffFailure(result.FailureCode!, result.FailureReason!);
        }

        return TypedResults.Created(
            (string?)null,
            new ProductionPlanHandoffApiResponse(
                result.HandoffId!.Value,
                result.ProductionPlanId!.Value,
                result.ReferenceCode!,
                result.Status!,
                result.HandoffVersion!.Value,
                result.PreparedAtUtc!.Value));
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapHandoffFailure(string failureCode, string reason) =>
        failureCode switch
        {
            ProductionPlanHandoffFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidRequest }),
            ProductionPlanHandoffFailureCodes.NotFound => TypedResults.Problem(
                title: "Production plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.NotFound }),
            ProductionPlanHandoffFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.ConcurrencyConflict }),
            ProductionPlanHandoffFailureCodes.NotScheduled
                or ProductionPlanHandoffFailureCodes.NotEligible
                or ProductionPlanHandoffFailureCodes.BlockingRisk => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.InvalidTransition }),
            ProductionPlanHandoffFailureCodes.AlreadyPrepared => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["failureCode"] = "handoff-already-prepared" }),
            ProductionPlanHandoffFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Handoff service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.SourceUnavailable }),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["failureCode"] = ProductionPlanApiFailureCodes.UnexpectedError }),
        };

    private static Guid? ReadCorrelationId(CorrelationContext correlationContext) =>
        correlationContext.IsAvailable ? correlationContext.CorrelationId : null;
}

public static class ProductionPlanningServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraProductionPlanning(this IServiceCollection services)
    {
        services.AddScoped<IProductionPlanReadRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanReadRepository()
                : new ProductionPlanReadRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });
        services.AddScoped<IProductionPlanWriteRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanWriteRepository()
                : new ProductionPlanWriteRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });
        services.AddScoped<IProductionPlanReadService, ProductionPlanReadService>();
        services.AddScoped<IProductionPlanDraftWriteService, ProductionPlanDraftWriteService>();
        services.AddScoped<IProductionPlanWorkflowRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanWorkflowRepository()
                : new ProductionPlanWorkflowRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });
        services.AddScoped<IProductionPlanWorkflowService, ProductionPlanWorkflowService>();

        services.AddScoped<IProductionPlanTransferRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanTransferRepository()
                : new ProductionPlanTransferRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IProductionPlanTransferService>(serviceProvider =>
        {
            var repository = serviceProvider.GetRequiredService<IProductionPlanTransferRepository>();
            var adapters = serviceProvider.GetServices<IProductionPlanTransferAdapter>();
            return new ProductionPlanTransferService(repository, adapters);
        });

        services.AddScoped<IProductionPlanHandoffRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanHandoffRepository()
                : new ProductionPlanHandoffRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });
        services.AddScoped<IProductionPlanHandoffService, ProductionPlanHandoffService>();

        services.AddScoped<IPlanningLogisticsSourceReader>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailablePlanningLogisticsSourceReader()
                : new PlanningLogisticsSourceReader(dbContext);
        });
        services.AddScoped<IPlanningMaterialInboundContextReader, UnavailablePlanningMaterialInboundContextReader>();
        services.AddScoped<IPlanningLogisticsConstraintRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailablePlanningLogisticsConstraintRepository()
                : new PlanningLogisticsConstraintRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IPlanningLogisticsSourceReader>(),
                    serviceProvider.GetRequiredService<IPlanningMaterialInboundContextReader>());
        });
        services.AddScoped<IPlanningLogisticsService, PlanningLogisticsService>();

        return services;
    }
}
