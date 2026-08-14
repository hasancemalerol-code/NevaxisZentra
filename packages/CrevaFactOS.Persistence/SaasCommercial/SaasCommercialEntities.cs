namespace CrevaFactOS.Persistence.SaasCommercial;

public sealed class SaasCommercialPlan
{
    public Guid Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string PlanCodeNormalized { get; set; } = string.Empty;
    public string DisplayNameTr { get; set; } = string.Empty;
    public string? DescriptionTr { get; set; }
    public string LifecycleState { get; set; } = SaasCommercialPlanLifecycleStates.Draft;
    public string AvailabilityState { get; set; } = SaasCommercialAvailabilityStates.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public ICollection<SaasCommercialPlanVersion> Versions { get; set; } = [];
}

public sealed class SaasCommercialPlanVersion
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int VersionNumber { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveUntilUtc { get; set; }
    public string BillingCadence { get; set; } = SaasCommercialBillingCadences.Monthly;
    public int TrialDays { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public bool Sellable { get; set; }
    public string PublicationStatus { get; set; } = SaasCommercialPublicationStatuses.Draft;
    public string? CommercialNotes { get; set; }
    public string? Provenance { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public SaasCommercialPlan? Plan { get; set; }
    public ICollection<SaasCommercialPriceComponent> PriceComponents { get; set; } = [];
    public ICollection<SaasCommercialPlanVersionEntitlement> EntitlementGrants { get; set; } = [];
    public ICollection<SaasCommercialPaymentMethodCapability> PaymentMethodCapabilities { get; set; } = [];
    public SaasCommercialGracePeriodPolicy? GracePeriodPolicy { get; set; }
}

public sealed class SaasCommercialPriceComponent
{
    public Guid Id { get; set; }
    public Guid PlanVersionId { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public int SortOrder { get; set; }

    public SaasCommercialPlanVersion? PlanVersion { get; set; }
}

public sealed class SaasCommercialEntitlementDefinition
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CodeNormalized { get; set; } = string.Empty;
    public string DisplayNameTr { get; set; } = string.Empty;
    public string? DescriptionTr { get; set; }
    public string ValueKind { get; set; } = SaasCommercialEntitlementValueKinds.FeatureAccess;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<SaasCommercialPlanVersionEntitlement> Grants { get; set; } = [];
}

public sealed class SaasCommercialPlanVersionEntitlement
{
    public Guid Id { get; set; }
    public Guid PlanVersionId { get; set; }
    public Guid EntitlementDefinitionId { get; set; }
    public string Semantics { get; set; } = SaasCommercialEntitlementSemantics.Unknown;
    public bool? BooleanValue { get; set; }
    public int? IntegerLimitValue { get; set; }

    public SaasCommercialPlanVersion? PlanVersion { get; set; }
    public SaasCommercialEntitlementDefinition? EntitlementDefinition { get; set; }
}

public sealed class SaasCommercialPaymentMethodCapability
{
    public Guid Id { get; set; }
    public Guid PlanVersionId { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool ManualVerificationRequired { get; set; }
    public bool RecurringCapable { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveUntilUtc { get; set; }

    public SaasCommercialPlanVersion? PlanVersion { get; set; }
}

public sealed class SaasCommercialGracePeriodPolicy
{
    public Guid Id { get; set; }
    public Guid PlanVersionId { get; set; }
    public int GracePeriodDays { get; set; }
    public string RestrictedBehaviorToken { get; set; } = string.Empty;
    public string SuspensionEligibilityPolicy { get; set; } = string.Empty;
    public string OperatorExplanationTr { get; set; } = string.Empty;

    public SaasCommercialPlanVersion? PlanVersion { get; set; }
}

public sealed class SaasCommercialAuditEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
