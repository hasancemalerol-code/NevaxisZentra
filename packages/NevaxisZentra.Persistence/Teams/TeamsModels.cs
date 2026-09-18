namespace NevaxisZentra.Persistence.Teams;

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

public sealed record TeamsApprovalReviewerAssignment(
    Guid TenantMembershipId,
    string Role);

public sealed record CreateTeamsApprovalRequestCommand(
    Guid ChannelId,
    Guid? MessageId,
    Guid? AiTaskId,
    string Title,
    string Rationale,
    string IdempotencyKey,
    IReadOnlyList<TeamsApprovalReviewerAssignment> Reviewers);

public sealed record RecordTeamsApprovalDecisionCommand(
    Guid RequestId,
    string Decision,
    string? Note);

public sealed record CancelTeamsApprovalRequestCommand(Guid RequestId);

public sealed record AssignTeamsApprovalReviewerCommand(
    Guid RequestId,
    Guid TenantMembershipId,
    string Role);

public sealed record TeamsApprovalReviewerReadModel(
    Guid TenantMembershipId,
    string DisplayLabel,
    string Role);

public sealed record TeamsApprovalDecisionReadModel(
    Guid Id,
    Guid ReviewerTenantMembershipId,
    string ReviewerLabel,
    string Decision,
    string? Note,
    DateTimeOffset CreatedAtUtc);

public sealed record TeamsApprovalAuditEventReadModel(
    Guid Id,
    string EventType,
    string ActorLabel,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);

public sealed record TeamsApprovalSourceReadModel(
    string SourceKind,
    Guid? MessageId,
    Guid? AiTaskId,
    string ChannelName,
    string? Label,
    string? DeepLinkPath,
    bool CanOpen,
    string? UnavailableReason);

public sealed record TeamsApprovalSummaryReadModel(
    Guid Id,
    string Title,
    string Status,
    string ChannelName,
    Guid ChannelId,
    string RequestedByLabel,
    DateTimeOffset CreatedAtUtc,
    string SourceKind,
    bool CanOpen);

public sealed record TeamsApprovalDetailReadModel(
    Guid Id,
    string Title,
    string Rationale,
    string Status,
    Guid ChannelId,
    string ChannelName,
    Guid RequestedByTenantMembershipId,
    string RequestedByLabel,
    TeamsApprovalSourceReadModel Source,
    IReadOnlyList<TeamsApprovalReviewerReadModel> Reviewers,
    TeamsApprovalDecisionReadModel? Decision,
    IReadOnlyList<TeamsApprovalAuditEventReadModel> AuditEvents,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    bool CanDecide,
    bool CanCancel,
    bool CanManageReviewers,
    string Disclaimer);

public sealed record TeamsApprovalListReadModel(
    IReadOnlyList<TeamsApprovalSummaryReadModel> Items,
    bool CanCreate,
    bool CanReview,
    bool CanManage);

public sealed record TeamsApprovalWriteResult(
    bool Succeeded,
    Guid? RequestId,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason,
    TeamsApprovalDetailReadModel? Detail = null)
{
    public static TeamsApprovalWriteResult Success(
        Guid requestId,
        TeamsApprovalDetailReadModel detail,
        bool idempotentReplay = false) =>
        new(true, requestId, idempotentReplay, null, null, detail);

    public static TeamsApprovalWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, false, failureCode, failureReason);
}

public sealed record TeamsHandoverAccessCapabilities(
    bool CanRead,
    bool CanCreate,
    bool CanAcknowledge,
    bool CanManage,
    bool CanReadAiTasks);

public sealed record TeamsHandoverItemInput(
    string ItemKind,
    string Title,
    string Body,
    Guid? MessageId,
    Guid? AiTaskId);

public sealed record CreateTeamsHandoverCommand(
    Guid ChannelId,
    DateOnly ShiftDate,
    string ShiftType,
    string Summary,
    string SafetyNote,
    string IdempotencyKey,
    IReadOnlyList<TeamsHandoverItemInput> Items);

public sealed record UpdateTeamsHandoverCommand(
    Guid HandoverId,
    DateOnly ShiftDate,
    string ShiftType,
    string Summary,
    string SafetyNote,
    IReadOnlyList<TeamsHandoverItemInput> Items);

public sealed record PublishTeamsHandoverCommand(Guid HandoverId);

public sealed record AcknowledgeTeamsHandoverCommand(Guid HandoverId);

public sealed record TeamsHandoverItemReadModel(
    Guid Id,
    string ItemKind,
    string Title,
    string Body,
    Guid? MessageId,
    Guid? AiTaskId,
    string? SourceKind,
    string? SourceLabel,
    string? DeepLinkPath,
    bool CanOpen,
    string? UnavailableReason);

public sealed record TeamsHandoverAcknowledgementReadModel(
    Guid TenantMembershipId,
    string DisplayLabel,
    DateTimeOffset AcknowledgedAtUtc);

public sealed record TeamsHandoverAuditEventReadModel(
    Guid Id,
    string EventType,
    string ActorLabel,
    DateTimeOffset OccurredAtUtc);

public sealed record TeamsHandoverSummaryReadModel(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    DateOnly ShiftDate,
    string ShiftType,
    string Status,
    string AuthorLabel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    int ItemCount,
    int AcknowledgementCount);

public sealed record TeamsHandoverDetailReadModel(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    Guid AuthorTenantMembershipId,
    string AuthorLabel,
    DateOnly ShiftDate,
    string ShiftType,
    string Status,
    string Summary,
    string SafetyNote,
    IReadOnlyList<TeamsHandoverItemReadModel> Items,
    IReadOnlyList<TeamsHandoverAcknowledgementReadModel> Acknowledgements,
    IReadOnlyList<TeamsHandoverAuditEventReadModel> AuditEvents,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    bool CanEdit,
    bool CanPublish,
    bool CanAcknowledge,
    string Disclaimer);

