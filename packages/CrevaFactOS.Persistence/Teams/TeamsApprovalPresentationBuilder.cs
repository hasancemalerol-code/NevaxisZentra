namespace CrevaFactOS.Persistence.Teams;

public static class TeamsApprovalPresentationBuilder
{
    public static string? ValidateCreateInput(CreateTeamsApprovalRequestCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title)
            || command.Title.Trim().Length > TeamsConstants.MaxApprovalTitleLength)
        {
            return "Onay başlığı zorunludur ve 160 karakteri aşamaz.";
        }

        if (string.IsNullOrWhiteSpace(command.Rationale)
            || command.Rationale.Trim().Length > TeamsConstants.MaxApprovalRationaleLength)
        {
            return "Gerekçe zorunludur ve 2000 karakteri aşamaz.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)
            || command.IdempotencyKey.Length > 128)
        {
            return "Geçerli bir idempotency anahtarı gerekli.";
        }

        var hasMessage = command.MessageId is Guid messageId && messageId != Guid.Empty;
        var hasTask = command.AiTaskId is Guid taskId && taskId != Guid.Empty;
        if (hasMessage == hasTask)
        {
            return "İstek tek bir Teams mesajına veya tek bir AI görevine bağlanmalıdır.";
        }

        if (command.Reviewers is null || command.Reviewers.Count == 0)
        {
            return "En az bir inceleyen seçilmelidir.";
        }

        foreach (var reviewer in command.Reviewers)
        {
            if (reviewer.TenantMembershipId == Guid.Empty)
            {
                return "İnceleyen kimliği geçersiz.";
            }

            if (!IsReviewerRole(reviewer.Role))
            {
                return "Geçersiz inceleyen rolü.";
            }
        }

        if (command.Reviewers.Select(r => r.TenantMembershipId).Distinct().Count() != command.Reviewers.Count)
        {
            return "Aynı inceleyen birden fazla eklenemez.";
        }

        return null;
    }

    public static string? ValidateDecisionInput(string decision, string? note)
    {
        if (!IsDecision(decision))
        {
            return "Desteklenen karar: onayla, reddet veya değişiklik iste.";
        }

        if (note is not null && note.Trim().Length > TeamsConstants.MaxApprovalDecisionNoteLength)
        {
            return "Karar notu 1000 karakteri aşamaz.";
        }

        return null;
    }

    public static bool IsReviewerRole(string? role) =>
        TeamsApprovalReviewerRoles.Reviewer.Equals(role, StringComparison.Ordinal)
        || TeamsApprovalReviewerRoles.FinalReviewer.Equals(role, StringComparison.Ordinal);

    public static bool IsDecision(string? decision) =>
        TeamsApprovalDecisions.Approved.Equals(decision, StringComparison.Ordinal)
        || TeamsApprovalDecisions.Rejected.Equals(decision, StringComparison.Ordinal)
        || TeamsApprovalDecisions.ChangesRequested.Equals(decision, StringComparison.Ordinal);

    public static string MapDecisionToStatus(string decision) =>
        decision switch
        {
            TeamsApprovalDecisions.Approved => TeamsApprovalStatuses.Approved,
            TeamsApprovalDecisions.Rejected => TeamsApprovalStatuses.Rejected,
            TeamsApprovalDecisions.ChangesRequested => TeamsApprovalStatuses.ChangesRequested,
            _ => TeamsApprovalStatuses.Pending,
        };

    public static string BuildDeepLink(Guid requestId) =>
        $"/teams/onaylar/{requestId:D}";

    public static string BuildMessageDeepLink(Guid channelId, Guid messageId) =>
        $"/teams?channel={channelId:D}&message={messageId:D}";

    public static string BuildTaskDeepLink(Guid taskId) =>
        $"/ai-gorevleri/{taskId:D}";
}
