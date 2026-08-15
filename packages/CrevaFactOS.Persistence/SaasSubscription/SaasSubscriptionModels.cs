namespace CrevaFactOS.Persistence.SaasSubscription;

public sealed record SaasSubscriptionOperationContext(
    Guid TenantId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record SaasSubscriptionPlanVersionReferenceReadModel(
    Guid CommercialPlanVersionId,
    string? PlanCode,
    int? VersionNumber);

public sealed record SaasSubscriptionGraceStateReadModel(
    bool InGracePeriod,
    DateTimeOffset? GraceUntilUtc);

public sealed record SaasSubscriptionLimitEvidenceReadModel(
    string EntitlementCode,
    string Semantics,
    int? LimitValue,
    int? UsageValue,
    bool LimitConfigured);

public sealed record TenantEntitlementEvaluationReadModel(
    bool Allowed,
    string ReasonCode,
    string ExplanationKey,
    string? SubscriptionStatus,
    SaasSubscriptionPlanVersionReferenceReadModel? PlanVersion,
    string? EntitlementCode,
    SaasSubscriptionLimitEvidenceReadModel? Limit,
    SaasSubscriptionGraceStateReadModel? GraceState,
    DateTimeOffset EvaluatedAtUtc)
{
    public static TenantEntitlementEvaluationReadModel Deny(
        string reasonCode,
        string explanationKey,
        string? subscriptionStatus = null,
        SaasSubscriptionPlanVersionReferenceReadModel? planVersion = null,
        string? entitlementCode = null,
        SaasSubscriptionLimitEvidenceReadModel? limit = null,
        SaasSubscriptionGraceStateReadModel? graceState = null) =>
        new(
            false,
            reasonCode,
            explanationKey,
            subscriptionStatus,
            planVersion,
            entitlementCode,
            limit,
            graceState,
            DateTimeOffset.UtcNow);

    public static TenantEntitlementEvaluationReadModel Allow(
        string? subscriptionStatus,
        SaasSubscriptionPlanVersionReferenceReadModel? planVersion,
        string? entitlementCode = null,
        SaasSubscriptionLimitEvidenceReadModel? limit = null,
        SaasSubscriptionGraceStateReadModel? graceState = null) =>
        new(
            true,
            SaasSubscriptionEntitlementReasonCodes.Allowed,
            SaasSubscriptionEntitlementExplanationKeys.Allowed,
            subscriptionStatus,
            planVersion,
            entitlementCode,
            limit,
            graceState,
            DateTimeOffset.UtcNow);
}

public sealed record SaasSubscriptionSummaryReadModel(
    Guid SubscriptionId,
    string SubscriptionStatus,
    string BillingCadence,
    Guid CommercialPlanVersionId,
    string? PlanCode,
    int? PlanVersionNumber,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset CurrentPeriodStartUtc,
    DateTimeOffset CurrentPeriodEndUtc,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset? GraceUntilUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    bool CancelAtPeriodEnd,
    string SourceProvenance,
    bool IsCurrent,
    long Version);

public sealed record SaasSubscriptionTransitionHistoryReadModel(
    Guid TransitionId,
    Guid SubscriptionId,
    string FromStatus,
    string ToStatus,
    string TransitionReason,
    string ActorReference,
    DateTimeOffset OccurredAtUtc,
    string? IdempotencyKey);

public sealed record SaasSubscriptionAuditEventReadModel(
    string EventType,
    string EntityType,
    string EntityReference,
    string ActorReference,
    DateTimeOffset OccurredAtUtc,
    object? Payload);

public sealed record SaasSubscriptionWriteResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureMessage,
    bool IdempotentReplay = false)
{
    public static SaasSubscriptionWriteResult Ok(bool idempotentReplay = false) =>
        new(true, null, null, idempotentReplay);

    public static SaasSubscriptionWriteResult Fail(string code, string message) =>
        new(false, code, message);
}

public sealed record AssignDevelopmentSaasSubscriptionCommand(
    Guid CommercialPlanVersionId,
    string BillingCadence,
    string SubscriptionStatus,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset CurrentPeriodStartUtc,
    DateTimeOffset CurrentPeriodEndUtc,
    DateTimeOffset? TrialEndsAtUtc,
    string SourceProvenance,
    string TransitionReason,
    string? IdempotencyKey);

public sealed record TransitionSaasSubscriptionCommand(
    Guid SubscriptionId,
    string TargetStatus,
    string TransitionReason,
    long ExpectedVersion,
    bool? CancelAtPeriodEnd,
    DateTimeOffset? GraceUntilUtc,
    string? IdempotencyKey);

public sealed record EvaluateTenantEntitlementQuery(
    string? RoutePath,
    string? EntitlementCode,
    string? FeatureCode,
    bool EvaluatePaidApplicationAccess);
