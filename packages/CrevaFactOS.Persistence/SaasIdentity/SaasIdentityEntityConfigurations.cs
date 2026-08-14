using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.SaasIdentity;

internal sealed class SaasIdentityAccountEntityConfiguration : IEntityTypeConfiguration<SaasIdentityAccount>
{
    public void Configure(EntityTypeBuilder<SaasIdentityAccount> entity)
    {
        entity.ToTable("saas_identity_accounts");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.AccountReference).HasColumnName("account_reference").HasMaxLength(100).IsRequired();
        entity.Property(a => a.EmailNormalized).HasColumnName("email_normalized").HasMaxLength(320).IsRequired();
        entity.Property(a => a.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        entity.Property(a => a.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.EmailVerifiedAtUtc).HasColumnName("email_verified_at_utc");
        entity.Property(a => a.FailedSignInCount).HasColumnName("failed_sign_in_count");
        entity.Property(a => a.LockoutUntilUtc).HasColumnName("lockout_until_utc");
        entity.Property(a => a.SecurityStamp).HasColumnName("security_stamp");
        entity.Property(a => a.IdentitySubjectId).HasColumnName("identity_subject_id");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasOne<IdentitySubject>()
            .WithMany()
            .HasForeignKey(a => a.IdentitySubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => a.AccountReference)
            .HasDatabaseName("ix_saas_identity_accounts_account_reference")
            .IsUnique();
        entity.HasIndex(a => a.EmailNormalized)
            .HasDatabaseName("ix_saas_identity_accounts_email_normalized")
            .IsUnique();
        entity.HasIndex(a => a.IdentitySubjectId)
            .HasDatabaseName("ix_saas_identity_accounts_identity_subject_id")
            .IsUnique();
    }
}

internal sealed class SaasIdentityPasswordCredentialEntityConfiguration : IEntityTypeConfiguration<SaasIdentityPasswordCredential>
{
    public void Configure(EntityTypeBuilder<SaasIdentityPasswordCredential> entity)
    {
        entity.ToTable("saas_identity_password_credentials");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.AccountId).HasColumnName("account_id");
        entity.Property(c => c.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        entity.Property(c => c.PasswordHashVersion).HasColumnName("password_hash_version");
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<SaasIdentityAccount>()
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => c.AccountId)
            .HasDatabaseName("ix_saas_identity_password_credentials_account_id")
            .IsUnique();
    }
}