public sealed record TeamsHandoverListReadModel(
    IReadOnlyList<TeamsHandoverSummaryReadModel> Items,
    bool CanCreate,
    bool CanAcknowledge,
    bool CanManage);

public sealed record TeamsHandoverWriteResult(
    bool Succeeded,
    Guid? HandoverId,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason,
    TeamsHandoverDetailReadModel? Detail = null)
{
    public static TeamsHandoverWriteResult Success(
        Guid handoverId,
        TeamsHandoverDetailReadModel detail,
        bool idempotentReplay = false) =>
        new(true, handoverId, idempotentReplay, null, null, detail);

    public static TeamsHandoverWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, false, failureCode, failureReason);
}

public sealed record TeamsWorkInboxAccessCapabilities(
    bool CanReadAiTasks,
    bool CanReviewAiTasks,
    bool CanReviewApprovals,
    bool CanAcknowledgeHandovers,
    bool CanReadMentions);

public sealed record TeamsWorkInboxItemReadModel(
    string SourceKind,
    Guid SourceId,
    string Title,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    string DeepLinkPath);

public sealed record TeamsWorkInboxSourceAvailabilityReadModel(
    bool AiTasks,
    bool Approvals,
    bool Handovers,
    bool Mentions);

public sealed record TeamsWorkInboxReadModel(
    IReadOnlyList<TeamsWorkInboxItemReadModel> Items,
    TeamsWorkInboxSourceAvailabilityReadModel Sources,
    string Disclaimer);

public sealed record TeamsDirectMessageAccessCapabilities(
    bool CanRead,
    bool CanWrite,
    bool CanManage);

public sealed record CreateTeamsDirectConversationCommand(
    Guid RecipientTenantMembershipId,
    string IdempotencyKey);

public sealed record PostTeamsDirectMessageCommand(
    Guid ConversationId,
    string Body,
    string IdempotencyKey);

public sealed record MarkTeamsDirectConversationReadCommand(
    Guid ConversationId,
    Guid? LastReadMessageId);

public sealed record TeamsDirectRecipientReadModel(
    Guid TenantMembershipId,
    string DisplayLabel);

public sealed record TeamsDirectMessageReadModel(
    Guid Id,
    Guid AuthorTenantMembershipId,
    string AuthorLabel,
    string Body,
    DateTimeOffset CreatedAtUtc,
    bool IsOwnMessage);

public sealed record TeamsDirectConversationSummaryReadModel(
    Guid Id,
    Guid OtherParticipantMembershipId,
    string OtherParticipantLabel,
    string? LatestMessagePreview,
    DateTimeOffset? LatestMessageAtUtc,
    int UnreadCount,
    DateTimeOffset CreatedAtUtc);

public sealed record TeamsDirectConversationDetailReadModel(
    Guid Id,
    Guid OtherParticipantMembershipId,
    string OtherParticipantLabel,
    IReadOnlyList<TeamsDirectMessageReadModel> Messages,
    int TotalMessageCount,
    int Page,
    int PageSize,
    Guid? LastReadMessageId,
    DateTimeOffset? LastReadAtUtc,
    bool CanWrite,
    string Disclaimer);

public sealed record TeamsDirectConversationListReadModel(
    IReadOnlyList<TeamsDirectConversationSummaryReadModel> Items,
    bool CanWrite,
    string Disclaimer);

public sealed record TeamsDirectConversationWriteResult(
    bool Succeeded,
    Guid? ConversationId,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason,
    TeamsDirectConversationDetailReadModel? Detail = null)
{
    public static TeamsDirectConversationWriteResult Success(
        Guid conversationId,
        TeamsDirectConversationDetailReadModel detail,
        bool idempotentReplay = false) =>
        new(true, conversationId, idempotentReplay, null, null, detail);

    public static TeamsDirectConversationWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, false, failureCode, failureReason);
}

public sealed record TeamsDirectMessageWriteResult(
    bool Succeeded,
    Guid? MessageId,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason,
    TeamsDirectConversationDetailReadModel? Detail = null)
{
    public static TeamsDirectMessageWriteResult Success(
        Guid messageId,
        TeamsDirectConversationDetailReadModel detail,
        bool idempotentReplay = false) =>
        new(true, messageId, idempotentReplay, null, null, detail);

    public static TeamsDirectMessageWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, false, failureCode, failureReason);
}

public sealed record TeamsNotificationAccessCapabilities(bool CanRead);

public sealed record MarkTeamsNotificationReadCommand(Guid NotificationId);

public sealed record TeamsNotificationReadModel(
    Guid Id,
    string NotificationType,
    string SourceModule,
    string SourceEntityId,
    string DeepLinkPath,
    string Title,
    string Reason,
    string? SourcePreview,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc,
    bool CanOpen,
    string? UnavailableReason);

public sealed record TeamsNotificationListReadModel(
    IReadOnlyList<TeamsNotificationReadModel> Items,
    int UnreadCount,
    string Disclaimer);

public sealed record TeamsNotificationWriteResult(
    bool Succeeded,
    Guid? NotificationId,
    bool IdempotentReplay,
    string? FailureCode,
    string? FailureReason)
{
    public static TeamsNotificationWriteResult Success(Guid notificationId, bool idempotentReplay = false) =>
        new(true, notificationId, idempotentReplay, null, null);

    public static TeamsNotificationWriteResult Fail(string failureCode, string failureReason) =>
        new(false, null, false, failureCode, failureReason);
}
