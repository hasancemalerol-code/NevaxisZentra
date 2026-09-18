using System.Globalization;
using System.Net;
using System.Text.Json;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.Email;

public sealed record EmailTaskNotificationTemplate(
    string Locale,
    string NotificationType,
    string SubjectTemplate);

public sealed record EmailTaskNotificationContent(
    string Subject,
    string PlainTextBody,
    string HtmlBody);

public static class EmailTaskNotificationTemplateResolver
{
    public const string DefaultNotificationType = "task-notification";
    public const string DefaultSubjectTemplate = "NevaxisZentra — Yeni görev bildirimi";

    public static EmailTaskNotificationTemplate Resolve(string? locale, string? configuredSubject)
    {
        var resolvedLocale = string.IsNullOrWhiteSpace(locale) ? "tr" : locale.Trim().ToLowerInvariant();
        return new EmailTaskNotificationTemplate(
            resolvedLocale,
            DefaultNotificationType,
            string.IsNullOrWhiteSpace(configuredSubject) ? DefaultSubjectTemplate : configuredSubject.Trim());
    }

    public static EmailTaskNotificationContent BuildContent(
        EmailTaskNotificationTemplate template,
        string deliveryProjectionJson,
        string secureTaskUrl)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(deliveryProjectionJson) ? "{}" : deliveryProjectionJson);
        var root = document.RootElement;
        var title = EscapeHtml(GetString(root, "title", "Görev"));
        var priority = EscapeHtml(GetString(root, "priority", "—"));
        var dueDate = EscapeHtml(FormatDueDate(root));

        var subject = template.SubjectTemplate;
        var plainText = $"""
            NevaxisZentra Factory Operations
            Yeni görev bildirimi

            Görev
            {GetString(root, "title", "Görev")}

            Öncelik
            {GetString(root, "priority", "—")}

            Son tarih
            {FormatDueDate(root)}

            Görevi güvenli olarak görüntüle
            {secureTaskUrl}
            """;

        var html = $"""
            <!DOCTYPE html>
            <html lang="tr">
            <body style="font-family:Segoe UI,Arial,sans-serif;background:#f8fafc;margin:0;padding:24px;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;">
                <tr>
                  <td style="padding:24px 28px;border-bottom:1px solid #e2e8f0;">
                    <strong style="font-size:18px;color:#0f172a;">NevaxisZentra Factory Operations</strong>
                    <div style="margin-top:4px;color:#64748b;font-size:13px;">Yeni görev bildirimi</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:24px 28px;color:#0f172a;font-size:14px;line-height:1.6;">
                    <p style="margin:0 0 12px;"><strong>Görev</strong><br>{title}</p>
                    <p style="margin:0 0 12px;"><strong>Öncelik</strong><br>{priority}</p>
                    <p style="margin:0 0 20px;"><strong>Son tarih</strong><br>{dueDate}</p>
                    <a href="{EscapeHtmlAttribute(secureTaskUrl)}" style="display:inline-block;background:#0f766e;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:600;">Görevi güvenli olarak görüntüle</a>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        return new EmailTaskNotificationContent(subject, plainText, html);
    }

    private static string GetString(JsonElement root, string property, string fallback) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static string FormatDueDate(JsonElement root)
    {
        if (!root.TryGetProperty("dueAtUtc", out var dueProp) || dueProp.ValueKind != JsonValueKind.String)
        {
            return "—";
        }

        var raw = dueProp.GetString();
        if (string.IsNullOrWhiteSpace(raw) || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return raw ?? "—";
        }

        return parsed.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
    }

    internal static string EscapeHtml(string value) => WebUtility.HtmlEncode(value);

    private static string EscapeHtmlAttribute(string value) => WebUtility.HtmlEncode(value);
}
