namespace NevaxisFactOS.Persistence.Teams;

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

    public const int MaxHandoverSummaryLength = 2000;
    public const int MaxHandoverSafetyNoteLength = 2000;
    public const int MaxHandoverItemTitleLength = 160;
    public const int MaxHandoverItemBodyLength = 2000;
    public const int MaxHandoverItems = 20;

    public const string HandoverDisclaimer =
        "Vardiya devri yalnızca operasyon bağlamı kaydeder; görev, onay, üretim, satın alma, stok, bakım, kalite veya finans işlemi başlatmaz.";

    public static string BuildHandoverCreateIdempotencyPurpose(
        Guid channelId,
        DateOnly shiftDate,
        string shiftType) =>
        $"teams-handover:{channelId:D}:{shiftDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}:{shiftType}";

    public const int MaxInboxItems = 50;

    public const string WorkInboxDisclaimer =
        "İş kutusu yalnızca mevcut kaynaklardaki size ait işleri bir arada gösterir; görev, onay, vardiya devri veya mesaj işlemi başlatmaz.";

    public const string DirectMessageDisclaimer =
        "Özel mesajlar yalnızca aynı tesis kapsamındaki üyeler arasında kalıcı iletişim sağlar; görev, onay, operasyon işlemi veya dış teslimat başlatmaz.";

    public const int MaxNotificationPreviewLength = 160;
    public const int MaxNotificationRouteKeyLength = 240;
    public const int MaxNotificationFingerprintLength = 200;

    public const string NotificationDisclaimer =
        "Bildirimler yalnızca erişebildiğiniz Teams kaynaklarına yönlendirir; görev, onay, vardiya devri veya mesaj işlemi başlatmaz.";
}

public static class TeamsNotificationTypes
{
    public const string DirectMessage = "direct-message";
    public const string Mention = "mention";
    public const string ApprovalReview = "approval-review";
    public const string HandoverAcknowledgement = "handover-acknowledgement";
    public const string TaskLink = "task-link";
    public const string TaskAssigned = "task-assigned";
    public const string TaskEscalated = "task-escalated";
    public const string TaskSlaWarning = "task-sla-warning";
    public const string TaskClosureApprovalRequested = "task-closure-approval-requested";
    public const string TaskClosureApproved = "task-closure-approved";
    public const string TaskClosureRejected = "task-closure-rejected";
    public const string TaskReopened = "task-reopened";
    public const string FactoryShiftHandover = "factory-shift-handover";
    public const string FactoryShiftHandoverAcknowledged = "factory-shift-handover-acknowledged";
}

public static class TeamsNotificationSourceModules
{
    public const string Teams = "teams";
    public const string TeamsDirect = "teams-direct";
    public const string AiTask = "ai-task";
    public const string ShiftHandover = "shift-handover";
}

public static class TeamsNotificationAuditEventTypes
{
    public const string Created = "notification-created";
    public const string Read = "notification-read";
}

public static class TeamsNotificationUnavailableReasons
{
    public const string SourceInaccessible = "Kaynak artık erişilebilir değil.";
}

public static class TeamsWorkInboxSourceKinds
{
    public const string AiTask = "ai-task";
    public const string Approval = "approval";
    public const string Handover = "handover";
    public const string Mention = "mention";
}

public static class TeamsHandoverStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
}

public static class TeamsHandoverShiftTypes
{
    public const string Morning = "morning";
    public const string Evening = "evening";
    public const string Night = "night";
    public const string Custom = "custom";
}

public static class TeamsHandoverItemKinds
{
    public const string Attention = "attention";
    public const string Incident = "incident";
    public const string PendingWork = "pending-work";
    public const string QualityNote = "quality-note";
    public const string MaintenanceNote = "maintenance-note";
    public const string MaterialNote = "material-note";
}

public static class TeamsHandoverAuditEventTypes
{
    public const string Created = "handover-created";
    public const string Updated = "handover-updated";
    public const string Published = "handover-published";
    public const string Acknowledged = "handover-acknowledged";
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

public static class TeamsDirectMessageAuditEventTypes
{
    public const string ConversationCreated = "direct-conversation-created";
    public const string MessageCreated = "direct-message-created";
    public const string ReadStateAdvanced = "direct-read-state-advanced";
}
