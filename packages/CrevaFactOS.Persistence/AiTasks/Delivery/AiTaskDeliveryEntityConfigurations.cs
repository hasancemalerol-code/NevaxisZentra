using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public sealed class AiTaskDeliveryEntityConfiguration : IEntityTypeConfiguration<AiTaskDelivery>
{
    public void Configure(EntityTypeBuilder<AiTaskDelivery> builder)
    {
        builder.ToTable("ai_task_deliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.Channel).HasColumnName("channel").HasMaxLength(50).IsRequired();
        builder.Property(d => d.RecipientKind).HasColumnName("recipient_kind").HasMaxLength(30).IsRequired();
        builder.Property(d => d.RecipientUserId).HasColumnName("recipient_user_id");
        builder.Property(d => d.MaskedRecipient).HasColumnName("masked_recipient").HasMaxLength(200);
        builder.Property(d => d.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(d => d.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        builder.Property(d => d.PayloadVersion).HasColumnName("payload_version");
        builder.Property(d => d.DeliveryProjectionJson).HasColumnName("delivery_projection_json");
        builder.Property(d => d.AttemptCount).HasColumnName("attempt_count");
        builder.Property(d => d.MaxAttempts).HasColumnName("max_attempts");
        builder.Property(d => d.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
        builder.Property(d => d.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(100);
        builder.Property(d => d.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
        builder.Property(d => d.Provider).HasColumnName("provider").HasMaxLength(50);
        builder.Property(d => d.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(200);
        builder.Property(d => d.ProviderCorrelationId).HasColumnName("provider_correlation_id").HasMaxLength(200);
        builder.Property(d => d.ProviderStatus).HasColumnName("provider_status").HasMaxLength(30);
        builder.Property(d => d.ProviderStatusAtUtc).HasColumnName("provider_status_at_utc");
        builder.Property(d => d.RecipientAddressProtected).HasColumnName("recipient_address_protected");
        builder.Property(d => d.ConsentStatus).HasColumnName("consent_status").HasMaxLength(30);
        builder.Property(d => d.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(100);
        builder.Property(d => d.LastErrorCategory).HasColumnName("last_error_category").HasMaxLength(30);
        builder.Property(d => d.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(d => d.RequestedAtUtc).HasColumnName("requested_at_utc");
        builder.Property(d => d.SentAtUtc).HasColumnName("sent_at_utc");
        builder.Property(d => d.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        builder.Property(d => d.SecureAccessGrantId).HasColumnName("secure_access_grant_id");
        builder.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(d => d.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.FacilityId).HasColumnName("facility_id");
        builder.Property(d => d.AiTaskId).HasColumnName("ai_task_id");

        builder.HasIndex(d => new { d.TenantId, d.AiTaskId }).HasDatabaseName("ix_ai_task_deliveries_tenant_task");
        builder.HasIndex(d => new { d.TenantId, d.Status, d.NextAttemptAtUtc })
            .HasDatabaseName("ix_ai_task_deliveries_tenant_status_next");
        builder.HasIndex(d => d.LeaseExpiresAtUtc).HasDatabaseName("ix_ai_task_deliveries_lease_expiry");
        builder.HasIndex(d => new { d.TenantId, d.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_deliveries_tenant_idempotency");

        builder.HasMany(d => d.Attempts)
            .WithOne(a => a.Delivery)
            .HasForeignKey(a => a.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AiTaskDeliveryAttemptEntityConfiguration : IEntityTypeConfiguration<AiTaskDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<AiTaskDeliveryAttempt> builder)
    {
        builder.ToTable("ai_task_delivery_attempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.AttemptNumber).HasColumnName("attempt_number");
        builder.Property(a => a.AdapterKey).HasColumnName("adapter_key").HasMaxLength(50).IsRequired();
        builder.Property(a => a.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(a => a.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(a => a.Outcome).HasColumnName("outcome").HasMaxLength(30).IsRequired();
        builder.Property(a => a.ProviderStatusCode).HasColumnName("provider_status_code").HasMaxLength(50);
        builder.Property(a => a.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(200);
        builder.Property(a => a.ErrorCategory).HasColumnName("error_category").HasMaxLength(30);
        builder.Property(a => a.SanitizedErrorCode).HasColumnName("sanitized_error_code").HasMaxLength(100);
        builder.Property(a => a.RetryScheduledForUtc).HasColumnName("retry_scheduled_for_utc");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.DeliveryId).HasColumnName("delivery_id");

        builder.HasIndex(a => new { a.DeliveryId, a.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_delivery_attempts_delivery_number");
    }
}

public sealed class AiTaskSecureAccessGrantEntityConfiguration : IEntityTypeConfiguration<AiTaskSecureAccessGrant>
{
    public void Configure(EntityTypeBuilder<AiTaskSecureAccessGrant> builder)
    {
        builder.ToTable("ai_task_secure_access_grants");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");
        builder.Property(g => g.Scope).HasColumnName("scope").HasMaxLength(30).IsRequired();
        builder.Property(g => g.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(g => g.IssuedByUserId).HasColumnName("issued_by_user_id");
        builder.Property(g => g.IssuedAtUtc).HasColumnName("issued_at_utc");
        builder.Property(g => g.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(g => g.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(g => g.RevokedByUserId).HasColumnName("revoked_by_user_id");
        builder.Property(g => g.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(500);
        builder.Property(g => g.LastAccessedAtUtc).HasColumnName("last_accessed_at_utc");
        builder.Property(g => g.AccessCount).HasColumnName("access_count");
        builder.Property(g => g.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(g => g.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(g => g.TenantId).HasColumnName("tenant_id");
        builder.Property(g => g.FacilityId).HasColumnName("facility_id");
        builder.Property(g => g.AiTaskId).HasColumnName("ai_task_id");

        builder.HasIndex(g => new { g.TenantId, g.AiTaskId }).HasDatabaseName("ix_ai_task_secure_access_grants_tenant_task");
        builder.HasIndex(g => g.ExpiresAtUtc).HasDatabaseName("ix_ai_task_secure_access_grants_expiry");
        builder.HasIndex(g => new { g.TenantId, g.TokenHash })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_secure_access_grants_tenant_token_hash");
    }
}
