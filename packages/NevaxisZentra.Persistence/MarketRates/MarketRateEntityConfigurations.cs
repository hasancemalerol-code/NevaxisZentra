using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.MarketRates;

public sealed class MarketRateSnapshotEntityConfiguration : IEntityTypeConfiguration<MarketRateSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketRateSnapshot> builder)
    {
        builder.ToTable("market_rate_snapshots");
        builder.HasKey(s => s.TenantId);
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.ProviderKey).HasColumnName("provider_key").HasMaxLength(40).IsRequired();
        builder.Property(s => s.SourceLabel).HasColumnName("source_label").HasMaxLength(120).IsRequired();
        builder.Property(s => s.SourceAttributionUrl).HasColumnName("source_attribution_url").HasMaxLength(240);
        builder.Property(s => s.ProviderObservedAtUtc).HasColumnName("provider_observed_at_utc");
        builder.Property(s => s.SynchronizedAtUtc).HasColumnName("synchronized_at_utc");
        builder.Property(s => s.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        builder.Property(s => s.AvailabilityState).HasColumnName("availability_state").HasMaxLength(20).IsRequired();
        builder.Property(s => s.ConfigurationMessage).HasColumnName("configuration_message").HasMaxLength(400);
        builder.Property(s => s.LastFetchFailureCode).HasColumnName("last_fetch_failure_code").HasMaxLength(60);
        builder.Property(s => s.SourceFingerprint).HasColumnName("source_fingerprint").HasMaxLength(200).IsRequired();
        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne<Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.QuoteLines)
            .WithOne(q => q.Snapshot)
            .HasForeignKey(q => q.TenantId)
            .HasPrincipalKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MarketRateQuoteLineEntityConfiguration : IEntityTypeConfiguration<MarketRateQuoteLine>
{
    public void Configure(EntityTypeBuilder<MarketRateQuoteLine> builder)
    {
        builder.ToTable("market_rate_quote_lines");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("id");
        builder.Property(q => q.TenantId).HasColumnName("tenant_id");
        builder.Property(q => q.BaseCurrency).HasColumnName("base_currency").HasMaxLength(3).IsRequired();
        builder.Property(q => q.QuoteCurrency).HasColumnName("quote_currency").HasMaxLength(3).IsRequired();
        builder.Property(q => q.PairLabel).HasColumnName("pair_label").HasMaxLength(12).IsRequired();
        builder.Property(q => q.Rate).HasColumnName("rate").HasPrecision(18, 6);
        builder.Property(q => q.ChangePercent).HasColumnName("change_percent").HasPrecision(9, 4);

        builder.HasIndex(q => new { q.TenantId, q.PairLabel })
            .HasDatabaseName("ux_market_rate_quote_lines_tenant_pair")
            .IsUnique();
    }
}

public sealed class MarketRateAuditEventEntityConfiguration : IEntityTypeConfiguration<MarketRateAuditEvent>
{
    public void Configure(EntityTypeBuilder<MarketRateAuditEvent> builder)
    {
        builder.ToTable("market_rate_audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(60).IsRequired();
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Property(e => e.ActorTenantMembershipId).HasColumnName("actor_tenant_membership_id").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ActorLabel).HasColumnName("actor_label").HasMaxLength(200).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();

        builder.HasOne<Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.OccurredAtUtc })
            .HasDatabaseName("ix_market_rate_audit_tenant_occurred");
    }
}
