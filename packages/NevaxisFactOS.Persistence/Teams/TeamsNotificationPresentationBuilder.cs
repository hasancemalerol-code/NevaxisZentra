using System.Globalization;

namespace NevaxisFactOS.Persistence.Teams;

public static class TeamsNotificationPresentationBuilder
{
    public static string DirectMessageReason => "Yeni özel mesaj";
    public static string MentionReason => "Sizi etiketleyen kanal mesajı";
    public static string ApprovalReviewReason => "İncelemenizi bekleyen onay isteği";
    public static string HandoverAcknowledgementReason => "Teslim almanız gereken vardiya devri";
    public static string TaskLinkReason => "Teams mesajına bağlı AI görevi";
    public static string TaskAssignedReason => "Size atanan AI görevi";
    public static string TaskEscalatedReason => "Eskale edilen AI görevi";
    public static string TaskSlaWarningReason => "SLA riski taşıyan AI görevi";
    public static string TaskClosureApprovalRequestedReason => "Kapanış onayı bekleyen AI görevi";
    public static string TaskClosureApprovedReason => "Kapanışı onaylanan AI görevi";
    public static string TaskClosureRejectedReason => "Kapanışı reddedilen AI görevi";
    public static string TaskReopenedReason => "Yeniden açılan AI görevi";
    public static string FactoryShiftHandoverReason => "Yeni vardiya devir teslimi";
    public static string FactoryShiftHandoverAcknowledgedReason => "Vardiya devri teslim alındı";

    public static string BuildDirectMessageFingerprint(Guid messageId, Guid recipientMembershipId) =>
        $"direct-message:{messageId:D}:{recipientMembershipId:D}";

    public static string BuildMentionFingerprint(Guid messageId, Guid recipientMembershipId) =>
        $"mention:{messageId:D}:{recipientMembershipId:D}";

    public static string BuildApprovalReviewFingerprint(Guid requestId, Guid recipientMembershipId) =>
        $"approval-review:{requestId:D}:{recipientMembershipId:D}";

    public static string BuildHandoverAcknowledgementFingerprint(Guid handoverId, Guid recipientMembershipId) =>
        $"handover-acknowledgement:{handoverId:D}:{recipientMembershipId:D}";

    public static string BuildFactoryShiftHandoverFingerprint(Guid handoverId, Guid recipientMembershipId) =>
        $"factory-shift-handover:{handoverId:D}:{recipientMembershipId:D}";

    public static string BuildFactoryShiftHandoverAcknowledgedFingerprint(Guid handoverId, Guid recipientMembershipId) =>
        $"factory-shift-handover-acknowledged:{handoverId:D}:{recipientMembershipId:D}";

    public static string BuildFactoryShiftHandoverRoute(Guid handoverId) =>
        $"/vardiya-devir/{handoverId:D}";

    public static string BuildTaskLinkFingerprint(Guid linkId, Guid recipientMembershipId) =>
        $"task-link:{linkId:D}:{recipientMembershipId:D}";

    public static string BuildTaskAssignedFingerprint(Guid taskId, Guid recipientMembershipId) =>
        $"task-assigned:{taskId:D}:{recipientMembershipId:D}";

    public static string BuildTaskEscalatedFingerprint(Guid taskId, int escalationLevel, Guid recipientMembershipId) =>
        $"task-escalated:{taskId:D}:{escalationLevel}:{recipientMembershipId:D}";

    public static string BuildTaskSlaWarningFingerprint(Guid taskId, Guid recipientMembershipId) =>
        $"task-sla-warning:{taskId:D}:{recipientMembershipId:D}";

    public static string BuildDirectMessageRoute(Guid conversationId) =>
        $"/teams/ozel-mesajlar?conversation={conversationId:D}";

    public static string BuildMentionRoute(Guid channelId, Guid messageId) =>
        TeamsWorkInboxPresentationBuilder.BuildMentionDeepLink(channelId, messageId);

    public static string BuildApprovalRoute(Guid requestId) =>
        TeamsWorkInboxPresentationBuilder.BuildApprovalDeepLink(requestId);

    public static string BuildHandoverRoute(Guid handoverId) =>
        TeamsWorkInboxPresentationBuilder.BuildHandoverDeepLink(handoverId);

    public static string BuildTaskLinkRoute(Guid taskId) =>
        TeamsWorkInboxPresentationBuilder.BuildAiTaskDeepLink(taskId);

    public static string BuildDirectMessageTitle(string authorLabel) =>
        $"{authorLabel} size özel mesaj gönderdi";

    public static string BuildMentionTitle(string channelName) =>
        $"{channelName} kanalında etiketlendiniz";

    public static string BuildApprovalReviewTitle(string requestTitle) =>
        requestTitle;

    public static string BuildHandoverTitle(DateOnly shiftDate, string shiftType) =>
        $"{shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {ResolveShiftLabel(shiftType)} vardiya devri";

    public static string BuildFactoryShiftHandoverTitle(DateOnly shiftDate, string shiftPeriod, int unresolvedCount) =>
        $"{shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {ResolveShiftLabel(shiftPeriod)} devir — {unresolvedCount} açık madde";

    public static string BuildTaskLinkTitle(string taskReference) =>
        $"Teams mesajına bağlı görev: {taskReference}";

    public static string BuildTaskAssignedTitle(string taskReference) =>
        $"Size atanan görev: {taskReference}";

    public static string BuildTaskEscalatedTitle(string taskReference, int escalationLevel) =>
        $"Eskale görev (seviye {escalationLevel}): {taskReference}";

    public static string BuildTaskClosureApprovalRequestedTitle(string taskReference) =>
        $"Kapanış onayı bekliyor: {taskReference}";

    public static string BuildTaskClosureApprovedTitle(string taskReference) =>
        $"Kapanış onaylandı: {taskReference}";

    public static string BuildTaskClosureRejectedTitle(string taskReference) =>
        $"Kapanış reddedildi: {taskReference}";

    public static string BuildTaskReopenedTitle(string taskReference) =>
        $"Görev yeniden açıldı: {taskReference}";

    public static string BuildTaskClosureApprovalRequestedFingerprint(Guid taskId, Guid recipientMembershipId) =>
        $"task-closure-approval-requested:{taskId:D}:{recipientMembershipId:D}";

    public static string BuildTaskClosureApprovedFingerprint(Guid taskId, Guid recipientMembershipId) =>
        $"task-closure-approved:{taskId:D}:{recipientMembershipId:D}";

    public static string BuildTaskClosureRejectedFingerprint(Guid taskId, Guid recipientMembershipId) =>
        $"task-closure-rejected:{taskId:D}:{recipientMembershipId:D}";

    public static string BuildTaskReopenedFingerprint(Guid taskId, Guid recipientMembershipId) =>
        $"task-reopened:{taskId:D}:{recipientMembershipId:D}";

    public static string TruncatePreview(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Length <= TeamsConstants.MaxNotificationPreviewLength
                ? value.Trim()
                : value.Trim()[..(TeamsConstants.MaxNotificationPreviewLength - 3)] + "...";

    private static string ResolveShiftLabel(string shiftType) =>
        shiftType switch
        {
            TeamsHandoverShiftTypes.Morning => "sabah",
            TeamsHandoverShiftTypes.Evening => "akşam",
            TeamsHandoverShiftTypes.Night => "gece",
            _ => "özel",
        };
}
