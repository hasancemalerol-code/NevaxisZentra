using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Teams;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Teams;

public static class TeamsDirectMessageEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamsDirectMessageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams/direct-messages").RequireAuthorization();
        group.MapGet("/recipients", ListRecipientsAsync);
        group.MapGet("/", ListConversationsAsync);
        group.MapPost("/", CreateConversationAsync);
        group.MapGet("/{conversationId:guid}", GetConversationAsync);
        group.MapPost("/{conversationId:guid}/messages", PostMessageAsync);
        group.MapPost("/{conversationId:guid}/read", MarkReadAsync);
        return endpoints;
    }

    private static async Task<IResult> ListRecipientsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsDirectMessageRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveDirectWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var recipients = await repository.ListRecipientsAsync(context, cancellationToken);
        return TypedResults.Ok(new
        {
            items = recipients.Select(recipient => new
            {
                tenantMembershipId = recipient.TenantMembershipId,
                displayLabel = recipient.DisplayLabel,
            }),
        });
    }

    private static async Task<IResult> ListConversationsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsDirectMessageRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveDirectReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveDirectMessageCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.ListConversationsAsync(context, capabilities, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(item => new
            {
                id = item.Id,
                otherParticipantMembershipId = item.OtherParticipantMembershipId,
                otherParticipantLabel = item.OtherParticipantLabel,
                latestMessagePreview = item.LatestMessagePreview,
                latestMessageAt = item.LatestMessageAtUtc,
                unreadCount = item.UnreadCount,
                createdAt = item.CreatedAtUtc,
            }),
            capabilities = new
            {
                canWrite = result.CanWrite,
            },
            disclaimer = result.Disclaimer,
        });
    }

    private static async Task<IResult> CreateConversationAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateTeamsDirectConversationRequestBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsDirectMessageRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveDirectWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveDirectMessageCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.CreateOrGetConversationAsync(
            context,
            capabilities,
            new CreateTeamsDirectConversationCommand(
                body.RecipientTenantMembershipId,
                body.IdempotencyKey ?? string.Empty),
            cancellationToken);

        return TeamsEndpointHelpers.MapDirectConversationWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> GetConversationAsync(
        Guid conversationId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsDirectMessageRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveDirectReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveDirectMessageCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var detail = await repository.GetConversationAsync(
            context,
            capabilities,
            conversationId,
            page ?? 1,
            pageSize ?? TeamsConstants.DefaultPageSize,
            cancellationToken);

        return detail is null
            ? TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.NotFound, "Konuşma bulunamadı veya erişim yok.")
            : TypedResults.Ok(TeamsEndpointHelpers.MapDirectConversationDetail(detail));
    }

    private static async Task<IResult> PostMessageAsync(
        Guid conversationId,
        [FromQuery] Guid? facilityId,
        [FromBody] PostTeamsDirectMessageRequestBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsDirectMessageRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveDirectWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveDirectMessageCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.PostMessageAsync(
            context,
            capabilities,
            new PostTeamsDirectMessageCommand(
                conversationId,
                body.Body ?? string.Empty,
                body.IdempotencyKey ?? string.Empty),
            cancellationToken);

        return TeamsEndpointHelpers.MapDirectMessageWriteResult(result);
    }

    private static async Task<IResult> MarkReadAsync(
        Guid conversationId,
        [FromQuery] Guid? facilityId,
        [FromBody] MarkTeamsDirectConversationReadRequestBody body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsDirectMessageRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveDirectWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveDirectMessageCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.MarkConversationReadAsync(
            context,
            capabilities,
            new MarkTeamsDirectConversationReadCommand(conversationId, body.LastReadMessageId),
            cancellationToken);

        return TeamsEndpointHelpers.MapDirectMessageWriteResult(result);
    }
}

public sealed record CreateTeamsDirectConversationRequestBody(
    Guid RecipientTenantMembershipId,
    string? IdempotencyKey);

public sealed record PostTeamsDirectMessageRequestBody(
    string? Body,
    string? IdempotencyKey);

public sealed record MarkTeamsDirectConversationReadRequestBody(Guid? LastReadMessageId);
