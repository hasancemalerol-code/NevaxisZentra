namespace CrevaFactOS.Persistence.Teams;

public sealed record TeamsOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record TeamsMemberReadModel(
    Guid TenantMembershipId,
    string DisplayLabel,
    string Role);

public sealed record TeamsMessageMentionReadModel(
    Guid TenantMembershipId,
    string DisplayLabel);

public sealed record TeamsSourceContextReadModel(
    string ContextType,
    string? Label,
    string? DeepLinkPath,
    Guid? AiTaskId,
    string? SourceModule,
    string? SourceEntityId,
    bool CanOpen);

public sealed record TeamsMessageLinkedTaskReadModel(
    Guid TaskId,
    string TaskReference,
    string TaskStatus,
    string TaskDeepLinkPath,
    bool CanOpenTask);

public sealed record TeamsMessageReadModel(
    Guid Id,
    Guid ChannelId,
    Guid AuthorTenantMembershipId,
    string AuthorLabel,
    string Body,
    TeamsSourceContextReadModel? SourceContext,
    IReadOnlyList<TeamsMessageMentionReadModel> Mentions,
    DateTimeOffset CreatedAtUtc,
    TeamsMessageLinkedTaskReadModel? LinkedTask = null);

public sealed record TeamsLatestMessagePreviewReadModel(
    Guid MessageId,
    string AuthorLabel,
    string BodyPreview,
    DateTimeOffset CreatedAtUtc);

public sealed record TeamsChannelSummaryReadModel(
    Guid Id,
    string Name,
    string ChannelType,
    Guid? AiTaskId,
    int MemberCount,
    int UnreadCount,
    TeamsLatestMessagePreviewReadModel? LatestMessage,
    DateTimeOffset? LastReadAtUtc,
    string CurrentMemberRole);

public sealed record TeamsChannelDetailReadModel(
    Guid Id,
    string Name,
    string ChannelType,
    Guid? AiTaskId,
    TeamsSourceContextReadModel? TaskContext,
    IReadOnlyList<TeamsMemberReadModel> Members,
    IReadOnlyList<TeamsMessageReadModel> Messages,
    int TotalMessageCount,
    int Page,
    int PageSize,
    DateTimeOffset? LastReadAtUtc,
    Guid? LastReadMessageId,
    string CurrentMemberRole);

public sealed record TeamsTaskContextChannelReadModel(
    Guid ChannelId,
    string ChannelName,
    string TeamsDeepLinkPath,
    bool CanOpenTask,
    string? TaskDeepLinkPath,
    string? TaskTitle);

public sealed record TeamsChannelListReadModel(
    IReadOnlyList<TeamsChannelSummaryReadModel> Items,
    bool CanManage,
    bool CanWrite);

public sealed record CreateTeamsChannelCommand(
    string Name,
    string ChannelType,
    Guid? AiTaskId,
    IReadOnlyList<Guid> InitialMemberMembershipIds);

public sealed record AddTeamsChannelMemberCommand(
    Guid ChannelId,
    Guid TenantMembershipId,
    string Role);

public sealed record RemoveTeamsChannelMemberCommand(
    Guid ChannelId,
    Guid TenantMembershipId);

public sealed record PostTeamsMessageCommand(
    Guid ChannelId,
    string Body,
    string IdempotencyKey,
    Guid? AiTaskId,
    string? SourceModule,
    string? SourceEntityId,
    IReadOnlyList<Guid> MentionedMembershipIds);

public sealed record MarkTeamsChannelReadCommand(
    Guid ChannelId,
    Guid? LastReadMessageId);

public sealed record CreateMessageTaskDraftCommand(
    Guid MessageId,
    string Title,
    string Category,
    string Priority,
    string? HumanNote);

public sealed record TeamsMessageSourceContextReadModel(
    Guid MessageId,
    Guid ChannelId,
    string ChannelName,
    string MessageBodyPreview,
    string TeamsDeepLinkPath,
    bool CanOpen,
    string? UnavailableReason);

public sealed record TeamsMessageTaskDraftWriteResult(
    bool Succeeded,
    Guid? TaskId,
    string? TaskReference,
    string? TaskStatus,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason)
{
    public static TeamsMessageTaskDraftWriteResult Success(
        Guid taskId,
        string taskReference,
        string taskStatus,
        bool idempotentReplay = false) =>
        new(true, taskId, taskReference, taskStatus, idempotentReplay, null, null);

    public static TeamsMessageTaskDraftWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, null, null, false, failureCode, failureReason);
}

public sealed record TeamsWriteResult(
    bool Succeeded,
    Guid? EntityId,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason,
    TeamsMessageReadModel? Message = null,
    TeamsChannelDetailReadModel? ChannelDetail = null)
{
    public static TeamsWriteResult Success(
        Guid entityId,
        bool idempotentReplay = false,
        TeamsMessageReadModel? message = null,
        TeamsChannelDetailReadModel? channelDetail = null) =>
        new(true, entityId, idempotentReplay, null, null, message, channelDetail);

    public static TeamsWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, false, failureCode, failureReason);
}

public sealed record TeamsPagedMessagesReadModel(
    IReadOnlyList<TeamsMessageReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);
