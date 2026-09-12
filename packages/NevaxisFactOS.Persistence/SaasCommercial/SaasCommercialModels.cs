namespace NevaxisFactOS.Persistence.SaasCommercial;

public sealed record SaasCommercialOperationContext(
    Guid TenantId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record SaasCommercialPlanSummaryReadModel(
    string PlanCode,
    string DisplayNameTr,
    string? DescriptionTr,
    string LifecycleState,
    string AvailabilityState,
    IReadOnlyList<SaasCommercialPlanVersionSummaryReadModel> Versions);

public sealed record SaasCommercialPlanVersionSummaryReadModel(
    int VersionNumber,
    string BillingCadence,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    string PublicationStatus,
    bool Sellable,
    string? Provenance);

public sealed record SaasCommercialPlanDetailReadModel(
    string PlanCode,
    string DisplayNameTr,
    string? DescriptionTr,
    string LifecycleState,
    string AvailabilityState,
    IReadOnlyList<SaasCommercialPlanVersionDetailReadModel> Versions);

public sealed record SaasCommercialPlanVersionDetailReadModel(
    int VersionNumber,
    string BillingCadence,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    int TrialDays,
    string CurrencyCode,
    bool Sellable,
    string PublicationStatus,
    string? CommercialNotes,
    string? Provenance,
    IReadOnlyList<SaasCommercialPriceComponentReadModel> PriceComponents);

public sealed record SaasCommercialPriceComponentReadModel(
    string ComponentType,
    decimal? Amount,
    string CurrencyCode,
    int SortOrder);

public sealed record SaasCommercialEntitlementReadModel(
    string Code,
    string DisplayNameTr,
    string? DescriptionTr,
    string ValueKind,
    string Semantics,
    bool? BooleanValue,
    int? IntegerLimitValue);

public sealed record SaasCommercialPaymentMethodCapabilityReadModel(
    string MethodType,
    bool Enabled,
    bool ManualVerificationRequired,
    bool RecurringCapable,
    int DisplayOrder,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc);

public sealed record SaasCommercialGracePeriodPolicyReadModel(
    int GracePeriodDays,
    string RestrictedBehaviorToken,
    string SuspensionEligibilityPolicy,
    string OperatorExplanationTr);

public sealed record SaasCommercialCatalogListReadModel(
    IReadOnlyList<SaasCommercialPlanSummaryReadModel> Plans);

public sealed record SaasCommercialWriteResult(bool Succeeded, string? FailureCode, string? FailureMessage)
{
    public static SaasCommercialWriteResult Ok() => new(true, null, null);

    public static SaasCommercialWriteResult Fail(string code, string message) =>
        new(false, code, message);
}

public static class SaasCommercialFailureCodes
{
    public const string Conflict = "saas-commercial.conflict";
    public const string ValidationFailed = "saas-commercial.validation-failed";
    public const string NotFound = "saas-commercial.not-found";
    public const string ConcurrencyConflict = "saas-commercial.concurrency-conflict";
    public const string ImmutablePublished = "saas-commercial.immutable-published";
    public const string Unavailable = "saas-commercial.unavailable";
}

public sealed record CreateSaasCommercialPlanCommand(
    string PlanCode,
    string DisplayNameTr,
    string? DescriptionTr,
    string LifecycleState,
    string AvailabilityState);

public sealed record CreateSaasCommercialPlanVersionCommand(
    string PlanCode,
    int VersionNumber,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    string BillingCadence,
    int TrialDays,
    string CurrencyCode,
    bool Sellable,
    string? CommercialNotes,
    string? Provenance,
    uint ExpectedPlanVersion);

public sealed record PublishSaasCommercialPlanVersionCommand(
    string PlanCode,
    int VersionNumber,
    uint ExpectedVersionVersion);

public sealed record RetireSaasCommercialPlanVersionCommand(
    string PlanCode,
    int VersionNumber);
