namespace CrevaFactOS.Persistence.Teams;

public static class TeamsDirectMessagePresentationBuilder
{
    public static string? ValidateCreateConversation(CreateTeamsDirectConversationCommand command)
    {
        if (command.RecipientTenantMembershipId == Guid.Empty)
        {
            return "Alıcı üyeliği gerekli.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency anahtarı gerekli.";
        }

        return null;
    }

    public static string? ValidatePostMessage(PostTeamsDirectMessageCommand command)
    {
        if (command.ConversationId == Guid.Empty)
        {
            return "Konuşma tanımlayıcısı gerekli.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency anahtarı gerekli.";
        }

        if (string.IsNullOrWhiteSpace(command.Body))
        {
            return "Mesaj gövdesi gerekli.";
        }

        if (command.Body.Trim().Length > TeamsConstants.MaxMessageBodyLength)
        {
            return "Mesaj gövdesi çok uzun.";
        }

        return null;
    }

    public static string BuildConversationIdempotencyPurpose(Guid lowMembershipId, Guid highMembershipId) =>
        $"teams-direct:{lowMembershipId:D}:{highMembershipId:D}";

    public static string TruncatePreview(string body)
    {
        var trimmed = body.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..117] + "...";
    }
}
