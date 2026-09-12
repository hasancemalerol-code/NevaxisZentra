using System.Globalization;
using NevaxisFactOS.Api.AiTasks;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.ShiftHandover;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.ShiftHandover;

public static class ShiftHandoverEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShiftHandoverEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shift-handovers").RequireAuthorization();

        group.MapGet("/", ListHandoversAsync);
        group.MapPost("/", CreateHandoverAsync);
        group.MapGet("/{handoverId:guid}", GetHandoverAsync);
        group.MapPatch("/{handoverId:guid}", UpdateHandoverAsync);
        group.MapPost("/{handoverId:guid}/items", AddItemAsync);
        group.MapPatch("/{handoverId:guid}/items/{itemId:guid}", UpdateItemAsync);
        group.MapPost("/{handoverId:guid}/ready", MarkReadyAsync);
        group.MapPost("/{handoverId:guid}/handover", HandOverAsync);
        group.MapPost("/{handoverId:guid}/acknowledge", AcknowledgeAsync);
        group.MapPost("/{handoverId:guid}/complete", CompleteAsync);
        group.MapPost("/{handoverId:guid}/items/{itemId:guid}/follow-up-task", CreateFollowUpTaskAsync);
        group.MapGet("/{handoverId:guid}/history", GetHistoryAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSShiftHandover(this IServiceCollection services)
    {
        services.AddScoped<IShiftHandoverRepository>(sp =>
        {
            var dbContext = sp.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableShiftHandoverRepository()
                : new ShiftHandoverRepository(
                    dbContext,
                    sp.GetRequiredService<TenantDatabaseSession>(),
                    sp.GetRequiredService<NevaxisFactOS.Persistence.Teams.ITeamsNotificationRepository>());
        });

        return services;
    }

    private static async Task<IResult> ListHandoversAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? shiftDate,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        DateOnly? parsedDate = DateOnly.TryParse(shiftDate, out var d) ? d : null;

        var result = await repository.ListAsync(context, capabilities, status, parsedDate, cancellationToken);
        return TypedResults.Ok(new
        {
            items = result.Items.Select(ShiftHandoverApiMapper.MapSummary).ToList(),
            capabilities = new
            {
                canRead = result.Capabilities.CanRead,
                canManage = result.Capabilities.CanManage,
                canAcknowledge = result.Capabilities.CanAcknowledge,
                canCreateTask = result.Capabilities.CanCreateTask,
            },
            automationDisclaimer = result.AutomationDisclaimer,
        });
    }

    private static async Task<IResult> GetHandoverAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var detail = await repository.GetByIdAsync(context, capabilities, handoverId, cancellationToken);

        return detail is null
            ? TypedResults.NotFound(new { code = ShiftHandoverFailureCodes.NotFound, message = "Vardiya devri bulunamad─▒." })
            : TypedResults.Ok(ShiftHandoverApiMapper.MapDetail(detail));
    }

    private static async Task<IResult> CreateHandoverAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateShiftHandoverApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!DateOnly.TryParse(body.ShiftDate, out var shiftDate))
        {
            return ShiftHandoverEndpointHelpers.MapFailure(
                ShiftHandoverFailureCodes.ValidationFailed,
                "Vardiya tarihi ge├ğersiz.");
        }

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.CreateAsync(
            context,
            capabilities,
            new CreateShiftHandoverCommand(
                shiftDate,
                body.ShiftPeriod,
                body.Department,
                body.Summary ?? string.Empty,
                body.IncomingMembershipId),
            cancellationToken);

        return MapWriteResult(result, handoverId => TypedResults.Created(
            $"/api/shift-handovers/{handoverId:D}",
            new { id = handoverId, version = result.Version }));
    }

    private static async Task<IResult> UpdateHandoverAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateShiftHandoverApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!DateOnly.TryParse(body.ShiftDate, out var shiftDate))
        {
            return ShiftHandoverEndpointHelpers.MapFailure(
                ShiftHandoverFailureCodes.ValidationFailed,
                "Vardiya tarihi ge├ğersiz.");
        }

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.UpdateAsync(
            context,
            capabilities,
            new UpdateShiftHandoverCommand(
                handoverId,
                body.ExpectedVersion,
                shiftDate,
                body.ShiftPeriod,
                body.Department,
                body.Summary ?? string.Empty,
                body.IncomingMembershipId),
            cancellationToken);

        return MapWriteResult(result, id => TypedResults.Ok(new { id, version = result.Version }));
    }

    private static async Task<IResult> AddItemAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] AddShiftHandoverItemApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.AddItemAsync(
            context,
            capabilities,
            new AddShiftHandoverItemCommand(
                handoverId,
                body.Category,
                body.Title,
                body.Description ?? string.Empty,
                body.Severity,
                body.SourceModule,
                body.SourceEntityId,
                body.RequiresFollowUp,
                body.EvidenceMetadataJson),
            cancellationToken);

        return MapWriteResult(result, id => TypedResults.Created(
            $"/api/shift-handovers/{handoverId:D}/items/{id:D}",
            new { id, version = result.Version }));
    }

    private static async Task<IResult> UpdateItemAsync(
        Guid handoverId,
        Guid itemId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateShiftHandoverItemApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.UpdateItemAsync(
            context,
            capabilities,
            new UpdateShiftHandoverItemCommand(
                handoverId,
                itemId,
                body.ExpectedVersion,
                body.Category,
                body.Title,
                body.Description ?? string.Empty,
                body.Severity,
                body.Status,
                body.RequiresFollowUp,
                body.EvidenceMetadataJson),
            cancellationToken);

        return MapWriteResult(result, id => TypedResults.Ok(new { id, version = result.Version }));
    }

    private static async Task<IResult> MarkReadyAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] ShiftHandoverLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await LifecycleActionAsync(
            handoverId, facilityId, body, facilityAccessResolver, repository, correlationContext,
            (ctx, caps, cmd) => repository.MarkReadyAsync(ctx, caps, cmd, cancellationToken),
            cancellationToken);

    private static async Task<IResult> HandOverAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] ShiftHandoverLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await LifecycleActionAsync(
            handoverId, facilityId, body, facilityAccessResolver, repository, correlationContext,
            (ctx, caps, cmd) => repository.HandOverAsync(ctx, caps, cmd, cancellationToken),
            cancellationToken);

    private static async Task<IResult> AcknowledgeAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] ShiftHandoverLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveAcknowledgeAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.AcknowledgeAsync(
            context,
            capabilities,
            new ShiftHandoverLifecycleCommand(handoverId, body.ExpectedVersion),
            cancellationToken);

        return MapWriteResult(result, id => TypedResults.Ok(new
        {
            id,
            version = result.Version,
            idempotentReplay = result.IdempotentReplay,
        }));
    }

    private static async Task<IResult> CompleteAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] ShiftHandoverLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await LifecycleActionAsync(
            handoverId, facilityId, body, facilityAccessResolver, repository, correlationContext,
            (ctx, caps, cmd) => repository.CompleteAsync(ctx, caps, cmd, cancellationToken),
            cancellationToken);

    private static async Task<IResult> CreateFollowUpTaskAsync(
        Guid handoverId,
        Guid itemId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await ShiftHandoverEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var detail = await repository.GetByIdAsync(readContext, capabilities, handoverId, cancellationToken);
        if (detail is null)
        {
            return TypedResults.NotFound(new { code = ShiftHandoverFailureCodes.NotFound, message = "Vardiya devri bulunamad─▒." });
        }

        var item = detail.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return TypedResults.NotFound(new { code = ShiftHandoverFailureCodes.ItemNotFound, message = "Madde bulunamad─▒." });
        }

        if (item.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = item.LinkedAiTaskId.Value,
                handoverId,
                itemId,
                idempotentReplay = true,
                deepLinkPath = ShiftHandoverPresentationBuilder.BuildDeepLink(handoverId),
            });
        }

        if (!item.RequiresFollowUp)
        {
            return ShiftHandoverEndpointHelpers.MapFailure(
                ShiftHandoverFailureCodes.ValidationFailed,
                "Bu madde takip gerektirmiyor.");
        }

        var handoverEntity = new FactoryShiftHandover
        {
            Id = detail.Id,
            HandoverReference = detail.HandoverReference,
            ShiftDate = detail.ShiftDate,
            ShiftPeriod = detail.ShiftPeriod,
            HandedOverAtUtc = detail.HandedOverAtUtc,
            UpdatedAtUtc = detail.UpdatedAtUtc,
            Status = detail.Status,
        };
        var itemEntity = new ShiftHandoverItem
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Category = item.Category,
            Severity = item.Severity,
            RequiresFollowUp = item.RequiresFollowUp,
            Status = item.Status,
            EvidenceMetadataJson = item.EvidenceMetadataJson,
            UpdatedAtUtc = item.UpdatedAtUtc,
        };

        if (!ShiftHandoverPresentationBuilder.CanCreateFollowUpTask(handoverEntity, itemEntity))
        {
            return ShiftHandoverEndpointHelpers.MapFailure(
                ShiftHandoverFailureCodes.InvalidTransition,
                "Bu a┼şamada takip g├Ârevi olu┼şturulamaz.");
        }

        var taskCommand = ShiftHandoverPresentationBuilder.BuildFollowUpTaskCommand(handoverEntity, itemEntity);
        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(createContext, taskCommand, cancellationToken);
        if (!taskResult.Succeeded)
        {
            return AiTaskEndpointHelpers.MapFailure(taskResult.FailureCode!, taskResult.FailureReason!);
        }

        var taskId = taskResult.EntityId!.Value;
        var linkResult = await repository.LinkItemTaskAsync(
            readContext,
            new LinkShiftHandoverItemTaskCommand(handoverId, itemId, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return ShiftHandoverEndpointHelpers.MapFailure(linkResult.FailureCode!, linkResult.FailureReason!);
        }

        return TypedResults.Ok(new
        {
            taskId,
            handoverId,
            itemId,
            idempotentReplay = taskResult.IdempotentReplay,
            deepLinkPath = ShiftHandoverPresentationBuilder.BuildDeepLink(handoverId),
        });
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var events = await repository.GetHistoryAsync(context, handoverId, cancellationToken);
        return TypedResults.Ok(new
        {
            handoverId,
            events = events.Select(ShiftHandoverApiMapper.MapEvent).ToList(),
        });
    }

    private static async Task<IResult> LifecycleActionAsync(
        Guid handoverId,
        Guid? facilityId,
        ShiftHandoverLifecycleApiRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IShiftHandoverRepository repository,
        CorrelationContext correlationContext,
        Func<ShiftHandoverOperationContext, ShiftHandoverAccessCapabilities, ShiftHandoverLifecycleCommand, Task<ShiftHandoverWriteResult>> action,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ShiftHandoverEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await ShiftHandoverEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await action(
            context,
            capabilities,
            new ShiftHandoverLifecycleCommand(handoverId, body.ExpectedVersion));

        return MapWriteResult(result, id => TypedResults.Ok(new
        {
            id,
            version = result.Version,
            idempotentReplay = result.IdempotentReplay,
        }));
    }

    private static IResult MapWriteResult(
        ShiftHandoverWriteResult result,
        Func<Guid, IResult> onSuccess)
    {
        if (!result.Succeeded)
        {
            var statusCode = result.FailureCode switch
            {
                ShiftHandoverFailureCodes.NotFound or ShiftHandoverFailureCodes.ItemNotFound
                    => StatusCodes.Status404NotFound,
                ShiftHandoverFailureCodes.AccessDenied => StatusCodes.Status403Forbidden,
                ShiftHandoverFailureCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };

            return TypedResults.Problem(
                title: result.FailureReason,
                statusCode: statusCode,
                extensions: ShiftHandoverEndpointHelpers.FailureExt(result.FailureCode!));
        }

        return onSuccess(result.EntityId!.Value);
    }
}

