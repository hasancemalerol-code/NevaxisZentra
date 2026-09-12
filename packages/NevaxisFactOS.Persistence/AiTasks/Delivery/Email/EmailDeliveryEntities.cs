namespace NevaxisFactOS.Persistence.AiTasks.Delivery.Email;

public sealed class AiTaskEmailChannelConfiguration
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public bool Enabled { get; set; }
    public string Provider { get; set; } = EmailDeliveryProviders.Smtp;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string SecurityMode { get; set; } = EmailSecurityModes.StartTls;
    public string? Username { get; set; }
    public string? PasswordProtected { get; set; }
    public string? FromAddress { get; set; }
    public string? FromDisplayName { get; set; }
    public string? ReplyToAddress { get; set; }
    public string DefaultLocale { get; set; } = "tr";
    public string? TemplateSubject { get; set; }
    public string ReadinessStatus { get; set; } = EmailReadinessStates.NotConfigured;
    public string? ReadinessErrorCode { get; set; }
    public DateTimeOffset? LastValidatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
