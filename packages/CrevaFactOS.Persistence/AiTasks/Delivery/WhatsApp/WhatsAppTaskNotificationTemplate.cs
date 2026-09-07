using System.Text.Json;

namespace CrevaFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public sealed record WhatsAppTaskNotificationTemplate(
    string TemplateName,
    string LanguageCode,
    string Locale,
    string NotificationType);

public sealed record WhatsAppTaskNotificationContent(
    string HeaderLine,
    string TaskTitle,
    string PriorityLabel,
    string DueDateLabel,
    string SecureTaskUrl);

public static class WhatsAppTaskNotificationTemplateResolver
{
    public const string DefaultNotificationType = "task-notification";

    public static WhatsAppTaskNotificationTemplate Resolve(
        string? locale,
        string? configuredTemplateName,
        string? configuredLanguageCode)
    {
        var resolvedLocale = string.IsNullOrWhiteSpace(locale) ? "tr" : locale.Trim().ToLowerInvariant();
        return new WhatsAppTaskNotificationTemplate(
            string.IsNullOrWhiteSpace(configuredTemplateName) ? "crevafactos_task_notification" : configuredTemplateName.Trim(),
            string.IsNullOrWhiteSpace(configuredLanguageCode) ? resolvedLocale : configuredLanguageCode.Trim(),
            resolvedLocale,
            DefaultNotificationType);
    }

    public static WhatsAppTaskNotificationContent BuildContent(
        string deliveryProjectionJson,
        string secureTaskUrl)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(deliveryProjectionJson) ? "{}" : deliveryProjectionJson);
        var root = document.RootElement;
        var title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Görev" : "Görev";
        var priority = root.TryGetProperty("priority", out var priorityProp) ? priorityProp.GetString() ?? "—" : "—";
        var dueDate = root.TryGetProperty("dueAtUtc", out var dueProp) && dueProp.ValueKind == JsonValueKind.String
            ? dueProp.GetString() ?? "—"
            : "—";

        return new WhatsAppTaskNotificationContent(
            "CrevaFactOS görev bildirimi",
            title,
            priority,
            dueDate,
            secureTaskUrl);
    }

    public static string BuildTemplateBodyParameters(WhatsAppTaskNotificationContent content) =>
        JsonSerializer.Serialize(new[]
        {
            content.TaskTitle,
            content.PriorityLabel,
            content.DueDateLabel,
            content.SecureTaskUrl,
        });
}
