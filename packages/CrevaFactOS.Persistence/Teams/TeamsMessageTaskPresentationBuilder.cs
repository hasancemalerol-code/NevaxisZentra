using CrevaFactOS.Persistence.AiTasks;

namespace CrevaFactOS.Persistence.Teams;

public static class TeamsMessageTaskPresentationBuilder
{
    public static string BuildMessageDeepLink(Guid channelId, Guid messageId) =>
        $"/teams?channel={channelId:D}&message={messageId:D}";

    public static string BuildSourceContextReference(Guid channelId, Guid messageId) =>
        $"teams:message:{messageId:D}:channel:{channelId:D}";

    public static string? ValidateDraftInput(string title, string category, string priority)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Görev başlığı zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(category) || !AiTaskCategories.All.Contains(category.Trim()))
        {
            return "Geçerli bir görev kategorisi seçin.";
        }

        if (string.IsNullOrWhiteSpace(priority) || !AiTaskPriorities.All.Contains(priority.Trim()))
        {
            return "Geçerli bir öncelik seçin.";
        }

        return null;
    }

    public static CreateSourceLinkedAiTaskCommand BuildSourceLinkedTaskCommand(
        TeamsMessage message,
        string channelName,
        CreateMessageTaskDraftCommand command)
    {
        var idempotencyKey = TeamsConstants.BuildHumanReviewDraftIdempotencyKey(message.Id);
        var deepLink = BuildMessageDeepLink(message.ChannelId, message.Id);
        var bodyPreview = Truncate(message.Body, 500);
        var humanNote = string.IsNullOrWhiteSpace(command.HumanNote) ? null : command.HumanNote.Trim();
        var reasoning = humanNote is null
            ? $"Teams kanalı \"{channelName}\" içindeki mesajdan insan incelemeli görev taslağı oluşturuldu."
            : $"Teams kanalı \"{channelName}\" içindeki mesajdan insan incelemeli görev taslağı oluşturuldu. Not: {humanNote}";

        return new CreateSourceLinkedAiTaskCommand(
            Title: command.Title.Trim(),
            Summary: BuildSummary(channelName, message, humanNote),
            Category: command.Category.Trim(),
            Priority: command.Priority.Trim(),
            RiskLevel: ResolveRiskLevel(command.Priority),
            DetectedIssue: bodyPreview,
            Reasoning: reasoning,
            Recommendation: new AiTaskRecommendationModel(
                "review",
                "Teams mesajından oluşturulan görev taslağı yalnızca insan incelemesi içindir; otomatik operasyon başlatmaz.",
                "review",
                RequiresApproval: true,
                Reversible: true,
                TargetSystem: "crevafactos"),
            Confidence: new AiTaskConfidenceModel(
                0.5m,
                "Teams mesajından manuel görev taslağı oluşturuldu.",
                null),
            Impact: new AiTaskImpactModel(
                null, null, null, null, null, null, null,
                $"Kanal: {channelName}"),
            Sources:
            [
                new AiTaskSourceReferenceModel(
                    TeamsConstants.TeamsSourceModule,
                    message.Id.ToString(),
                    $"Teams mesajı — {channelName}",
                    message.CreatedAtUtc,
                    bodyPreview),
                new AiTaskSourceReferenceModel(
                    TeamsConstants.TeamsSourceModule,
                    message.ChannelId.ToString(),
                    channelName,
                    message.CreatedAtUtc,
                    deepLink),
            ],
            SourceModule: TeamsConstants.TeamsSourceModule,
            SourceEntityId: message.Id.ToString(),
            IdempotencyKey: idempotencyKey,
            SiteId: null,
            ProductionLineId: null,
            MachineId: null,
            MoldId: null,
            ProductionOrderId: null,
            SalesOrderId: null,
            MaterialId: null,
            SupplierId: null,
            CustomerId: null,
            DueAtUtc: null);
    }

    private static string BuildSummary(string channelName, TeamsMessage message, string? humanNote)
    {
        var excerpt = Truncate(message.Body.Trim(), 280);
        return humanNote is null
            ? $"Teams kanalı \"{channelName}\" mesajından oluşturulan insan incelemeli görev taslağı. Mesaj: {excerpt}"
            : $"Teams kanalı \"{channelName}\" mesajından oluşturulan insan incelemeli görev taslağı. Mesaj: {excerpt} Not: {humanNote}";
    }

    private static string ResolveRiskLevel(string priority) =>
        priority.Trim() switch
        {
            AiTaskPriorities.Critical => AiTaskRiskLevels.Critical,
            AiTaskPriorities.High => AiTaskRiskLevels.High,
            AiTaskPriorities.Low => AiTaskRiskLevels.Low,
            _ => AiTaskRiskLevels.Medium,
        };

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..(maxLength - 3)] + "...";
    }
}
