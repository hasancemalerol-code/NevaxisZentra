using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Integration.FactoryEventSpine;

internal sealed class IntegrationInboundFactoryEventEntityConfiguration : IEntityTypeConfiguration<IntegrationInboundFactoryEvent>
{
    public void Configure(EntityTypeBuilder<IntegrationInboundFactoryEvent> entity)
    {
        entity.ToTable("integration_inbound_factory_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.SourceEventId).HasColumnName("source_event_id").HasMaxLength(200);
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(240);
        entity.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128);
        entity.Property(e => e.RawPayloadJson).HasColumnName("raw_payload_json");
        entity.Property(e => e.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(40);
        entity.Property(e => e.ValidationState).HasColumnName("validation_state").HasMaxLength(40);
        entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
        entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
        entity.Property(e => e.LastError).HasColumnName("last_error");
        entity.Property(e => e.ErrorClassification).HasColumnName("error_classification").HasMaxLength(40);
        entity.Property(e => e.NextRetryAtUtc).HasColumnName("next_retry_at_utc");
        entity.Property(e => e.NormalizedEventId).HasColumnName("normalized_event_id");
        entity.Property(e => e.QuarantineId).HasColumnName("quarantine_id");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique().HasDatabaseName("ux_integration_inbound_factory_events_idempotency");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProcessingStatus, e.ReceivedAtUtc }).HasDatabaseName("ix_integration_inbound_factory_events_processing");
    }
}

internal sealed class IntegrationCanonicalFactoryEventEntityConfiguration : IEntityTypeConfiguration<IntegrationCanonicalFactoryEvent>
{
    public void Configure(EntityTypeBuilder<IntegrationCanonicalFactoryEvent> entity)
    {
        entity.ToTable("integration_canonical_factory_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectorId).HasColumnName("connector_id");
        entity.Property(e => e.InboundEventId).HasColumnName("inbound_event_id");
        entity.Property(e => e.SchemaVersion).HasColumnName("schema_version").HasMaxLength(40);
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(120);
        entity.Property(e => e.EventVersion).HasColumnName("event_version").HasMaxLength(20);
        entity.Property(e => e.SourceSystem).HasColumnName("source_system").HasMaxLength(120);
        entity.Property(e => e.SourceEventId).HasColumnName("source_event_id").HasMaxLength(200);
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(240);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(e => e.MachineId).HasColumnName("machine_id").HasMaxLength(80);
        entity.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(80);
        entity.Property(e => e.MaterialId).HasColumnName("material_id").HasMaxLength(80);
        entity.Property(e => e.MoldId).HasColumnName("mold_id").HasMaxLength(80);
        entity.Property(e => e.ProductionLineId).HasColumnName("production_line_id").HasMaxLength(80);
        entity.Property(e => e.ShiftId).HasColumnName("shift_id").HasMaxLength(80);
        entity.Property(e => e.MeasurementsJson).HasColumnName("measurements_json");
        entity.Property(e => e.BusinessAttributesJson).HasColumnName("business_attributes_json");
        entity.Property(e => e.NormalizedPayloadJson).HasColumnName("normalized_payload_json");
        entity.Property(e => e.CorrelationKeysJson).HasColumnName("correlation_keys_json");
        entity.Property(e => e.ProcessingState).HasColumnName("processing_state").HasMaxLength(40);
        entity.Property(e => e.ValidationState).HasColumnName("validation_state").HasMaxLength(40);
        entity.Property(e => e.IsLateEvent).HasColumnName("is_late_event");
        entity.Property(e => e.SourceSequence).HasColumnName("source_sequence");
        entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(240);
        entity.Property(e => e.Description).HasColumnName("description");
        entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
        entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
        entity.Property(e => e.LastError).HasColumnName("last_error");
        entity.Property(e => e.ErrorClassification).HasColumnName("error_classification").HasMaxLength(40);
        entity.Property(e => e.NextRetryAtUtc).HasColumnName("next_retry_at_utc");
        entity.Property(e => e.RoutedCaseId).HasColumnName("routed_case_id");
        entity.Property(e => e.RoutedCorrelationKey).HasColumnName("routed_correlation_key").HasMaxLength(240);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique().HasDatabaseName("ux_integration_canonical_factory_events_idempotency");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProcessingState, e.ReceivedAtUtc }).HasDatabaseName("ix_integration_canonical_factory_events_dispatch");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MachineId, e.SourceSequence }).HasDatabaseName("ix_integration_canonical_factory_events_ordering");
    }
}

internal sealed class IntegrationEventConsumerReceiptEntityConfiguration : IEntityTypeConfiguration<IntegrationEventConsumerReceipt>
{
    public void Configure(EntityTypeBuilder<IntegrationEventConsumerReceipt> entity)
    {
        entity.ToTable("integration_event_consumer_receipts");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.CanonicalEventId).HasColumnName("canonical_event_id");
        entity.Property(e => e.ConsumerName).HasColumnName("consumer_name").HasMaxLength(80);
        entity.Property(e => e.ProcessingKey).HasColumnName("processing_key").HasMaxLength(240);
        entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(40);
        entity.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.ProcessingKey }).IsUnique().HasDatabaseName("ux_integration_event_consumer_receipts_processing_key");
    }
}

internal sealed class IntegrationEventDispatchOutboxEntityConfiguration : IEntityTypeConfiguration<IntegrationEventDispatchOutbox>
{
    public void Configure(EntityTypeBuilder<IntegrationEventDispatchOutbox> entity)
    {
        entity.ToTable("integration_event_dispatch_outbox");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.CanonicalEventId).HasColumnName("canonical_event_id");
        entity.Property(e => e.ConsumerName).HasColumnName("consumer_name").HasMaxLength(80);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
        entity.Property(e => e.LastError).HasColumnName("last_error");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.DispatchedAtUtc).HasColumnName("dispatched_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAtUtc }).HasDatabaseName("ix_integration_event_dispatch_outbox_status");
    }
}
