namespace NevaxisZentra.Persistence.MarketRates;

public sealed class MarketRateSnapshot
{
    public Guid TenantId { get; set; }

    public string ProviderKey { get; set; } = MarketRatesProviderKeys.NotConfigured;

    public string SourceLabel { get; set; } = string.Empty;

    public string? SourceAttributionUrl { get; set; }

    public DateTimeOffset? ProviderObservedAtUtc { get; set; }

    public DateTimeOffset? SynchronizedAtUtc { get; set; }

    public string FreshnessState { get; set; } = MarketRatesFreshnessStates.Unknown;

    public string AvailabilityState { get; set; } = MarketRatesAvailabilityStates.NotConfigured;

    public string? ConfigurationMessage { get; set; }

    public string? LastFetchFailureCode { get; set; }

    public string SourceFingerprint { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<MarketRateQuoteLine> QuoteLines { get; set; } = [];
}

public sealed class MarketRateQuoteLine
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string BaseCurrency { get; set; } = string.Empty;

    public string QuoteCurrency { get; set; } = string.Empty;

    public string PairLabel { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public decimal? ChangePercent { get; set; }

    public MarketRateSnapshot Snapshot { get; set; } = null!;
}

public sealed class MarketRateAuditEvent
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string ActorTenantMembershipId { get; set; } = string.Empty;

    public string ActorLabel { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";
}