internal sealed class SaasIdentitySessionEntityConfiguration : IEntityTypeConfiguration<SaasIdentitySession>
{
    public void Configure(EntityTypeBuilder<SaasIdentitySession> entity)
    {
        entity.ToTable("saas_identity_sessions");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.SessionReference).HasColumnName("session_reference").HasMaxLength(100).IsRequired();
        entity.Property(s => s.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        entity.Property(s => s.AccountId).HasColumnName("account_id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(s => s.SecurityStampSnapshot).HasColumnName("security_stamp_snapshot");
        entity.Property(s => s.DeviceLabel).HasColumnName("device_label").HasMaxLength(200);
        entity.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(s => s.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        entity.Property(s => s.IdleExpiresAtUtc).HasColumnName("idle_expires_at_utc");
        entity.Property(s => s.AbsoluteExpiresAtUtc).HasColumnName("absolute_expires_at_utc");
        entity.Property(s => s.RevokedAtUtc).HasColumnName("revoked_at_utc");
        entity.Property(s => s.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(100);
        entity.Property(s => s.MfaSatisfied).HasColumnName("mfa_satisfied");
        entity.Property(s => s.PendingMfa).HasColumnName("pending_mfa");
        entity.HasOne<SaasIdentityAccount>()
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(s => s.TenantMembershipId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(s => s.TokenHash)
            .HasDatabaseName("ix_saas_identity_sessions_token_hash")
            .IsUnique();
        entity.HasIndex(s => s.SessionReference)
            .HasDatabaseName("ix_saas_identity_sessions_session_reference")
            .IsUnique();
        entity.HasIndex(s => new { s.AccountId, s.RevokedAtUtc })
            .HasDatabaseName("ix_saas_identity_sessions_account_revoked");
        entity.HasIndex(s => s.TenantId)
            .HasDatabaseName("ix_saas_identity_sessions_tenant_id");
    }
}

internal sealed class SaasIdentityInvitationEntityConfiguration : IEntityTypeConfiguration<SaasIdentityInvitation>
{
    public void Configure(EntityTypeBuilder<SaasIdentityInvitation> entity)
    {
        entity.ToTable("saas_identity_invitations");
        entity.HasKey(i => i.Id);
        entity.Property(i => i.Id).HasColumnName("id");
        entity.Property(i => i.InvitationReference).HasColumnName("invitation_reference").HasMaxLength(100).IsRequired();
        entity.Property(i => i.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        entity.Property(i => i.TenantId).HasColumnName("tenant_id");
        entity.Property(i => i.IntendedEmailNormalized).HasColumnName("intended_email_normalized").HasMaxLength(320).IsRequired();
        entity.Property(i => i.IntendedRoleId).HasColumnName("intended_role_id");
        entity.Property(i => i.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(i => i.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        entity.Property(i => i.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(i => i.TokenHash)
            .HasDatabaseName("ix_saas_identity_invitations_token_hash")
            .IsUnique();
        entity.HasIndex(i => i.InvitationReference)
            .HasDatabaseName("ix_saas_identity_invitations_invitation_reference")
            .IsUnique();
        entity.HasIndex(i => new { i.TenantId, i.IntendedEmailNormalized, i.ConsumedAtUtc })
            .HasDatabaseName("ix_saas_identity_invitations_tenant_email_consumed");
    }
}

internal sealed class SaasIdentityEmailVerificationTokenEntityConfiguration
    : IEntityTypeConfiguration<SaasIdentityEmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<SaasIdentityEmailVerificationToken> entity)
    {
        entity.ToTable("saas_identity_email_verification_tokens");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.AccountId).HasColumnName("account_id");
        entity.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        entity.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(t => t.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<SaasIdentityAccount>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => t.TokenHash)
            .HasDatabaseName("ix_saas_identity_email_verification_tokens_token_hash")
            .IsUnique();
        entity.HasIndex(t => new { t.AccountId, t.ConsumedAtUtc })
            .HasDatabaseName("ix_saas_identity_email_verification_tokens_account_consumed");
    }
}

internal sealed class SaasIdentityPasswordResetTokenEntityConfiguration
    : IEntityTypeConfiguration<SaasIdentityPasswordResetToken>
{
    public void Configure(EntityTypeBuilder<SaasIdentityPasswordResetToken> entity)
    {
        entity.ToTable("saas_identity_password_reset_tokens");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.AccountId).HasColumnName("account_id");
        entity.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        entity.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(t => t.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<SaasIdentityAccount>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => t.TokenHash)
            .HasDatabaseName("ix_saas_identity_password_reset_tokens_token_hash")
            .IsUnique();
        entity.HasIndex(t => new { t.AccountId, t.ConsumedAtUtc })
            .HasDatabaseName("ix_saas_identity_password_reset_tokens_account_consumed");
    }
}

internal sealed class SaasIdentityMfaMethodEntityConfiguration : IEntityTypeConfiguration<SaasIdentityMfaMethod>
{
    public void Configure(EntityTypeBuilder<SaasIdentityMfaMethod> entity)
    {
        entity.ToTable("saas_identity_mfa_methods");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.AccountId).HasColumnName("account_id");
        entity.Property(m => m.EncryptedSecret).HasColumnName("encrypted_secret").HasMaxLength(1000).IsRequired();
        entity.Property(m => m.IsRequired).HasColumnName("is_required");
        entity.Property(m => m.EnrolledAtUtc).HasColumnName("enrolled_at_utc");
        entity.HasOne<SaasIdentityAccount>()
            .WithMany()
            .HasForeignKey(m => m.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(m => m.AccountId)
            .HasDatabaseName("ix_saas_identity_mfa_methods_account_id")
            .IsUnique();
    }
}

internal sealed class SaasIdentityRecoveryCodeEntityConfiguration : IEntityTypeConfiguration<SaasIdentityRecoveryCode>
{
    public void Configure(EntityTypeBuilder<SaasIdentityRecoveryCode> entity)
    {
        entity.ToTable("saas_identity_recovery_codes");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.MfaMethodId).HasColumnName("mfa_method_id");
        entity.Property(c => c.CodeHash).HasColumnName("code_hash").HasMaxLength(128).IsRequired();
        entity.Property(c => c.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<SaasIdentityMfaMethod>()
            .WithMany()
            .HasForeignKey(c => c.MfaMethodId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(c => new { c.MfaMethodId, c.CodeHash })
            .HasDatabaseName("ix_saas_identity_recovery_codes_method_hash")
            .IsUnique();
    }
}

internal sealed class SaasIdentitySecurityAuditEventEntityConfiguration
    : IEntityTypeConfiguration<SaasIdentitySecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SaasIdentitySecurityAuditEvent> entity)
    {
        entity.ToTable("saas_identity_security_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.AccountId).HasColumnName("account_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<SaasIdentityAccount>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => e.OccurredAtUtc)
            .HasDatabaseName("ix_saas_identity_security_audit_events_occurred_at_utc");
        entity.HasIndex(e => new { e.AccountId, e.OccurredAtUtc })
            .HasDatabaseName("ix_saas_identity_security_audit_events_account_occurred");
        entity.HasIndex(e => new { e.TenantId, e.OccurredAtUtc })
            .HasDatabaseName("ix_saas_identity_security_audit_events_tenant_occurred");
    }
}
