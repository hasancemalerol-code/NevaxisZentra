namespace NevaxisFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public static class WhatsAppBusinessProviders
{
    public const string MetaWhatsAppCloud = "meta-whatsapp-cloud";
}

public static class WhatsAppReadinessStates
{
    public const string NotConfigured = "not-configured";
    public const string Configured = "configured";
    public const string Ready = "ready";
    public const string Degraded = "degraded";
    public const string InvalidCredentials = "invalid-credentials";
    public const string Disabled = "disabled";
}

public static class WhatsAppProviderStatuses
{
    public const string Accepted = "accepted";
    public const string Sent = "sent";
    public const string Delivered = "delivered";
    public const string Read = "read";
    public const string Failed = "failed";

    private static readonly Dictionary<string, int> Rank = new(StringComparer.Ordinal)
    {
        [Accepted] = 1,
        [Sent] = 2,
        [Delivered] = 3,
        [Read] = 4,
        [Failed] = 99,
    };

    public static bool ShouldAdvance(string? current, string incoming) =>
        incoming == Failed
        || !Rank.TryGetValue(current ?? string.Empty, out var currentRank)
        || Rank[incoming] > currentRank;
}

public static class WhatsAppConsentStatuses
{
    public const string Known = "known";
    public const string Unknown = "unknown";
    public const string NotVerified = "not-verified";
}

public static class WhatsAppProviderErrorCategories
{
    public const string Retryable = "retryable";
    public const string Permanent = "permanent";
    public const string Configuration = "configuration";
    public const string Recipient = "recipient";
    public const string PolicyTemplate = "policy-template";
    public const string RateLimit = "rate-limit";
    public const string Authentication = "authentication";
    public const string Unknown = "unknown";
}

public static class WhatsAppDeliveryAuditEventTypes
{
    public const string Queued = "delivery.whatsapp.queued";
    public const string ProviderAccepted = "delivery.whatsapp.provider-accepted";
    public const string Sent = "delivery.whatsapp.sent";
    public const string Delivered = "delivery.whatsapp.delivered";
    public const string Read = "delivery.whatsapp.read";
    public const string Failed = "delivery.whatsapp.failed";
    public const string WebhookRejected = "delivery.whatsapp.webhook-rejected";
}
