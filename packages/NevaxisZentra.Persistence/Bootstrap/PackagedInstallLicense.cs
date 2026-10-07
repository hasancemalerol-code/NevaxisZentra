using System.Globalization;
using NevaxisZentra.Persistence.SaasCommercial;
using NevaxisZentra.Persistence.SaasSubscription;

namespace NevaxisZentra.Persistence.Bootstrap;

/// <summary>
/// The commercial records a packaged (single-machine) installation needs for its first tenant to use the
/// application: one plan, one published plan version and an active subscription. No module or limit
/// entitlement definitions are created on purpose: the entitlement evaluator treats a module without a
/// definition as unrestricted, so the installed product is fully usable and no limit is invented here.
/// Pricing, term and per-module restrictions belong to a later licensing flow.
/// </summary>
public static class PackagedInstallLicense
{
    public const string PlanCode = "zentra-packaged-install";

    public const string Provenance = "packaged-install;first-account-enrollment";

    public const string ActorReference = "system:first-account-enrollment";

    public static readonly Guid PlanId = new("bb000001-0f01-4000-8000-0000000000a1");

    public static readonly Guid PlanVersionId = new("bb000001-0f01-4000-8000-0000000000a2");

    private static readonly Guid PlanCreatedAuditId = new("bb000001-0f01-4000-8000-0000000000a3");

    private static readonly Guid VersionPublishedAuditId = new("bb000001-0f01-4000-8000-0000000000a4");

    /// <summary>Global catalog rows. Idempotent by fixed identifiers; written by the privileged phase only.</summary>
    public static PackagedInstallCatalog BuildCatalog(DateTimeOffset now) => new(
        new SaasCommercialPlan
        {
            Id = PlanId,
            PlanCode = PlanCode,
            PlanCodeNormalized = SaasCommercialValueConversions.NormalizePlanCode(PlanCode),
            DisplayNameTr = "Nevaxis Zentra Kurulu Sürüm",
            DescriptionTr = "Tek makineli paketli kurulum için ilk kuruluşa verilen lisans kaydı.",
            LifecycleState = SaasCommercialPlanLifecycleStates.Active,
            AvailabilityState = SaasCommercialAvailabilityStates.Unavailable,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        },
        new SaasCommercialPlanVersion
        {
            Id = PlanVersionId,
            PlanId = PlanId,
            VersionNumber = 1,
            EffectiveFromUtc = now,
            EffectiveUntilUtc = null,
            BillingCadence = SaasCommercialBillingCadences.Annual,
            TrialDays = 0,
            CurrencyCode = "TRY",
            Sellable = false,
            PublicationStatus = SaasCommercialPublicationStatuses.Published,
            CommercialNotes = "Paketli kurulum lisansı; fiyat ve modül kısıtı tanımlamaz.",
            Provenance = Provenance,
            CreatedAtUtc = now,
            Version = 1,
        },
        [
            new SaasCommercialAuditEvent
            {
                Id = PlanCreatedAuditId,
                EventType = SaasCommercialAuditEventTypes.PlanCreated,
                EntityType = SaasCommercialAuditEntityTypes.Plan,
                EntityId = PlanId,
                PayloadJson = $$"""{"planCode":"{{PlanCode}}","provenance":"{{Provenance}}"}""",
                ActorSubjectId = ActorReference,
                OccurredAtUtc = now,
            },
            new SaasCommercialAuditEvent
            {
                Id = VersionPublishedAuditId,
                EventType = SaasCommercialAuditEventTypes.VersionPublished,
                EntityType = SaasCommercialAuditEntityTypes.PlanVersion,
                EntityId = PlanVersionId,
                PayloadJson = $$"""{"planCode":"{{PlanCode}}","versionNumber":1,"provenance":"{{Provenance}}"}""",
                ActorSubjectId = ActorReference,
                OccurredAtUtc = now,
            },
        ]);

