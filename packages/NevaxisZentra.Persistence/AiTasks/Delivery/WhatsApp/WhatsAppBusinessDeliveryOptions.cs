namespace NevaxisZentra.Persistence.AiTasks.Delivery.WhatsApp;

public sealed class WhatsAppBusinessDeliveryOptions
{
    public const string SectionName = "AiTaskDelivery:WhatsApp";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = WhatsAppBusinessProviders.MetaWhatsAppCloud;

    public string? PhoneNumberId { get; set; }

    public string? BusinessAccountId { get; set; }

    public string? AccessToken { get; set; }

    public string? AppSecret { get; set; }

    public string? WebhookVerifyToken { get; set; }

    public string DefaultLocale { get; set; } = "tr";

    public string? DefaultTemplateName { get; set; }

    public string? DefaultTemplateLanguageCode { get; set; }

    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com/v21.0";

    public int HttpTimeoutSeconds { get; set; } = 30;

    public string? TestApiBaseUrl { get; set; }

    public bool IsConfigured() =>
        Enabled
        && !string.IsNullOrWhiteSpace(PhoneNumberId)
        && !string.IsNullOrWhiteSpace(AccessToken);

    public bool HasWebhookVerification() =>
        !string.IsNullOrWhiteSpace(AppSecret) || !string.IsNullOrWhiteSpace(WebhookVerifyToken);
}
