namespace NevaxisFactOS.Persistence.SaasSubscription;

public static class SaasSubscriptionStatuses
{
    public const string Trialing = "trialing";
    public const string Active = "active";
    public const string GracePeriod = "grace-period";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Trialing, Active, GracePeriod, Suspended, Cancelled, Expired,
    };

    public static readonly IReadOnlySet<string> PaidApplicationAccess =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Trialing, Active, GracePeriod,
        };
}

public static class SaasSubscriptionTransitionReasons
{
    public const string TrialStarted = "trial-started";
    public const string TrialConverted = "trial-converted";
    public const string PaymentReceived = "payment-received";
    public const string PaymentFailed = "payment-failed";
    public const string GraceEntered = "grace-entered";
    public const string GraceExpired = "grace-expired";
    public const string ManualSuspension = "manual-suspension";
    public const string ManualReactivation = "manual-reactivation";
    public const string CancellationRequested = "cancellation-requested";
    public const string CancellationEffective = "cancellation-effective";
    public const string SubscriptionExpired = "subscription-expired";
    public const string DevelopmentSeed = "development-seed";
    public const string PlanChanged = "plan-changed";
    public const string DevelopmentAssignment = "development-assignment";
}

public static class SaasSubscriptionProvenance
{
    public const string LocalDevelopmentPilot = "local-development-pilot";
    public const string NonProduction = "non-production";
    public const string BillingSystem = "billing-system";
    public const string ManualOverride = "manual-override";
}

public static class SaasSubscriptionAuditEventTypes
{
    public const string SubscriptionAssigned = "saas-subscription.subscription.assigned";
    public const string SubscriptionChanged = "saas-subscription.subscription.changed";
    public const string SubscriptionTransitioned = "saas-subscription.subscription.transitioned";
    public const string DevelopmentSubscriptionAssigned = "saas-subscription.development.assigned";
}

public static class SaasSubscriptionAuditEntityTypes
{
    public const string TenantSubscription = "saas-tenant-subscription";
    public const string TransitionHistory = "saas-tenant-subscription-transition";
}

public static class SaasSubscriptionEntitlementReasonCodes
{
    public const string Allowed = "saas-subscription.entitlement.allowed";
    public const string SubscriptionNotFound = "saas-subscription.entitlement.subscription-not-found";
    public const string SubscriptionInactive = "saas-subscription.entitlement.subscription-inactive";
    public const string PaidApplicationDenied = "saas-subscription.entitlement.paid-application-denied";
    public const string EntitlementUnavailable = "saas-subscription.entitlement.unavailable";
    public const string EntitlementLimitExceeded = "saas-subscription.entitlement.limit-exceeded";
    public const string EntitlementUnknownConfig = "saas-subscription.entitlement.unknown-config";
    public const string RouteExempt = "saas-subscription.entitlement.route-exempt";
    public const string EvaluationUnavailable = "saas-subscription.entitlement.evaluation-unavailable";
}

public static class SaasSubscriptionEntitlementExplanationKeys
{
    public const string Allowed = "saas-subscription.explanation.allowed";
    public const string SubscriptionNotFound = "saas-subscription.explanation.subscription-not-found";
    public const string SubscriptionInactive = "saas-subscription.explanation.subscription-inactive";
    public const string PaidApplicationDenied = "saas-subscription.explanation.paid-application-denied";
    public const string EntitlementUnavailable = "saas-subscription.explanation.entitlement-unavailable";
    public const string EntitlementLimitExceeded = "saas-subscription.explanation.entitlement-limit-exceeded";
    public const string EntitlementUnknownConfig = "saas-subscription.explanation.entitlement-unknown-config";
    public const string RouteExempt = "saas-subscription.explanation.route-exempt";
    public const string EvaluationUnavailable = "saas-subscription.explanation.evaluation-unavailable";
}

public static class SaasSubscriptionFailureCodes
{
    public const string Conflict = "saas-subscription.conflict";
    public const string ValidationFailed = "saas-subscription.validation-failed";
    public const string NotFound = "saas-subscription.not-found";
    public const string ConcurrencyConflict = "saas-subscription.concurrency-conflict";
    public const string IdempotencyConflict = "saas-subscription.idempotency-conflict";
    public const string InvalidTransition = "saas-subscription.invalid-transition";
    public const string Unavailable = "saas-subscription.unavailable";
}

public static class SaasSubscriptionValueConversions
{
    public static bool IsValidStatus(string status) =>
        SaasSubscriptionStatuses.All.Contains(status);

    public static bool CanTenantAccessPaidApplication(string? subscriptionStatus) =>
        subscriptionStatus is not null
        && SaasSubscriptionStatuses.PaidApplicationAccess.Contains(subscriptionStatus);

    public static bool ResolvePaidApplicationAccess(
        string subscriptionStatus,
        DateTimeOffset nowUtc,
        DateTimeOffset? trialEndsAtUtc,
        DateTimeOffset? graceUntilUtc,
        DateTimeOffset currentPeriodEndUtc) =>
        subscriptionStatus switch
        {
            SaasSubscriptionStatuses.Trialing =>
                trialEndsAtUtc is null || nowUtc <= trialEndsAtUtc,
            SaasSubscriptionStatuses.Active => true,
            SaasSubscriptionStatuses.GracePeriod =>
                graceUntilUtc is null || nowUtc <= graceUntilUtc,
            SaasSubscriptionStatuses.Cancelled => nowUtc <= currentPeriodEndUtc,
            _ => false,
        };

    public static bool IsTerminalStatus(string status) =>
        status is SaasSubscriptionStatuses.Cancelled or SaasSubscriptionStatuses.Expired;

    public static bool IsValidTransition(string fromStatus, string toStatus) =>
        fromStatus switch
        {
            SaasSubscriptionStatuses.Trialing => toStatus is SaasSubscriptionStatuses.Active
                or SaasSubscriptionStatuses.Cancelled,
            SaasSubscriptionStatuses.Active => toStatus is SaasSubscriptionStatuses.GracePeriod
                or SaasSubscriptionStatuses.Suspended
                or SaasSubscriptionStatuses.Cancelled,
            SaasSubscriptionStatuses.GracePeriod => toStatus is SaasSubscriptionStatuses.Active
                or SaasSubscriptionStatuses.Suspended
                or SaasSubscriptionStatuses.Expired,
            SaasSubscriptionStatuses.Suspended => toStatus is SaasSubscriptionStatuses.Active
                or SaasSubscriptionStatuses.Cancelled
                or SaasSubscriptionStatuses.Expired,
            SaasSubscriptionStatuses.Cancelled => toStatus is SaasSubscriptionStatuses.Expired,
            SaasSubscriptionStatuses.Expired => false,
            _ => false,
        };
}
