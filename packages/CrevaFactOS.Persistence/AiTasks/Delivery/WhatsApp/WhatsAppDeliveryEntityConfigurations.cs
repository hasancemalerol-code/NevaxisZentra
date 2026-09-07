using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.AiTasks.Delivery.WhatsApp;

public sealed class AiTaskWhatsAppChannelConfigurationEntityConfiguration
    : IEntityTypeConfiguration<AiTaskWhatsAppChannelConfiguration>
{
    public void Configure(EntityTypeBuilder<AiTaskWhatsAppChannelConfiguration> builder)
    {
        builder.ToTable("ai_task_whatsapp_channel_configurations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Enabled).HasColumnName("enabled");
        builder.Property(c => c.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(c => c.PhoneNumberId).HasColumnName("phone_number_id").HasMaxLength(100);
        builder.Property(c => c.BusinessAccountId).HasColumnName("business_account_id").HasMaxLength(100);
        builder.Property(c => c.SenderDisplayName).HasColumnName("sender_display_name").HasMaxLength(120);
        builder.Property(c => c.DefaultLocale).HasColumnName("default_locale").HasMaxLength(10).IsRequired();
        builder.Property(c => c.TemplateName).HasColumnName("template_name").HasMaxLength(120);
        builder.Property(c => c.TemplateLanguageCode).HasColumnName("template_language_code").HasMaxLength(10);
        builder.Property(c => c.AccessTokenProtected).HasColumnName("access_token_protected");
        builder.Property(c => c.WebhookVerifyTokenProtected).HasColumnName("webhook_verify_token_protected");
        builder.Property(c => c.ReadinessStatus).HasColumnName("readiness_status").HasMaxLength(30).IsRequired();
        builder.Property(c => c.ReadinessErrorCode).HasColumnName("readiness_error_code").HasMaxLength(100);
        builder.Property(c => c.LastValidatedAtUtc).HasColumnName("last_validated_at_utc");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(c => c.TenantId).IsUnique().HasDatabaseName("ux_ai_task_whatsapp_channel_configurations_tenant");
    }
}

public sealed class AiTaskDeliveryProviderEventEntityConfiguration
    : IEntityTypeConfiguration<AiTaskDeliveryProviderEvent>
{
    public void Configure(EntityTypeBuilder<AiTaskDeliveryProviderEvent> builder)
    {
        builder.ToTable("ai_task_delivery_provider_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.DeliveryId).HasColumnName("delivery_id");
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ProviderStatus).HasColumnName("provider_status").HasMaxLength(30).IsRequired();
        builder.Property(e => e.EventHash).HasColumnName("event_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc");
        builder.Property(e => e.SanitizedPayloadJson).HasColumnName("sanitized_payload_json");

        builder.HasIndex(e => new { e.TenantId, e.EventHash })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_delivery_provider_events_tenant_hash");
        builder.HasIndex(e => new { e.TenantId, e.ProviderMessageId })
            .HasDatabaseName("ix_ai_task_delivery_provider_events_tenant_message");
    }
}
