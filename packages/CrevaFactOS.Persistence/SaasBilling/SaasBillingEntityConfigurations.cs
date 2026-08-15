using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.SaasBilling;

internal sealed class SaasTenantBillingAccountEntityConfiguration : IEntityTypeConfiguration<SaasTenantBillingAccount>
{
    public void Configure(EntityTypeBuilder<SaasTenantBillingAccount> entity)
    {
        entity.ToTable("saas_tenant_billing_accounts");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.LegalName).HasColumnName("legal_name").HasMaxLength(200).IsRequired();
        entity.Property(item => item.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        entity.Property(item => item.BillingContactMasked).HasColumnName("billing_contact_masked").HasMaxLength(320).IsRequired();
        entity.Property(item => item.BillingAddressSnapshot).HasColumnName("billing_address_snapshot").HasMaxLength(500).IsRequired();
        entity.Property(item => item.TaxCountryCode).HasColumnName("tax_country_code").HasMaxLength(2);
        entity.Property(item => item.TaxNumberMasked).HasColumnName("tax_number_masked").HasMaxLength(40);
        entity.Property(item => item.PreferredBillingCadence).HasColumnName("preferred_billing_cadence").HasMaxLength(20).IsRequired();
        entity.Property(item => item.PreferredPaymentMethod).HasColumnName("preferred_payment_method").HasMaxLength(30);
        entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => item.TenantId)
            .IsUnique()
            .HasDatabaseName("ux_saas_tenant_billing_accounts_tenant");
    }
}

internal sealed class SaasBillingInvoiceEntityConfiguration : IEntityTypeConfiguration<SaasBillingInvoice>
{
    public void Configure(EntityTypeBuilder<SaasBillingInvoice> entity)
    {
        entity.ToTable("saas_billing_invoices");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.BillingAccountId).HasColumnName("billing_account_id");
        entity.Property(item => item.SubscriptionId).HasColumnName("subscription_id");
        entity.Property(item => item.CommercialPlanVersionId).HasColumnName("commercial_plan_version_id");
        entity.Property(item => item.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(40).IsRequired();
        entity.Property(item => item.PeriodStartUtc).HasColumnName("period_start_utc");
        entity.Property(item => item.PeriodEndUtc).HasColumnName("period_end_utc");
        entity.Property(item => item.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        entity.Property(item => item.SubtotalAmount).HasColumnName("subtotal_amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.TaxAmount).HasColumnName("tax_amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.InvoiceStatus).HasColumnName("invoice_status").HasMaxLength(20).IsRequired();
        entity.Property(item => item.IssuedAtUtc).HasColumnName("issued_at_utc");
        entity.Property(item => item.DueAtUtc).HasColumnName("due_at_utc");
        entity.Property(item => item.PaidAtUtc).HasColumnName("paid_at_utc");
        entity.Property(item => item.VoidedAtUtc).HasColumnName("voided_at_utc");
        entity.Property(item => item.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(100).IsRequired();
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.InvoiceSequence).HasColumnName("invoice_sequence");
        entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<SaasTenantBillingAccount>().WithMany()
            .HasForeignKey(item => item.BillingAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("ux_saas_billing_invoices_tenant_number");
        entity.HasIndex(item => new { item.TenantId, item.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_saas_billing_invoices_idempotency")
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(item => new { item.TenantId, item.InvoiceStatus, item.DueAtUtc })
            .HasDatabaseName("ix_saas_billing_invoices_tenant_status_due");
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_saas_billing_invoices_status",
            "invoice_status IN ('draft', 'open', 'paid', 'overdue', 'void')"));
    }
}

internal sealed class SaasBillingInvoiceLineEntityConfiguration : IEntityTypeConfiguration<SaasBillingInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SaasBillingInvoiceLine> entity)
    {
        entity.ToTable("saas_billing_invoice_lines");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.InvoiceId).HasColumnName("invoice_id");
        entity.Property(item => item.DescriptionSnapshot).HasColumnName("description_snapshot").HasMaxLength(300).IsRequired();
        entity.Property(item => item.ComponentType).HasColumnName("component_type").HasMaxLength(40).IsRequired();
        entity.Property(item => item.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,4)");
        entity.Property(item => item.UnitAmount).HasColumnName("unit_amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.LineSubtotalAmount).HasColumnName("line_subtotal_amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(100).IsRequired();
        entity.Property(item => item.SortOrder).HasColumnName("sort_order");
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<SaasBillingInvoice>().WithMany(item => item.Lines)
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.InvoiceId, item.SortOrder })
            .HasDatabaseName("ix_saas_billing_invoice_lines_invoice_sort");
    }
}

internal sealed class SaasBillingPaymentAttemptEntityConfiguration : IEntityTypeConfiguration<SaasBillingPaymentAttempt>
{
    public void Configure(EntityTypeBuilder<SaasBillingPaymentAttempt> entity)
    {
        entity.ToTable("saas_billing_payment_attempts");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.InvoiceId).HasColumnName("invoice_id");
        entity.Property(item => item.PaymentMethod).HasColumnName("payment_method").HasMaxLength(30).IsRequired();
        entity.Property(item => item.ProviderKey).HasColumnName("provider_key").HasMaxLength(60);
        entity.Property(item => item.ExternalReferenceMasked).HasColumnName("external_reference_masked").HasMaxLength(80);
        entity.Property(item => item.Amount).HasColumnName("amount").HasColumnType("numeric(18,4)");
        entity.Property(item => item.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        entity.Property(item => item.PaymentStatus).HasColumnName("payment_status").HasMaxLength(40).IsRequired();
        entity.Property(item => item.FailureCode).HasColumnName("failure_code").HasMaxLength(80);
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<SaasBillingInvoice>().WithMany()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(item => new { item.TenantId, item.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_saas_billing_payment_attempts_idempotency")
            .HasFilter("idempotency_key IS NOT NULL");
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_saas_billing_payment_attempts_status",
            "payment_status IN ('pending', 'awaiting-manual-verification', 'succeeded', 'failed', 'cancelled', 'refunded')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_saas_billing_payment_attempts_method",
            "payment_method IN ('card', 'bank-transfer', 'enterprise-invoice')"));
    }
}

internal sealed class SaasBillingIdempotencyRecordEntityConfiguration : IEntityTypeConfiguration<SaasBillingIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<SaasBillingIdempotencyRecord> entity)
    {
        entity.ToTable("saas_billing_idempotency_records");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        entity.Property(item => item.Operation).HasColumnName("operation").HasMaxLength(80);
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(item => new { item.TenantId, item.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_saas_billing_idempotency_key");
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SaasBillingAuditEventEntityConfiguration : IEntityTypeConfiguration<SaasBillingAuditEvent>
{
    public void Configure(EntityTypeBuilder<SaasBillingAuditEvent> entity)
    {
        entity.ToTable("saas_billing_audit_events");
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
            .HasDatabaseName("ix_saas_billing_audit_events_tenant_occurred");
        entity.HasOne<Tenant>().WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
