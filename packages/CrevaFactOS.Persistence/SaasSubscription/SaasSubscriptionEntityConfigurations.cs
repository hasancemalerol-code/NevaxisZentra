using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.SaasSubscription;

internal sealed class SaasTenantSubscriptionEntityConfiguration : IEntityTypeConfiguration<SaasTenantSubscription>
{
    public void Configure(EntityTypeBuilder<SaasTenantSubscription> entity)
    {
        entity.ToTable("saas_tenant_subscriptions");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.CommercialPlanVersionId).HasColumnName("commercial_plan_version_id");
        entity.Property(item => item.SubscriptionStatus).HasColumnName("subscription_status").HasMaxLength(30).IsRequired();
        entity.Property(item => item.BillingCadence).HasColumnName("billing_cadence").HasMaxLength(20).IsRequired();
        entity.Property(item => item.StartsAtUtc).HasColumnName("starts_at_utc");
        entity.Property(item => item.CurrentPeriodStartUtc).HasColumnName("current_period_start_utc");
        entity.Property(item => item.CurrentPeriodEndUtc).HasColumnName("current_period_end_utc");
        entity.Property(item => item.TrialEndsAtUtc).HasColumnName("trial_ends_at_utc");
        entity.Property(item => item.GraceUntilUtc).HasColumnName("grace_until_utc");
        entity.Property(item => item.SuspendedAtUtc).HasColumnName("suspended_at_utc");
        entity.Property(item => item.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        entity.Property(item => item.CancelAtPeriodEnd).HasColumnName("cancel_at_period_end");
        entity.Property(item => item.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(100).IsRequired();
        entity.Property(item => item.IsCurrent).HasColumnName("is_current");
        entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<SaasCommercialPlanVersion>().WithMany()
            .HasForeignKey(item => item.CommercialPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.IsCurrent })
            .HasDatabaseName("ix_saas_tenant_subscriptions_tenant_current");
        entity.HasIndex(item => item.TenantId)
            .HasDatabaseName("ux_saas_tenant_subscriptions_tenant_current")
            .IsUnique()
            .HasFilter("is_current = TRUE");
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_saas_tenant_subscriptions_status",
            "subscription_status IN ('trialing', 'active', 'grace-period', 'suspended', 'cancelled', 'expired')"));
    }
}

internal sealed class SaasTenantSubscriptionTransitionHistoryEntityConfiguration
    : IEntityTypeConfiguration<SaasTenantSubscriptionTransitionHistory>
{
    public void Configure(EntityTypeBuilder<SaasTenantSubscriptionTransitionHistory> entity)
    {
        entity.ToTable("saas_tenant_subscription_transition_history");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.SubscriptionId).HasColumnName("subscription_id");
        entity.Property(item => item.FromStatus).HasColumnName("from_status").HasMaxLength(30).IsRequired();
        entity.Property(item => item.ToStatus).HasColumnName("to_status").HasMaxLength(30).IsRequired();
        entity.Property(item => item.TransitionReason).HasColumnName("transition_reason").HasMaxLength(80).IsRequired();
        entity.Property(item => item.ActorReference).HasColumnName("actor_reference").HasMaxLength(120).IsRequired();
        entity.Property(item => item.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.EvidenceJson).HasColumnName("evidence_json").IsRequired();
        entity.HasOne<SaasTenantSubscription>().WithMany()
            .HasForeignKey(item => item.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.SubscriptionId, item.OccurredAtUtc })
            .HasDatabaseName("ix_saas_tenant_subscription_transition_history_lookup");
        entity.HasIndex(item => new { item.TenantId, item.IdempotencyKey })
            .HasDatabaseName("ux_saas_tenant_subscription_transition_history_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
    }
}

internal sealed class SaasSubscriptionIdempotencyRecordEntityConfiguration
    : IEntityTypeConfiguration<SaasSubscriptionIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<SaasSubscriptionIdempotencyRecord> entity)
    {
        entity.ToTable("saas_subscription_idempotency_records");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        entity.Property(item => item.Operation).HasColumnName("operation").HasMaxLength(80);
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(item => new { item.TenantId, item.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_saas_subscription_idempotency_key");
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SaasSubscriptionAuditEventEntityConfiguration
    : IEntityTypeConfiguration<SaasSubscriptionAuditEvent>
{
    public void Configure(EntityTypeBuilder<SaasSubscriptionAuditEvent> entity)
    {
        entity.ToTable("saas_subscription_audit_events");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(item => item.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        entity.Property(item => item.EntityReference).HasColumnName("entity_reference").HasMaxLength(120).IsRequired();
        entity.Property(item => item.ActorReference).HasColumnName("actor_reference").HasMaxLength(120).IsRequired();
        entity.Property(item => item.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.Property(item => item.CorrelationId).HasColumnName("correlation_id");
        entity.Property(item => item.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.HasIndex(item => new { item.TenantId, item.OccurredAtUtc })
            .HasDatabaseName("ix_saas_subscription_audit_events_tenant_occurred");
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