internal sealed record CreateShiftHandoverApiRequest(
    string ShiftDate,
    string ShiftPeriod,
    string Department,
    string? Summary,
    Guid? IncomingMembershipId);

internal sealed record UpdateShiftHandoverApiRequest(
    uint ExpectedVersion,
    string ShiftDate,
    string ShiftPeriod,
    string Department,
    string? Summary,
    Guid? IncomingMembershipId);

internal sealed record AddShiftHandoverItemApiRequest(
    string Category,
    string Title,
    string? Description,
    string Severity,
    string? SourceModule,
    string? SourceEntityId,
    bool RequiresFollowUp,
    string? EvidenceMetadataJson);

internal sealed record UpdateShiftHandoverItemApiRequest(
    uint ExpectedVersion,
    string Category,
    string Title,
    string? Description,
    string Severity,
    string Status,
    bool RequiresFollowUp,
    string? EvidenceMetadataJson);

internal sealed record ShiftHandoverLifecycleApiRequest(uint ExpectedVersion);

internal static class ShiftHandoverApiMapper
{
    public static object MapSummary(ShiftHandoverSummaryReadModel item) => new
    {
        id = item.Id,
        handoverReference = item.HandoverReference,
        shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        shiftPeriod = item.ShiftPeriod,
        department = item.Department,
        status = item.Status,
        outgoingLabel = item.OutgoingLabel,
        incomingLabel = item.IncomingLabel,
        unresolvedItemCount = item.UnresolvedItemCount,
        followUpItemCount = item.FollowUpItemCount,
        createdAt = item.CreatedAtUtc,
        handedOverAt = item.HandedOverAtUtc,
        deepLinkPath = ShiftHandoverPresentationBuilder.BuildDeepLink(item.Id),
    };

