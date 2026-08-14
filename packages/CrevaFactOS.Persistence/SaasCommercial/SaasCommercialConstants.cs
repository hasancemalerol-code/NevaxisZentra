namespace CrevaFactOS.Persistence.SaasCommercial;

public static class SaasCommercialPlanLifecycleStates
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Retired = "retired";
}

public static class SaasCommercialAvailabilityStates
{
    public const string Draft = "draft";
    public const string Available = "available";
    public const string Unavailable = "unavailable";
}

public static class SaasCommercialBillingCadences
{
    public const string Monthly = "monthly";
    public const string Annual = "annual";
}

public static class SaasCommercialPublicationStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Retired = "retired";
}

public static class SaasCommercialPriceComponentTypes
{
    public const string TenantBase = "tenant-base";
    public const string IncludedFacility = "included-facility";
    public const string AdditionalFacility = "additional-facility";
    public const string IncludedConnectedMachine = "included-connected-machine";
    public const string AdditionalConnectedMachine = "additional-connected-machine";
    public const string IncludedUser = "included-user";
    public const string AdditionalUser = "additional-user";
}

public static class SaasCommercialEntitlementValueKinds
{
    public const string ModuleAccess = "module-access";
    public const string FeatureAccess = "feature-access";
    public const string FacilityLimit = "facility-limit";
    public const string ConnectedMachineLimit = "connected-machine-limit";
    public const string UserLimit = "user-limit";
    public const string BooleanCapability = "boolean-capability";
    public const string IntegerLimit = "integer-limit";
}

public static class SaasCommercialEntitlementSemantics
{
    public const string Unavailable = "unavailable";
    public const string Limited = "limited";
    public const string Unlimited = "unlimited";
    public const string Unknown = "unknown";
}

public static class SaasCommercialPaymentMethodTypes
{
    public const string Card = "card";
    public const string BankTransfer = "bank-transfer";
    public const string EnterpriseInvoice = "enterprise-invoice";
}

public static class SaasCommercialCatalogProvenance
{
    public const string LocalDevelopmentPilot = "local-development-pilot";
    public const string NonProductionDraft = "non-production-draft";
}

public static class SaasCommercialAuditEventTypes
{
    public const string PlanCreated = "saas-commercial.plan.created";
    public const string PlanUpdated = "saas-commercial.plan.updated";
    public const string VersionCreated = "saas-commercial.version.created";
    public const string VersionPublished = "saas-commercial.version.published";
    public const string VersionRetired = "saas-commercial.version.retired";
}

public static class SaasCommercialAuditEntityTypes
{
    public const string Plan = "saas-commercial-plan";
    public const string PlanVersion = "saas-commercial-plan-version";
}

public static class SaasCommercialValueConversions
{
    public static string NormalizePlanCode(string planCode) =>
        planCode.Trim().ToLowerInvariant();

    public static string NormalizeEntitlementCode(string code) =>
        code.Trim().ToLowerInvariant();

    public static bool IsValidCurrencyCode(string? currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode)
        && currencyCode.Length == 3
        && currencyCode.All(c => char.IsAsciiLetter(c));

    public static bool IsValidBillingCadence(string cadence) =>
        cadence is SaasCommercialBillingCadences.Monthly or SaasCommercialBillingCadences.Annual;

    public static bool IsValidPaymentMethodType(string methodType) =>
        methodType is SaasCommercialPaymentMethodTypes.Card
            or SaasCommercialPaymentMethodTypes.BankTransfer
            or SaasCommercialPaymentMethodTypes.EnterpriseInvoice;

    public static bool IsValidEntitlementSemantics(string semantics) =>
        semantics is SaasCommercialEntitlementSemantics.Unavailable
            or SaasCommercialEntitlementSemantics.Limited
            or SaasCommercialEntitlementSemantics.Unlimited
            or SaasCommercialEntitlementSemantics.Unknown;

    public static bool IsPublishedImmutable(string publicationStatus) =>
        publicationStatus is SaasCommercialPublicationStatuses.Published
            or SaasCommercialPublicationStatuses.Retired;

    public static bool PeriodsOverlap(
        DateTimeOffset fromA,
        DateTimeOffset? untilA,
        DateTimeOffset fromB,
        DateTimeOffset? untilB)
    {
        var endA = untilA ?? DateTimeOffset.MaxValue;
        var endB = untilB ?? DateTimeOffset.MaxValue;
        return fromA < endB && fromB < endA;
    }
}
