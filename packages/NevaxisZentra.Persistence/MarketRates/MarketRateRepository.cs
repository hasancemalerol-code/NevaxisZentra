using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.MarketRates;

public interface IMarketRateRepository
{
    Task<MarketRateSummaryReadModel> GetSummaryAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default);

    Task<MarketRateRefreshResult> RefreshAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default);
}

public sealed class MarketRateRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    MarketRateSourceSynchronizer synchronizer,
    IExchangeRateProvider exchangeRateProvider) : IMarketRateRepository
{
    public async Task<MarketRateSummaryReadModel> GetSummaryAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var providerStatus = exchangeRateProvider.GetStatus();
        var refreshScopeKey = MarketRatesAutomaticRefreshPolicy.BuildRefreshScopeKey(context);

        var initialSnapshot = await tenantDatabaseSession.ExecuteReadAsync(
            ct => LoadSnapshotAsync(context.TenantId, ct),
            cancellationToken);

        if (MarketRatesAutomaticRefreshPolicy.ShouldAttemptRefresh(initialSnapshot, providerStatus, DateTimeOffset.UtcNow))
        {
            await MarketRateAutomaticRefreshCoordinator.ExecuteExclusiveAsync(
                refreshScopeKey,
                async ct =>
                {
                    var latestSnapshot = await tenantDatabaseSession.ExecuteReadAsync(
                        innerCt => LoadSnapshotAsync(context.TenantId, innerCt),
                        ct);

                    if (!MarketRatesAutomaticRefreshPolicy.ShouldAttemptRefresh(
                            latestSnapshot,
                            providerStatus,
                            DateTimeOffset.UtcNow))
                    {
                        return;
                    }

                    await tenantDatabaseSession.ExecuteWriteAsync(
                        innerCt => synchronizer.SynchronizeAsync(context, innerCt),
                        ct);
                },
                cancellationToken);
        }

        return await tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var snapshot = await LoadSnapshotAsync(context.TenantId, ct);
            if (snapshot is null)
            {
                return BuildSummaryFromProviderStatus(providerStatus);
            }

            return MapSnapshot(snapshot, DateTimeOffset.UtcNow);
        }, cancellationToken);
    }

    public Task<MarketRateRefreshResult> RefreshAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var result = await synchronizer.SynchronizeAsync(context, ct);
            return new MarketRateRefreshResult(
                result.Success,
                result.FailureCode,
                result.Success
                    ? await GetSummaryAfterWriteAsync(context.TenantId, ct)
                    : await GetSummaryPreservingSnapshotOnFailureAsync(context.TenantId, ct));
        }, cancellationToken);

    private async Task<MarketRateSnapshot?> LoadSnapshotAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await dbContext.MarketRateSnapshots
            .AsNoTracking()
            .Include(s => s.QuoteLines)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    private async Task<MarketRateSummaryReadModel> GetSummaryAfterWriteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.MarketRateSnapshots
            .AsNoTracking()
            .Include(s => s.QuoteLines)
            .FirstAsync(s => s.TenantId == tenantId, cancellationToken);

        return MapSnapshot(snapshot, DateTimeOffset.UtcNow);
    }

    private async Task<MarketRateSummaryReadModel> GetSummaryPreservingSnapshotOnFailureAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.MarketRateSnapshots
            .AsNoTracking()
            .Include(s => s.QuoteLines)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (snapshot is not null)
        {
            return MapSnapshot(snapshot, DateTimeOffset.UtcNow);
        }

        return BuildSummaryFromProviderStatus(exchangeRateProvider.GetStatus());
    }

    internal static MarketRateSummaryReadModel BuildSummaryFromProviderStatus(
        ExchangeRateProviderStatus status)
    {
        var missingKeys = status.MissingConfigurationKeys;
        return new MarketRateSummaryReadModel(
            status.ProviderKey,
            status.IsConfigured ? status.ProviderKey : "—",
            null,
            null,
            null,
            MarketRatesFreshnessStates.Unknown,
            MarketRatesValueConversions.ToDisplayFreshnessState(MarketRatesFreshnessStates.Unknown),
            MarketRatesAvailabilityStates.NotConfigured,
            MarketRatesValueConversions.ToDisplayAvailabilityState(MarketRatesAvailabilityStates.NotConfigured),
            status.ConfigurationMessage,
            missingKeys,
            status.IsConfigured ? null : MarketRatesFailureCodes.ProviderNotConfigured,
            MarketRatesValueConversions.BuildExpectedPairPlaceholders(),
            MarketRatesDisclaimer.ReadOnlyContext);
    }

    internal static MarketRateSummaryReadModel MapSnapshot(
        MarketRateSnapshot snapshot,
        DateTimeOffset nowUtc)
    {
        var freshness = MarketRatesValueConversions.ComputeFreshnessState(
            snapshot.ProviderObservedAtUtc,
            nowUtc,
            snapshot.ProviderKey);

        var quotesByPair = snapshot.QuoteLines.ToDictionary(q => q.PairLabel, StringComparer.Ordinal);
        var quotes = MarketRatesValueConversions.BuildExpectedPairPlaceholders()
            .Select(placeholder =>
            {
                if (!quotesByPair.TryGetValue(placeholder.PairLabel, out var line))
                {
                    return placeholder;
                }

                return new MarketRateQuoteReadModel(
                    line.PairLabel,
                    line.BaseCurrency,
                    line.QuoteCurrency,
                    line.Rate,
                    line.ChangePercent,
                    line.ChangePercent is not null);
            })
            .ToList();

        return new MarketRateSummaryReadModel(
            snapshot.ProviderKey,
            snapshot.SourceLabel,
            snapshot.SourceAttributionUrl,
            snapshot.ProviderObservedAtUtc,
            snapshot.SynchronizedAtUtc,
            freshness,
            MarketRatesValueConversions.ToDisplayFreshnessState(freshness),
            snapshot.AvailabilityState,
            MarketRatesValueConversions.ToDisplayAvailabilityState(snapshot.AvailabilityState),
            snapshot.ConfigurationMessage,
            [],
            snapshot.LastFetchFailureCode,
            quotes,
            MarketRatesDisclaimer.ReadOnlyContext);
    }
}

