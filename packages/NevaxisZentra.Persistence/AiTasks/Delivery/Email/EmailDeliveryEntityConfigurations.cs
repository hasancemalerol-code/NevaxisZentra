using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.Email;

public sealed class AiTaskEmailChannelConfigurationEntityConfiguration
    : IEntityTypeConfiguration<AiTaskEmailChannelConfiguration>
{
    public void Configure(EntityTypeBuilder<AiTaskEmailChannelConfiguration> builder)
    {
        builder.ToTable("ai_task_email_channel_configurations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Enabled).HasColumnName("enabled");
        builder.Property(c => c.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(c => c.SmtpHost).HasColumnName("smtp_host").HasMaxLength(255);
        builder.Property(c => c.SmtpPort).HasColumnName("smtp_port");
        builder.Property(c => c.SecurityMode).HasColumnName("security_mode").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Username).HasColumnName("username").HasMaxLength(255);
        builder.Property(c => c.PasswordProtected).HasColumnName("password_protected");
        builder.Property(c => c.FromAddress).HasColumnName("from_address").HasMaxLength(320);
        builder.Property(c => c.FromDisplayName).HasColumnName("from_display_name").HasMaxLength(120);
        builder.Property(c => c.ReplyToAddress).HasColumnName("reply_to_address").HasMaxLength(320);
        builder.Property(c => c.DefaultLocale).HasColumnName("default_locale").HasMaxLength(10).IsRequired();
        builder.Property(c => c.TemplateSubject).HasColumnName("template_subject").HasMaxLength(200);
        builder.Property(c => c.ReadinessStatus).HasColumnName("readiness_status").HasMaxLength(30).IsRequired();
        builder.Property(c => c.ReadinessErrorCode).HasColumnName("readiness_error_code").HasMaxLength(100);
        builder.Property(c => c.LastValidatedAtUtc).HasColumnName("last_validated_at_utc");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(c => c.TenantId).IsUnique().HasDatabaseName("ux_ai_task_email_channel_configurations_tenant");
    }
}
