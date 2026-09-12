namespace NevaxisFactOS.Persistence.AiTasks.Delivery.Email;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "AiTaskDelivery:Email";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = EmailDeliveryProviders.Smtp;

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string SecurityMode { get; set; } = EmailSecurityModes.StartTls;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string? FromDisplayName { get; set; }

    public string? ReplyToAddress { get; set; }

    public string DefaultLocale { get; set; } = "tr";

    public int TimeoutSeconds { get; set; } = 30;

    public string? TestSmtpHost { get; set; }

    public int? TestSmtpPort { get; set; }

    public bool IsConfigured() =>
        Enabled
        && !string.IsNullOrWhiteSpace(SmtpHost)
        && SmtpPort > 0
        && !string.IsNullOrWhiteSpace(FromAddress);

    public string ResolveReadinessStatus()
    {
        if (!Enabled)
        {
            return EmailReadinessStates.Disabled;
        }

        if (!IsConfigured())
        {
            return EmailReadinessStates.NotConfigured;
        }

        if (SecurityMode == EmailSecurityModes.None)
        {
            return EmailReadinessStates.Degraded;
        }

        return EmailReadinessStates.Ready;
    }
}
