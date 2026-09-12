namespace NevaxisFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public sealed class AiTaskWhatsAppChannelConfiguration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public bool Enabled { get; set; }
    public string Provider { get; set; } = WhatsAppBusinessProviders.MetaWhatsAppCloud;
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? SenderDisplayName { get; set; }
    public string DefaultLocale { get; set; } = "tr";
    public string? TemplateName { get; set; }
    public string? TemplateLanguageCode { get; set; }
    public string? AccessTokenProtected { get; set; }
    public string? WebhookVerifyTokenProtected { get; set; }
    public string ReadinessStatus { get; set; } = WhatsAppReadinessStates.NotConfigured;
    public string? ReadinessErrorCode { get; set; }
    public DateTimeOffset? LastValidatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class AiTaskDeliveryProviderEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DeliveryId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string ProviderMessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ProviderStatus { get; set; } = string.Empty;
    public string EventHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? SanitizedPayloadJson { get; set; }
}
