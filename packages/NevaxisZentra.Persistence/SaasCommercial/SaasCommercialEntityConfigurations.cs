using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.SaasCommercial;

internal sealed class SaasCommercialPlanEntityConfiguration : IEntityTypeConfiguration<SaasCommercialPlan>
{
    public void Configure(EntityTypeBuilder<SaasCommercialPlan> entity)
    {
        entity.ToTable("saas_commercial_plans");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.PlanCode).HasColumnName("plan_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.PlanCodeNormalized).HasColumnName("plan_code_normalized").HasMaxLength(100).IsRequired();
        entity.Property(p => p.DisplayNameTr).HasColumnName("display_name_tr").HasMaxLength(200).IsRequired();
        entity.Property(p => p.DescriptionTr).HasColumnName("description_tr").HasMaxLength(1000);
        entity.Property(p => p.LifecycleState).HasColumnName("lifecycle_state").HasMaxLength(30).IsRequired();
        entity.Property(p => p.AvailabilityState).HasColumnName("availability_state").HasMaxLength(30).IsRequired();
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(p => p.PlanCodeNormalized)
            .HasDatabaseName("ix_saas_commercial_plans_plan_code_normalized")
            .IsUnique();
    }
}

internal sealed class SaasCommercialPlanVersionEntityConfiguration : IEntityTypeConfiguration<SaasCommercialPlanVersion>
{
    public void Configure(EntityTypeBuilder<SaasCommercialPlanVersion> entity)
    {
        entity.ToTable("saas_commercial_plan_versions");
        entity.HasKey(v => v.Id);
        entity.Property(v => v.Id).HasColumnName("id");
        entity.Property(v => v.PlanId).HasColumnName("plan_id");
        entity.Property(v => v.VersionNumber).HasColumnName("version_number");
        entity.Property(v => v.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(v => v.EffectiveUntilUtc).HasColumnName("effective_until_utc");
        entity.Property(v => v.BillingCadence).HasColumnName("billing_cadence").HasMaxLength(20).IsRequired();
        entity.Property(v => v.TrialDays).HasColumnName("trial_days");
        entity.Property(v => v.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        entity.Property(v => v.Sellable).HasColumnName("sellable");
        entity.Property(v => v.PublicationStatus).HasColumnName("publication_status").HasMaxLength(20).IsRequired();
        entity.Property(v => v.CommercialNotes).HasColumnName("commercial_notes").HasMaxLength(2000);
        entity.Property(v => v.Provenance).HasColumnName("provenance").HasMaxLength(100);
        entity.Property(v => v.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(v => v.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasOne(v => v.Plan)
            .WithMany(p => p.Versions)
            .HasForeignKey(v => v.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(v => new { v.PlanId, v.VersionNumber })
            .HasDatabaseName("ix_saas_commercial_plan_versions_plan_version")
            .IsUnique();
        entity.HasIndex(v => new { v.PlanId, v.BillingCadence, v.EffectiveFromUtc })
            .HasDatabaseName("ix_saas_commercial_plan_versions_plan_cadence_effective");
    }
}

internal sealed class SaasCommercialPriceComponentEntityConfiguration : IEntityTypeConfiguration<SaasCommercialPriceComponent>
{
    public void Configure(EntityTypeBuilder<SaasCommercialPriceComponent> entity)
    {
        entity.ToTable("saas_commercial_price_components");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(c => c.ComponentType).HasColumnName("component_type").HasMaxLength(50).IsRequired();
        entity.Property(c => c.Amount).HasColumnName("amount").HasColumnType("numeric(18,4)");
        entity.Property(c => c.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        entity.Property(c => c.SortOrder).HasColumnName("sort_order");
        entity.HasOne(c => c.PlanVersion)
            .WithMany(v => v.PriceComponents)
            .HasForeignKey(c => c.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.PlanVersionId, c.ComponentType })
            .HasDatabaseName("ix_saas_commercial_price_components_version_type")
            .IsUnique();
    }
}

internal sealed class SaasCommercialEntitlementDefinitionEntityConfiguration : IEntityTypeConfiguration<SaasCommercialEntitlementDefinition>
{
    public void Configure(EntityTypeBuilder<SaasCommercialEntitlementDefinition> entity)
    {
        entity.ToTable("saas_commercial_entitlement_definitions");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.CodeNormalized).HasColumnName("code_normalized").HasMaxLength(100).IsRequired();
        entity.Property(d => d.DisplayNameTr).HasColumnName("display_name_tr").HasMaxLength(200).IsRequired();
        entity.Property(d => d.DescriptionTr).HasColumnName("description_tr").HasMaxLength(500);
        entity.Property(d => d.ValueKind).HasColumnName("value_kind").HasMaxLength(50).IsRequired();
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(d => d.CodeNormalized)
            .HasDatabaseName("ix_saas_commercial_entitlement_definitions_code_normalized")
            .IsUnique();
    }
}

internal sealed class SaasCommercialPlanVersionEntitlementEntityConfiguration : IEntityTypeConfiguration<SaasCommercialPlanVersionEntitlement>
{
    public void Configure(EntityTypeBuilder<SaasCommercialPlanVersionEntitlement> entity)
    {
        entity.ToTable("saas_commercial_plan_version_entitlements");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(e => e.EntitlementDefinitionId).HasColumnName("entitlement_definition_id");
        entity.Property(e => e.Semantics).HasColumnName("semantics").HasMaxLength(20).IsRequired();
        entity.Property(e => e.BooleanValue).HasColumnName("boolean_value");
        entity.Property(e => e.IntegerLimitValue).HasColumnName("integer_limit_value");
        entity.HasOne(e => e.PlanVersion)
            .WithMany(v => v.EntitlementGrants)
            .HasForeignKey(e => e.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.EntitlementDefinition)
            .WithMany(d => d.Grants)
            .HasForeignKey(e => e.EntitlementDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.PlanVersionId, e.EntitlementDefinitionId })
            .HasDatabaseName("ix_saas_commercial_plan_version_entitlements_version_definition")
            .IsUnique();
    }
}

internal sealed class SaasCommercialPaymentMethodCapabilityEntityConfiguration : IEntityTypeConfiguration<SaasCommercialPaymentMethodCapability>
{
    public void Configure(EntityTypeBuilder<SaasCommercialPaymentMethodCapability> entity)
    {
        entity.ToTable("saas_commercial_payment_method_capabilities");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(c => c.MethodType).HasColumnName("method_type").HasMaxLength(30).IsRequired();
        entity.Property(c => c.Enabled).HasColumnName("enabled");
        entity.Property(c => c.ManualVerificationRequired).HasColumnName("manual_verification_required");
        entity.Property(c => c.RecurringCapable).HasColumnName("recurring_capable");
        entity.Property(c => c.DisplayOrder).HasColumnName("display_order");
        entity.Property(c => c.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(c => c.EffectiveUntilUtc).HasColumnName("effective_until_utc");
        entity.HasOne(c => c.PlanVersion)
            .WithMany(v => v.PaymentMethodCapabilities)
            .HasForeignKey(c => c.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.PlanVersionId, c.MethodType })
            .HasDatabaseName("ix_saas_commercial_payment_method_capabilities_version_type")
            .IsUnique();
    }
}

internal sealed class SaasCommercialGracePeriodPolicyEntityConfiguration : IEntityTypeConfiguration<SaasCommercialGracePeriodPolicy>
{
    public void Configure(EntityTypeBuilder<SaasCommercialGracePeriodPolicy> entity)
    {
        entity.ToTable("saas_commercial_grace_period_policies");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.PlanVersionId).HasColumnName("plan_version_id");
        entity.Property(p => p.GracePeriodDays).HasColumnName("grace_period_days");
        entity.Property(p => p.RestrictedBehaviorToken).HasColumnName("restricted_behavior_token").HasMaxLength(100).IsRequired();
        entity.Property(p => p.SuspensionEligibilityPolicy).HasColumnName("suspension_eligibility_policy").HasMaxLength(100).IsRequired();
        entity.Property(p => p.OperatorExplanationTr).HasColumnName("operator_explanation_tr").HasMaxLength(1000).IsRequired();
        entity.HasOne(p => p.PlanVersion)
            .WithOne(v => v.GracePeriodPolicy)
            .HasForeignKey<SaasCommercialGracePeriodPolicy>(p => p.PlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(p => p.PlanVersionId)
            .HasDatabaseName("ix_saas_commercial_grace_period_policies_plan_version")
            .IsUnique();
    }
}

internal sealed class SaasCommercialAuditEventEntityConfiguration : IEntityTypeConfiguration<SaasCommercialAuditEvent>
{
    public void Configure(EntityTypeBuilder<SaasCommercialAuditEvent> entity)
    {
        entity.ToTable("saas_commercial_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityId).HasColumnName("entity_id");
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.HasIndex(e => e.OccurredAtUtc)
            .HasDatabaseName("ix_saas_commercial_audit_events_occurred_at_utc");
    }
}
