namespace NevaxisFactOS.Persistence.AiTasks.Delivery;

public sealed class AiTaskDelivery
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AiTaskId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string RecipientKind { get; set; } = AiTaskDeliveryRecipientKinds.User;
    public Guid? RecipientUserId { get; set; }
    public string? MaskedRecipient { get; set; }
    public string Status { get; set; } = AiTaskDeliveryStatuses.Queued;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int PayloadVersion { get; set; } = AiTaskDeliveryConstants.PayloadVersion;
    public string? DeliveryProjectionJson { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = AiTaskDeliveryConstants.DefaultMaxAttempts;
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ProviderCorrelationId { get; set; }
    public string? ProviderStatus { get; set; }
    public DateTimeOffset? ProviderStatusAtUtc { get; set; }
    public string? RecipientAddressProtected { get; set; }
    public string? ConsentStatus { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorCategory { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? SecureAccessGrantId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<AiTaskDeliveryAttempt> Attempts { get; set; } = [];
}

public sealed class AiTaskDeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DeliveryId { get; set; }
    public int AttemptNumber { get; set; }
    public string AdapterKey { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? ProviderStatusCode { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorCategory { get; set; }
    public string? SanitizedErrorCode { get; set; }
    public DateTimeOffset? RetryScheduledForUtc { get; set; }

    public AiTaskDelivery? Delivery { get; set; }
}

public sealed class AiTaskSecureAccessGrant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AiTaskId { get; set; }
    public string Scope { get; set; } = AiTaskSecureAccessScopes.ReadOnly;
    public string TokenHash { get; set; } = string.Empty;
    public Guid IssuedByUserId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }
    public DateTimeOffset? LastAccessedAtUtc { get; set; }
    public int AccessCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
