using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.SaasBilling;

internal sealed class SaasBillingCheckoutSessionEntityConfiguration
    : IEntityTypeConfiguration<SaasBillingCheckoutSession>
{
    public void Configure(EntityTypeBuilder<SaasBillingCheckoutSession> entity)
    {
        entity.ToTable("saas_billing_checkout_sessions");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.PaymentAttemptId).HasColumnName("payment_attempt_id");
        entity.Property(item => item.InvoiceId).HasColumnName("invoice_id");
        entity.Property(item => item.ProviderKey).HasColumnName("provider_key").HasMaxLength(60).IsRequired();
        entity.Property(item => item.ConversationId).HasColumnName("conversation_id").HasMaxLength(120).IsRequired();
        entity.Property(item => item.CheckoutSessionReferenceMasked)
            .HasColumnName("checkout_session_reference_masked")
            .HasMaxLength(80)
            .IsRequired();
        entity.Property(item => item.HostedCheckoutUrl).HasColumnName("hosted_checkout_url").HasMaxLength(500);
        entity.Property(item => item.SessionStatus).HasColumnName("session_status").HasMaxLength(20).IsRequired();
        entity.Property(item => item.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<SaasBillingPaymentAttempt>().WithMany()
            .HasForeignKey(item => item.PaymentAttemptId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<SaasBillingInvoice>().WithMany()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.CheckoutSessionReferenceMasked })
            .IsUnique()
            .HasDatabaseName("ux_saas_billing_checkout_sessions_reference");
        entity.HasIndex(item => new { item.TenantId, item.PaymentAttemptId })
            .HasDatabaseName("ix_saas_billing_checkout_sessions_attempt");
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_saas_billing_checkout_sessions_status",
            "session_status IN ('pending', 'completed', 'expired', 'cancelled')"));
    }
}

internal sealed class SaasBillingProviderEventEntityConfiguration
    : IEntityTypeConfiguration<SaasBillingProviderEvent>
{
    public void Configure(EntityTypeBuilder<SaasBillingProviderEvent> entity)
    {
        entity.ToTable("saas_billing_provider_events");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.ProviderKey).HasColumnName("provider_key").HasMaxLength(60).IsRequired();
        entity.Property(item => item.ProviderEventIdentityHash)
            .HasColumnName("provider_event_identity_hash")
            .HasMaxLength(64)
            .IsRequired();
        entity.Property(item => item.PaymentAttemptId).HasColumnName("payment_attempt_id");
        entity.Property(item => item.InvoiceId).HasColumnName("invoice_id");
        entity.Property(item => item.NormalizedStatus).HasColumnName("normalized_status").HasMaxLength(20).IsRequired();
        entity.Property(item => item.Amount).HasColumnName("amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        entity.Property(item => item.SanitizedEvidenceJson).HasColumnName("sanitized_evidence_json").IsRequired();
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.ProcessedAtUtc).HasColumnName("processed_at_utc");
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.ProviderEventIdentityHash })
            .IsUnique()
            .HasDatabaseName("ux_saas_billing_provider_events_identity");
        entity.HasIndex(item => new { item.TenantId, item.PaymentAttemptId })
            .HasDatabaseName("ix_saas_billing_provider_events_attempt");
    }
}
