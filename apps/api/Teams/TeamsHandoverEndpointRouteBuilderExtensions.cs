using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Teams;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Teams;

public static class TeamsHandoverEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamsHandoverEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams/handovers").RequireAuthorization();
        group.MapGet("/", ListHandoversAsync);
        group.MapPost("/", CreateHandoverAsync);
        group.MapGet("/{handoverId:guid}", GetHandoverAsync);
        group.MapPost("/{handoverId:guid}", UpdateDraftAsync);
        group.MapPost("/{handoverId:guid}/publish", PublishHandoverAsync);
        group.MapPost("/{handoverId:guid}/acknowledgements", AcknowledgeHandoverAsync);
        return endpoints;
    }

    private static async Task<IResult> ListHandoversAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? channelId,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveHandoverReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveHandoverCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.ListHandoversAsync(
            context, capabilities, channelId, status, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(item => new
            {
                id = item.Id,
                channelId = item.ChannelId,
                channelName = item.ChannelName,
                shiftDate = item.ShiftDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                shiftType = item.ShiftType,
                status = item.Status,
                authorLabel = item.AuthorLabel,
                createdAt = item.CreatedAtUtc,
                publishedAt = item.PublishedAtUtc,
                itemCount = item.ItemCount,
                acknowledgementCount = item.AcknowledgementCount,
            }),
            capabilities = new
            {
                canCreate = result.CanCreate,
                canAcknowledge = result.CanAcknowledge,
                canManage = result.CanManage,
            },
            disclaimer = TeamsConstants.HandoverDisclaimer,
        });
    }

    private static async Task<IResult> GetHandoverAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveHandoverReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveHandoverCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var detail = await repository.GetHandoverAsync(context, capabilities, handoverId, cancellationToken);
        return detail is null
            ? TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.NotFound, "Vardiya devri bulunamadı veya erişim yok.")
            : TypedResults.Ok(TeamsEndpointHelpers.MapHandoverDetail(detail));
    }

    private static async Task<IResult> CreateHandoverAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateTeamsHandoverRequestBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveHandoverCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!DateOnly.TryParse(body.ShiftDate, out var shiftDate))
        {
            return TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.ValidationFailed, "Vardiya tarihi geçersiz.");
        }

        var capabilities = await TeamsEndpointHelpers.ResolveHandoverCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.CreateHandoverAsync(
            context,
            capabilities,
            new CreateTeamsHandoverCommand(
                body.ChannelId,
                shiftDate,
                body.ShiftType ?? string.Empty,
                body.Summary ?? string.Empty,
                body.SafetyNote ?? string.Empty,
                body.IdempotencyKey ?? string.Empty,
                MapItems(body.Items)),
            cancellationToken);

        return TeamsEndpointHelpers.MapHandoverWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateTeamsHandoverRequestBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveHandoverCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (!DateOnly.TryParse(body.ShiftDate, out var shiftDate))
        {
            return TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.ValidationFailed, "Vardiya tarihi geçersiz.");
        }

        var capabilities = await TeamsEndpointHelpers.ResolveHandoverCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.UpdateDraftAsync(
            context,
            capabilities,
            new UpdateTeamsHandoverCommand(
                handoverId,
                shiftDate,
                body.ShiftType ?? string.Empty,
                body.Summary ?? string.Empty,
                body.SafetyNote ?? string.Empty,
                MapItems(body.Items)),
            cancellationToken);

        return TeamsEndpointHelpers.MapHandoverWriteResult(result);
    }

    private static async Task<IResult> PublishHandoverAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveHandoverCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveHandoverCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.PublishHandoverAsync(
            context,
            capabilities,
            new PublishTeamsHandoverCommand(handoverId),
            cancellationToken);

        return TeamsEndpointHelpers.MapHandoverWriteResult(result);
    }

    private static async Task<IResult> AcknowledgeHandoverAsync(
        Guid handoverId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsHandoverRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveHandoverAcknowledgeAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveHandoverCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.AcknowledgeHandoverAsync(
            context,
            capabilities,
            new AcknowledgeTeamsHandoverCommand(handoverId),
            cancellationToken);

        return TeamsEndpointHelpers.MapHandoverWriteResult(result);
    }

    private static List<TeamsHandoverItemInput> MapItems(IReadOnlyList<TeamsHandoverItemRequestBody>? items) =>
        (items ?? [])
            .Select(item => new TeamsHandoverItemInput(
                item.ItemKind ?? string.Empty,
                item.Title ?? string.Empty,
                item.Body ?? string.Empty,
                item.MessageId,
                item.AiTaskId))
            .ToList();
}

public sealed record TeamsHandoverItemRequestBody(
    string? ItemKind,
    string? Title,
    string? Body,
    Guid? MessageId,
    Guid? AiTaskId);

public sealed record CreateTeamsHandoverRequestBody(
    Guid ChannelId,
    string? ShiftDate,
    string? ShiftType,
    string? Summary,
    string? SafetyNote,
    string? IdempotencyKey,
    IReadOnlyList<TeamsHandoverItemRequestBody>? Items);

public sealed record UpdateTeamsHandoverRequestBody(
    string? ShiftDate,
    string? ShiftType,
    string? Summary,
    string? SafetyNote,
    IReadOnlyList<TeamsHandoverItemRequestBody>? Items);
