namespace NevaxisZentra.Persistence.AiTasks.Delivery;

public static class AiTaskDeliveryChannels
{
    public const string NevaxisZentraTeams = "NevaxisZentra-teams";
    public const string Email = "email";
    public const string WhatsApp = "whatsapp";
    public const string MicrosoftTeams = "microsoft-teams";
    public const string Sms = "sms";
    public const string DevelopmentLoopback = "development-loopback";

    public static readonly IReadOnlySet<string> FoundationChannels = new HashSet<string>(StringComparer.Ordinal)
    {
        NevaxisZentraTeams, Email, WhatsApp, MicrosoftTeams, Sms,
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        NevaxisZentraTeams, Email, WhatsApp, MicrosoftTeams, Sms, DevelopmentLoopback,
    };
}

public static class AiTaskDeliveryStatuses
{
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string Sent = "sent";
    public const string FailedRetryable = "failed-retryable";
    public const string FailedPermanent = "failed-permanent";
    public const string DeadLetter = "dead-letter";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Queued, Processing, Sent, FailedRetryable, FailedPermanent, DeadLetter, Cancelled,
    };

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.Ordinal)
    {
        Sent, FailedPermanent, DeadLetter, Cancelled,
    };
}

public static class AiTaskDeliveryRecipientKinds
{
    public const string User = "user";
    public const string External = "external";
    public const string SecureLink = "secure-link";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        User, External, SecureLink,
    };
}

public static class AiTaskDeliveryErrorCategories
{
    public const string Transient = "TRANSIENT";
    public const string Permanent = "PERMANENT";
    public const string Configuration = "CONFIGURATION";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Transient, Permanent, Configuration,
    };
}

public static class AiTaskDeliveryAttemptOutcomes
{
    public const string Success = "success";
    public const string TransientFailure = "transient-failure";
    public const string PermanentFailure = "permanent-failure";
    public const string ConfigurationFailure = "configuration-failure";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Success, TransientFailure, PermanentFailure, ConfigurationFailure,
    };
}

public static class AiTaskSecureAccessScopes
{
    public const string ReadOnly = "read-only";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ReadOnly,
    };
}

public static class AiTaskSecureAccessTtlPolicies
{
    public const string OneHour = "1h";
    public const string EightHours = "8h";
    public const string TwentyFourHours = "24h";
    public const string FortyEightHours = "48h";
    public const string SevenDays = "7d";
    public const string ClosureTenSeconds = "closure-10s";

    public static readonly IReadOnlyDictionary<string, TimeSpan> Durations =
        new Dictionary<string, TimeSpan>(StringComparer.Ordinal)
        {
            [OneHour] = TimeSpan.FromHours(1),
            [EightHours] = TimeSpan.FromHours(8),
            [TwentyFourHours] = TimeSpan.FromHours(24),
            [FortyEightHours] = TimeSpan.FromHours(48),
            [SevenDays] = TimeSpan.FromDays(7),
        };

    public static bool IsValidPolicy(string policy, bool enableClosurePolicies) =>
        Durations.ContainsKey(policy)
        || (enableClosurePolicies && policy == ClosureTenSeconds);

    public static TimeSpan ResolveDuration(string policy, bool enableClosurePolicies = false)
    {
        if (enableClosurePolicies && policy == ClosureTenSeconds)
        {
            return TimeSpan.FromSeconds(10);
        }

        return Durations.TryGetValue(policy, out var duration) ? duration : Durations[TwentyFourHours];
    }
}

public static class AiTaskDeliveryAuditEventTypes
{
    public const string DeliveryRequested = "delivery.requested";
    public const string DeliveryQueued = "delivery.queued";
    public const string DeliveryProcessing = "delivery.processing";
    public const string DeliverySent = "delivery.sent";
    public const string DeliveryRetryScheduled = "delivery.retry-scheduled";
    public const string DeliveryFailed = "delivery.failed";
    public const string DeliveryDeadLettered = "delivery.dead-lettered";
    public const string DeliveryCancelled = "delivery.cancelled";
    public const string SecureAccessIssued = "secure-access.issued";
    public const string SecureAccessRevoked = "secure-access.revoked";
    public const string SecureAccessRotated = "secure-access.rotated";
    public const string SecureAccessAccessed = "secure-access.accessed";
}

public static class AiTaskDeliveryFailureCodes
{
    public const string NotFound = "ai-task-delivery.not-found";
    public const string ValidationFailed = "ai-task-delivery.validation-failed";
    public const string InvalidTransition = "ai-task-delivery.invalid-transition";
    public const string ChannelUnavailable = "ai-task-delivery.channel-unavailable";
    public const string ChannelUnknown = "ai-task-delivery.channel-unknown";
    public const string IdempotencyConflict = "ai-task-delivery.idempotency-conflict";
    public const string TaskNotFound = "ai-task-delivery.task-not-found";
    public const string Unavailable = "ai-task-delivery.unavailable";
}

public static class AiTaskSecureAccessFailureCodes
{
    public const string NotFound = "ai-task-secure-access.not-found";
    public const string ValidationFailed = "ai-task-secure-access.validation-failed";
    public const string Expired = "ai-task-secure-access.expired";
    public const string Revoked = "ai-task-secure-access.revoked";
    public const string InvalidToken = "ai-task-secure-access.invalid-token";
    public const string Tampered = "ai-task-secure-access.tampered";
    public const string Unavailable = "ai-task-secure-access.unavailable";
}

public static class AiTaskDeliveryConstants
{
    public const int DefaultMaxAttempts = 5;
    public const int PayloadVersion = 1;
    public const int DefaultLeaseSeconds = 120;
    public const string DefaultSecureAccessTtlPolicy = AiTaskSecureAccessTtlPolicies.TwentyFourHours;
}
