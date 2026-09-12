namespace NevaxisFactOS.Persistence.AiTasks.Delivery.Email;

public static class EmailDeliveryProviders
{
    public const string Smtp = "smtp";
}

public static class EmailSecurityModes
{
    public const string None = "none";
    public const string StartTls = "starttls";
    public const string Tls = "tls";
}

public static class EmailReadinessStates
{
    public const string NotConfigured = "not-configured";
    public const string Configured = "configured";
    public const string Ready = "ready";
    public const string Degraded = "degraded";
    public const string InvalidCredentials = "invalid-credentials";
    public const string Disabled = "disabled";
}

public static class EmailProviderStatuses
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string TransportFailed = "transport-failed";
}

public static class EmailRecipientEligibility
{
    public const string Eligible = "eligible";
    public const string Invalid = "invalid";
    public const string Blocked = "blocked";
    public const string Unknown = "unknown";
}

public static class EmailProviderErrorCategories
{
    public const string Retryable = "retryable";
    public const string Permanent = "permanent";
    public const string Configuration = "configuration";
    public const string Recipient = "recipient";
    public const string Authentication = "authentication";
    public const string RateLimit = "rate-limit";
    public const string Network = "network";
    public const string Unknown = "unknown";
}

public static class EmailDeliveryAuditEventTypes
{
    public const string Queued = "delivery.email.queued";
    public const string ProviderAccepted = "delivery.email.provider-accepted";
    public const string Failed = "delivery.email.failed";
    public const string Cancelled = "delivery.email.cancelled";
}