    /// <summary>Tenant-scoped rows: written as the runtime role so RLS validates them.</summary>
    public static PackagedInstallSubscription BuildSubscription(
        Guid tenantId,
        DateTimeOffset now,
        PackagedInstallLicenseOptions? options = null)
    {
        var periodYears = (options ?? PackagedInstallLicenseOptions.Default).PeriodYears;
        var subscriptionId = Guid.NewGuid();
        var periodEnd = now.AddYears(periodYears);
        return new PackagedInstallSubscription(
            new SaasTenantSubscription
            {
                Id = subscriptionId,
                TenantId = tenantId,
                CommercialPlanVersionId = PlanVersionId,
                SubscriptionStatus = SaasSubscriptionStatuses.Active,
                BillingCadence = SaasCommercialBillingCadences.Annual,
                StartsAtUtc = now,
                CurrentPeriodStartUtc = now,
                CurrentPeriodEndUtc = periodEnd,
                TrialEndsAtUtc = null,
                GraceUntilUtc = null,
                SuspendedAtUtc = null,
                CancelledAtUtc = null,
                CancelAtPeriodEnd = false,
                SourceProvenance = Provenance,
                IsCurrent = true,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            new SaasTenantSubscriptionTransitionHistory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SubscriptionId = subscriptionId,
                FromStatus = "none",
                ToStatus = SaasSubscriptionStatuses.Active,
                TransitionReason = SaasSubscriptionTransitionReasons.PaymentReceived,
                ActorReference = ActorReference,
                OccurredAtUtc = now,
                EvidenceJson = $$"""{"commercialPlanVersionId":"{{PlanVersionId}}","provenance":"{{Provenance}}"}""",
            },
            new SaasSubscriptionAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventType = SaasSubscriptionAuditEventTypes.SubscriptionAssigned,
                EntityType = SaasSubscriptionAuditEntityTypes.TenantSubscription,
                EntityReference = subscriptionId.ToString("D"),
                ActorReference = ActorReference,
                PayloadJson = string.Create(
                    CultureInfo.InvariantCulture,
                    $$"""{"source":"{{Provenance}}","status":"active","periodYears":{{periodYears}}}"""),
                OccurredAtUtc = now,
            });
    }
}

public sealed record PackagedInstallCatalog(
    SaasCommercialPlan Plan,
    SaasCommercialPlanVersion PlanVersion,
    IReadOnlyList<SaasCommercialAuditEvent> AuditEvents);

public sealed record PackagedInstallSubscription(
    SaasTenantSubscription Subscription,
    SaasTenantSubscriptionTransitionHistory Transition,
    SaasSubscriptionAuditEvent AuditEvent);

/// <summary>
/// The single place the packaged-install licence period is configured. This is a default period for the
/// subscription granted to the first tenant, not a price, contract term or billing rule: a later licensing
/// flow replaces it. Override per machine with <see cref="PeriodYearsEnvironmentVariable"/>.
/// </summary>
public sealed record PackagedInstallLicenseOptions(int PeriodYears)
{
    public const string PeriodYearsEnvironmentVariable = "NEVAXIS_ZENTRA_PACKAGED_LICENSE_PERIOD_YEARS";

    public const int DefaultPeriodYears = 10;

    public const int MinimumPeriodYears = 1;

    public const int MaximumPeriodYears = 50;

    public static PackagedInstallLicenseOptions Default { get; } = new(DefaultPeriodYears);

    /// <summary>Environment override; a missing or out-of-range value falls back to the default.</summary>
    public static PackagedInstallLicenseOptions FromEnvironment() => Parse(
        Environment.GetEnvironmentVariable(PeriodYearsEnvironmentVariable));

    public static PackagedInstallLicenseOptions Parse(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var years)
        && years is >= MinimumPeriodYears and <= MaximumPeriodYears
            ? new PackagedInstallLicenseOptions(years)
            : Default;
}