    public static object MapDetail(ShiftHandoverDetailReadModel detail) => new
    {
        id = detail.Id,
        handoverReference = detail.HandoverReference,
        shiftDate = detail.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        shiftPeriod = detail.ShiftPeriod,
        department = detail.Department,
        summary = detail.Summary,
        status = detail.Status,
        outgoingMembershipId = detail.OutgoingMembershipId,
        outgoingLabel = detail.OutgoingLabel,
        incomingMembershipId = detail.IncomingMembershipId,
        incomingLabel = detail.IncomingLabel,
        facilityName = detail.FacilityName,
        createdAt = detail.CreatedAtUtc,
        updatedAt = detail.UpdatedAtUtc,
        handedOverAt = detail.HandedOverAtUtc,
        acknowledgedAt = detail.AcknowledgedAtUtc,
        completedAt = detail.CompletedAtUtc,
        version = detail.Version,
        deepLinkPath = ShiftHandoverPresentationBuilder.BuildDeepLink(detail.Id),
        items = detail.Items.Select(MapItem).ToList(),
        events = detail.Events.Select(MapEvent).ToList(),
        capabilities = new
        {
            canRead = detail.Capabilities.CanRead,
            canManage = detail.Capabilities.CanManage,
            canAcknowledge = detail.Capabilities.CanAcknowledge,
            canCreateTask = detail.Capabilities.CanCreateTask,
        },
        automationDisclaimer = ShiftHandoverConstants.AutomationDisclaimer,
    };

    public static object MapItem(ShiftHandoverItemReadModel item) => new
    {
        id = item.Id,
        sortOrder = item.SortOrder,
        category = item.Category,
        title = item.Title,
        description = item.Description,
        severity = item.Severity,
        sourceModule = item.SourceModule,
        sourceEntityId = item.SourceEntityId,
        status = item.Status,
        requiresFollowUp = item.RequiresFollowUp,
        linkedAiTaskId = item.LinkedAiTaskId,
        evidenceMetadataJson = item.EvidenceMetadataJson,
        version = item.Version,
        createdAt = item.CreatedAtUtc,
        updatedAt = item.UpdatedAtUtc,
        taskDeepLinkPath = item.LinkedAiTaskId is Guid taskId ? $"/?task={taskId:D}" : null,
    };

    public static object MapEvent(ShiftHandoverEventReadModel evt) => new
    {
        id = evt.Id,
        eventType = evt.EventType,
        actorLabel = evt.ActorLabel,
        payloadJson = evt.PayloadJson,
        occurredAt = evt.OccurredAtUtc,
    };
}
