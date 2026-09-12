namespace NevaxisFactOS.Persistence.Teams;

public static class TeamsWorkInboxPresentationBuilder
{
    public static string AiTaskAssignedReason => "Size atanan AI görevi";
    public static string AiTaskReviewReason => "İncelemeniz gereken AI görevi";
    public static string ApprovalReason => "Kararınızı bekleyen onay isteği";
    public static string HandoverReason => "Teslim almanız gereken vardiya devri";
    public static string MentionReason => "Sizi etiketleyen Teams mesajı";

    public static string BuildAiTaskDeepLink(Guid taskId) => $"/ai-gorevleri/{taskId:D}";

    public static string BuildApprovalDeepLink(Guid requestId) => $"/teams/onaylar/{requestId:D}";

    public static string BuildHandoverDeepLink(Guid handoverId) => $"/teams/devretme/{handoverId:D}";

    public static string BuildMentionDeepLink(Guid channelId, Guid messageId) =>
        $"/teams?channel={channelId:D}&message={messageId:D}";
}
