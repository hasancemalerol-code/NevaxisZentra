namespace CrevaFactOS.Persistence.Teams;

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
