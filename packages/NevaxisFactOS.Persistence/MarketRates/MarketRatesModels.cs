namespace NevaxisFactOS.Persistence.MarketRates;

public sealed record MarketRatesOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record MarketRateQuoteReadModel(
    string PairLabel,
    string BaseCurrency,
    string QuoteCurrency,
    decimal? Rate,
    decimal? ChangePercent,
    bool ChangePercentAvailable);

public sealed record MarketRateSummaryReadModel(
    string ProviderKey,
    string SourceLabel,
    string? SourceAttributionUrl,
    DateTimeOffset? ProviderObservedAtUtc,
    DateTimeOffset? SynchronizedAtUtc,
    string FreshnessState,
    string FreshnessLabel,
    string AvailabilityState,
    string AvailabilityLabel,
    string? ConfigurationMessage,
    IReadOnlyList<string> MissingConfigurationKeys,
    string? LastFetchFailureCode,
    IReadOnlyList<MarketRateQuoteReadModel> Quotes,
    string Disclaimer);

public sealed record MarketRateRefreshResult(
    bool Success,
    string? FailureCode,
    MarketRateSummaryReadModel Summary);