public sealed class UnavailableMarketRateRepository : IMarketRateRepository
{
    public Task<MarketRateSummaryReadModel> GetSummaryAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MarketRateSummaryReadModel(
            MarketRatesProviderKeys.NotConfigured,
            "—",
            null,
            null,
            null,
            MarketRatesFreshnessStates.Unknown,
            MarketRatesValueConversions.ToDisplayFreshnessState(MarketRatesFreshnessStates.Unknown),
            MarketRatesAvailabilityStates.Unavailable,
            MarketRatesValueConversions.ToDisplayAvailabilityState(MarketRatesAvailabilityStates.Unavailable),
            "Piyasa kuru servisi kullanılamıyor.",
            [],
            MarketRatesFailureCodes.PersistenceUnavailable,
            MarketRatesValueConversions.BuildExpectedPairPlaceholders(),
            MarketRatesDisclaimer.ReadOnlyContext));

    public Task<MarketRateRefreshResult> RefreshAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MarketRateRefreshResult(
            false,
            MarketRatesFailureCodes.PersistenceUnavailable,
            new MarketRateSummaryReadModel(
                MarketRatesProviderKeys.NotConfigured,
                "—",
                null,
                null,
                null,
                MarketRatesFreshnessStates.Unknown,
                MarketRatesValueConversions.ToDisplayFreshnessState(MarketRatesFreshnessStates.Unknown),
                MarketRatesAvailabilityStates.Unavailable,
                MarketRatesValueConversions.ToDisplayAvailabilityState(MarketRatesAvailabilityStates.Unavailable),
                "Piyasa kuru servisi kullanılamıyor.",
                [],
                MarketRatesFailureCodes.PersistenceUnavailable,
                MarketRatesValueConversions.BuildExpectedPairPlaceholders(),
                MarketRatesDisclaimer.ReadOnlyContext)));
}
