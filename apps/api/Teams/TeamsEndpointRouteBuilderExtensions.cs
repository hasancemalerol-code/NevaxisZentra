using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Teams;

public static class TeamsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTeamsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teams/channels").RequireAuthorization();

        group.MapGet("/", ListChannelsAsync);
        group.MapGet("/by-task/{taskId:guid}", GetTaskContextChannelAsync);
        group.MapGet("/{channelId:guid}", GetChannelAsync);
        group.MapPost("/", CreateChannelAsync);
        group.MapPost("/{channelId:guid}/members", AddMemberAsync);
        group.MapDelete("/{channelId:guid}/members/{membershipId:guid}", RemoveMemberAsync);
        group.MapPost("/{channelId:guid}/messages", PostMessageAsync);
        group.MapPost("/{channelId:guid}/read", MarkReadAsync);

        var messagesGroup = endpoints.MapGroup("/api/teams/messages").RequireAuthorization();
        messagesGroup.MapPost("/{messageId:guid}/task-draft", CreateMessageTaskDraftAsync);
        messagesGroup.MapGet("/{messageId:guid}/source-context", GetMessageSourceContextAsync);

        return endpoints;
    }

    private static async Task<IResult> ListChannelsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var (canWrite, canManage) = await TeamsEndpointHelpers.ResolveMutationAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.ListAccessibleChannelsAsync(context, capabilities, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapChannelSummary),
            capabilities = new
            {
                canRead = true,
                canWrite,
                canManage,
            },
            provenanceDisclaimer = TeamsConstants.DevelopmentPilotProvenance,
        });
    }

    private static async Task<IResult> GetChannelAsync(
        Guid channelId,
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = TeamsConstants.DefaultPageSize,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ITeamsRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var detail = await repository.GetChannelDetailAsync(
            context, capabilities, channelId, page, pageSize, cancellationToken);

        return detail is null
            ? TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.NotFound, "Kanal bulunamadı veya erişim yok.")
            : TypedResults.Ok(MapChannelDetail(detail));
    }

    private static async Task<IResult> GetTaskContextChannelAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.GetTaskContextChannelAsync(
            context, capabilities, taskId, cancellationToken);

        return result is null
            ? TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.NotFound, "Görev bağlam kanalı bulunamadı.")
            : TypedResults.Ok(new
            {
                channelId = result.ChannelId,
                channelName = result.ChannelName,
                teamsDeepLinkPath = result.TeamsDeepLinkPath,
                canOpenTask = result.CanOpenTask,
                taskDeepLinkPath = result.TaskDeepLinkPath,
                taskTitle = result.TaskTitle,
            });
    }

    private static async Task<IResult> CreateChannelAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateTeamsChannelRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateChannelAsync(
            context,
            new CreateTeamsChannelCommand(
                body.Name,
                body.ChannelType,
                body.AiTaskId,
                body.InitialMemberMembershipIds ?? []),
            cancellationToken);

        return TeamsEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> AddMemberAsync(
        Guid channelId,
        [FromQuery] Guid? facilityId,
        [FromBody] AddTeamsChannelMemberRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.AddMemberAsync(
            context,
            new AddTeamsChannelMemberCommand(channelId, body.TenantMembershipId, body.Role),
            cancellationToken);

        return TeamsEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid channelId,
        Guid membershipId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RemoveMemberAsync(
            context,
            new RemoveTeamsChannelMemberCommand(channelId, membershipId),
            cancellationToken);

        return TeamsEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> PostMessageAsync(
        Guid channelId,
        [FromQuery] Guid? facilityId,
        [FromBody] PostTeamsMessageRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var capabilities = await TeamsEndpointHelpers.ResolveCapabilitiesAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var result = await repository.PostMessageAsync(
            context,
            capabilities,
            new PostTeamsMessageCommand(
                channelId,
                body.Body,
                body.IdempotencyKey,
                body.AiTaskId,
                body.SourceModule,
                body.SourceEntityId,
                body.MentionedMembershipIds ?? []),
            cancellationToken);

        return TeamsEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> MarkReadAsync(
        Guid channelId,
        [FromQuery] Guid? facilityId,
        [FromBody] MarkTeamsChannelReadRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.MarkChannelReadAsync(
            context,
            new MarkTeamsChannelReadCommand(channelId, body.LastReadMessageId),
            cancellationToken);

        return TeamsEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> CreateMessageTaskDraftAsync(
        Guid messageId,
        [FromQuery] Guid? facilityId,
        [FromBody] CreateMessageTaskDraftRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (teamsContext, teamsFailure) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (teamsContext is null) return teamsFailure!;

        var (aiTaskContext, aiTaskFailure) = await TeamsEndpointHelpers.ResolveAiTaskCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (aiTaskContext is null) return aiTaskFailure!;

        var result = await repository.CreateMessageTaskDraftAsync(
            teamsContext,
            aiTaskContext,
            new CreateMessageTaskDraftCommand(
                messageId,
                body.Title,
                body.Category,
                body.Priority,
                body.HumanNote),
            cancellationToken);

        return TeamsEndpointHelpers.MapMessageTaskDraftResult(result);
    }

    private static async Task<IResult> GetMessageSourceContextAsync(
        Guid messageId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ITeamsRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await TeamsEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var sourceContext = await repository.GetMessageSourceContextAsync(
            context,
            messageId,
            cancellationToken);

        return sourceContext is null
            ? TeamsEndpointHelpers.MapFailure(TeamsFailureCodes.NotFound, "Mesaj bulunamadı.")
            : TypedResults.Ok(new
            {
                messageId = sourceContext.MessageId,
                channelId = sourceContext.ChannelId,
                channelName = sourceContext.ChannelName,
                messageBodyPreview = sourceContext.MessageBodyPreview,
                teamsDeepLinkPath = sourceContext.TeamsDeepLinkPath,
                canOpen = sourceContext.CanOpen,
                unavailableReason = sourceContext.UnavailableReason,
            });
    }

    private static object MapChannelSummary(TeamsChannelSummaryReadModel channel) => new
    {
        id = channel.Id,
        name = channel.Name,
        channelType = channel.ChannelType,
        aiTaskId = channel.AiTaskId,
        memberCount = channel.MemberCount,
        unreadCount = channel.UnreadCount,
        latestMessage = channel.LatestMessage is null ? null : new
        {
            messageId = channel.LatestMessage.MessageId,
            authorLabel = channel.LatestMessage.AuthorLabel,
            bodyPreview = channel.LatestMessage.BodyPreview,
            createdAt = channel.LatestMessage.CreatedAtUtc,
        },
        lastReadAt = channel.LastReadAtUtc,
        currentMemberRole = channel.CurrentMemberRole,
    };

    private static object MapChannelDetail(TeamsChannelDetailReadModel detail) => new
    {
        id = detail.Id,
        name = detail.Name,
        channelType = detail.ChannelType,
        aiTaskId = detail.AiTaskId,
        taskContext = detail.TaskContext is null ? null : new
        {
            contextType = detail.TaskContext.ContextType,
            label = detail.TaskContext.Label,
            deepLinkPath = detail.TaskContext.DeepLinkPath,
            aiTaskId = detail.TaskContext.AiTaskId,
            canOpen = detail.TaskContext.CanOpen,
        },
        members = detail.Members.Select(m => new
        {
            tenantMembershipId = m.TenantMembershipId,
            displayLabel = m.DisplayLabel,
            role = m.Role,
        }),
        messages = detail.Messages.Select(TeamsEndpointHelpers.MapMessage),
        totalMessageCount = detail.TotalMessageCount,
        page = detail.Page,
        pageSize = detail.PageSize,
        lastReadAt = detail.LastReadAtUtc,
        lastReadMessageId = detail.LastReadMessageId,
        currentMemberRole = detail.CurrentMemberRole,
    };
}

public sealed record CreateTeamsChannelRequest(
    string Name,
    string ChannelType,
    Guid? AiTaskId,
    IReadOnlyList<Guid>? InitialMemberMembershipIds);

public sealed record AddTeamsChannelMemberRequest(Guid TenantMembershipId, string Role);

public sealed record PostTeamsMessageRequest(
    string Body,
    string IdempotencyKey,
    Guid? AiTaskId,
    string? SourceModule,
    string? SourceEntityId,
    IReadOnlyList<Guid>? MentionedMembershipIds);

public sealed record MarkTeamsChannelReadRequest(Guid? LastReadMessageId);

public sealed record CreateMessageTaskDraftRequest(
    string Title,
    string Category,
    string Priority,
    string? HumanNote);

public static class TeamsServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSTeams(this IServiceCollection services)
    {
        services.AddScoped<ITeamsRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableTeamsRepository()
                : new TeamsRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IAiTaskRepository>());
        });

        return services;
    }
}
