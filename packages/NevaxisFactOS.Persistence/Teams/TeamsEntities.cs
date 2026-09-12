namespace NevaxisFactOS.Persistence.Teams;

public sealed class TeamsChannel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChannelType { get; set; } = TeamsChannelTypes.Department;
    public Guid? AiTaskId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<TeamsChannelMember> Members { get; set; } = [];
    public ICollection<TeamsMessage> Messages { get; set; } = [];
}

public sealed class TeamsChannelMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public string Role { get; set; } = TeamsChannelRoles.Member;
    public DateTimeOffset JoinedAtUtc { get; set; }
    public string AddedBy { get; set; } = string.Empty;

    public TeamsChannel? Channel { get; set; }
}

public sealed class TeamsMessage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid AuthorTenantMembershipId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? AiTaskId { get; set; }
    public string? SourceModule { get; set; }
    public string? SourceEntityId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string IdempotencyPayloadFingerprint { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamsChannel? Channel { get; set; }
    public ICollection<TeamsMessageMention> Mentions { get; set; } = [];
}

public sealed class TeamsMessageMention
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MessageId { get; set; }
    public Guid MentionedTenantMembershipId { get; set; }

    public TeamsMessage? Message { get; set; }
}

public sealed class TeamsChannelReadState
{
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset LastReadAtUtc { get; set; }
}

public sealed class TeamsMessageAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorTenantMembershipId { get; set; } = string.Empty;
    public string ActorLabel { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class TeamsMessageTaskLink
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid MessageId { get; set; }
    public Guid AiTaskId { get; set; }
    public string LinkPurpose { get; set; } = TeamsMessageTaskLinkPurposes.HumanReviewDraft;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string SourceContextReference { get; set; } = string.Empty;
    public Guid CreatedByTenantMembershipId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TeamsApprovalRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid RequestedByTenantMembershipId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? AiTaskId { get; set; }
    public string SourceKind { get; set; } = TeamsApprovalSourceKinds.Message;
    public string Title { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string Status { get; set; } = TeamsApprovalStatuses.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string IdempotencyPurpose { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }

    public ICollection<TeamsApprovalReviewer> Reviewers { get; set; } = [];
    public ICollection<TeamsApprovalDecision> Decisions { get; set; } = [];
}

public sealed class TeamsApprovalReviewer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RequestId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public string Role { get; set; } = TeamsApprovalReviewerRoles.Reviewer;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamsApprovalRequest? Request { get; set; }
}

public sealed class TeamsApprovalDecision
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RequestId { get; set; }
    public Guid ReviewerTenantMembershipId { get; set; }
    public string Decision { get; set; } = TeamsApprovalDecisions.Approved;
    public string? Note { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamsApprovalRequest? Request { get; set; }
}

public sealed class TeamsApprovalAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RequestId { get; set; }
    public Guid? ChannelId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorTenantMembershipId { get; set; } = string.Empty;
    public string ActorLabel { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public TeamsApprovalRequest? Request { get; set; }
}

public sealed class TeamsShiftHandover
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid AuthorTenantMembershipId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public string ShiftType { get; set; } = TeamsHandoverShiftTypes.Morning;
    public string Status { get; set; } = TeamsHandoverStatuses.Draft;
    public string Summary { get; set; } = string.Empty;
    public string SafetyNote { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string IdempotencyPurpose { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }

    public ICollection<TeamsShiftHandoverItem> Items { get; set; } = [];
    public ICollection<TeamsShiftHandoverAcknowledgement> Acknowledgements { get; set; } = [];
}

public sealed class TeamsShiftHandoverItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid HandoverId { get; set; }
    public int SortOrder { get; set; }
    public string ItemKind { get; set; } = TeamsHandoverItemKinds.Attention;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? MessageId { get; set; }
    public Guid? AiTaskId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamsShiftHandover? Handover { get; set; }
}

public sealed class TeamsShiftHandoverAcknowledgement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid HandoverId { get; set; }
    public Guid AcknowledgedByTenantMembershipId { get; set; }
    public DateTimeOffset AcknowledgedAtUtc { get; set; }

    public TeamsShiftHandover? Handover { get; set; }
}

public sealed class TeamsShiftHandoverAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid HandoverId { get; set; }
    public Guid? ChannelId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorTenantMembershipId { get; set; } = string.Empty;
    public string ActorLabel { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public TeamsShiftHandover? Handover { get; set; }
}

public sealed class TeamsDirectConversation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ParticipantLowMembershipId { get; set; }
    public Guid ParticipantHighMembershipId { get; set; }
    public Guid InitiatedByTenantMembershipId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string IdempotencyPurpose { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<TeamsDirectConversationMember> Members { get; set; } = [];
    public ICollection<TeamsDirectMessage> Messages { get; set; } = [];
}

public sealed class TeamsDirectConversationMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; }

    public TeamsDirectConversation? Conversation { get; set; }
}

public sealed class TeamsDirectMessage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid AuthorTenantMembershipId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string IdempotencyPayloadFingerprint { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamsDirectConversation? Conversation { get; set; }
}

public sealed class TeamsDirectReadState
{
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TenantMembershipId { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset LastReadAtUtc { get; set; }
}

public sealed class TeamsDirectMessageAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorTenantMembershipId { get; set; } = string.Empty;
    public string ActorLabel { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public TeamsDirectConversation? Conversation { get; set; }
}

public sealed class TeamsNotification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RecipientTenantMembershipId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public string SourceRouteKey { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string SourcePreview { get; set; } = string.Empty;
    public string SourceEventFingerprint { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}

public sealed class TeamsNotificationAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid NotificationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorTenantMembershipId { get; set; } = string.Empty;
    public string ActorLabel { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public TeamsNotification? Notification { get; set; }
}
