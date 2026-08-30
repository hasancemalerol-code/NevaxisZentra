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
    public const string MessageTaskDraftLinked = "message-task-draft-linked";
}

public static class TeamsMessageTaskLinkPurposes
{
    public const string HumanReviewDraft = "human-review-draft";
}

public static class TeamsSourceModules
{
    public const string Teams = "teams";
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

    public const string MessageTaskDraftDisclaimer =
        "Görev taslağı yalnızca insan incelemesi için oluşturulur; mesaj otomatik üretim, satın alma, bakım, kalite veya başka bir operasyon işlemi başlatmaz.";

    public const string TeamsSourceModule = "teams";

    public const int MaxApprovalTitleLength = 160;
    public const int MaxApprovalRationaleLength = 2000;
    public const int MaxApprovalDecisionNoteLength = 1000;

    public const string ApprovalDecisionDisclaimer =
        "Onay isteği yalnızca insan kararı kaydeder; onay, ret veya değişiklik talebi otomatik üretim, satın alma, stok, bakım, kalite, finans veya görev işlemi başlatmaz.";

    public static string BuildHumanReviewDraftIdempotencyKey(Guid messageId) =>
        $"teams-message-task:{messageId:D}:human-review-draft";

    public static string BuildApprovalCreateIdempotencyPurpose(Guid channelId, string sourceKind, Guid sourceId) =>
        $"teams-approval:{channelId:D}:{sourceKind}:{sourceId:D}";
}

public static class TeamsApprovalStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string ChangesRequested = "changes-requested";
    public const string Cancelled = "cancelled";
}

public static class TeamsApprovalReviewerRoles
{
    public const string Reviewer = "reviewer";
    public const string FinalReviewer = "final-reviewer";
}

public static class TeamsApprovalDecisions
{
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string ChangesRequested = "changes-requested";
}

public static class TeamsApprovalSourceKinds
{
    public const string Message = "message";
    public const string AiTask = "ai-task";
}

public static class TeamsApprovalAuditEventTypes
{
    public const string RequestCreated = "approval-request-created";
    public const string ReviewerAssigned = "approval-reviewer-assigned";
    public const string DecisionRecorded = "approval-decision-recorded";
    public const string RequestCancelled = "approval-request-cancelled";
}
