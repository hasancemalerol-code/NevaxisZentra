namespace CrevaFactOS.Persistence.Teams;

public static class TeamsChannelTypes
{
    public const string Department = "department";
    public const string Operational = "operational";
    public const string TaskContext = "task-context";
}

public static class TeamsChannelRoles
{
    public const string Owner = "owner";
    public const string Moderator = "moderator";
    public const string Member = "member";
}

public static class TeamsAuditEventTypes
{
    public const string ChannelCreated = "channel-created";
    public const string MemberAdded = "member-added";
    public const string MemberRemoved = "member-removed";
    public const string MessageCreated = "message-created";
}

public static class TeamsFailureCodes
{
    public const string NotFound = "teams.not-found";
    public const string ValidationFailed = "teams.validation-failed";
    public const string AccessDenied = "teams.access-denied";
    public const string ChannelAccessDenied = "teams.channel-access-denied";
    public const string IdempotencyConflict = "teams.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "teams.idempotency-legacy-unverifiable";
    public const string FacilityScopeDenied = "teams.facility-scope-denied";
}

public static class TeamsConstants
{
    public const int MaxMessageBodyLength = 4000;
    public const int MaxChannelNameLength = 120;
    public const int MaxPageSize = 50;
    public const int DefaultPageSize = 25;

    public const string DevelopmentPilotProvenance =
        "Bu alan operasyon iletişimi ve insan incelemesi içindir; mesaj göndermek otomatik üretim, satın alma, bakım veya kalite işlemi başlatmaz.";
}
