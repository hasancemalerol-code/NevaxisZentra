using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.MarketRates;

public sealed record MarketRateSynchronizeResult(bool Success, string? FailureCode);

public sealed class MarketRateSourceSynchronizer(
    NevaxisZentraDbContext dbContext,
    IExchangeRateProvider exchangeRateProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<MarketRateSynchronizeResult> SynchronizeAsync(
        MarketRatesOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var fetch = await exchangeRateProvider.FetchLatestAsync(cancellationToken);

        if (!fetch.Success)
        {
            await ApplyFailedFetchAsync(context, fetch, now, cancellationToken);
            return new MarketRateSynchronizeResult(false, fetch.FailureCode);
        }

        var snapshot = await dbContext.MarketRateSnapshots
            .FirstOrDefaultAsync(s => s.TenantId == context.TenantId, cancellationToken);

        if (snapshot is null)
        {
            snapshot = new MarketRateSnapshot
            {
                TenantId = context.TenantId,
                CreatedAtUtc = now,
            };
            dbContext.MarketRateSnapshots.Add(snapshot);
        }

        snapshot.ProviderKey = fetch.ProviderKey;
        snapshot.SourceLabel = fetch.SourceLabel;
        snapshot.SourceAttributionUrl = fetch.SourceAttributionUrl;
        snapshot.ProviderObservedAtUtc = fetch.ProviderObservedAtUtc;
        snapshot.SynchronizedAtUtc = now;
        snapshot.FreshnessState = fetch.FreshnessState;
        snapshot.AvailabilityState = fetch.AvailabilityState;
        snapshot.ConfigurationMessage = fetch.ConfigurationMessage;
        snapshot.LastFetchFailureCode = null;
        snapshot.SourceFingerprint = fetch.SourceFingerprint;
        snapshot.UpdatedAtUtc = now;

        await dbContext.MarketRateQuoteLines
            .Where(q => q.TenantId == context.TenantId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var quote in fetch.Quotes)
        {
            dbContext.MarketRateQuoteLines.Add(new MarketRateQuoteLine
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                BaseCurrency = quote.BaseCurrency,
                QuoteCurrency = quote.QuoteCurrency,
                PairLabel = quote.PairLabel,
                Rate = quote.Rate,
                ChangePercent = quote.ChangePercent,
            });
        }

        dbContext.MarketRateAuditEvents.Add(BuildAuditEvent(
            context,
            MarketRatesAuditEventTypes.SnapshotRefreshed,
            new
            {
                providerKey = fetch.ProviderKey,
                sourceLabel = fetch.SourceLabel,
                providerObservedAtUtc = fetch.ProviderObservedAtUtc,
                quoteCount = fetch.Quotes.Count,
            },
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        return new MarketRateSynchronizeResult(true, null);
    }

    private async Task ApplyFailedFetchAsync(
        MarketRatesOperationContext context,
        ExchangeRateFetchResult fetch,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.MarketRateSnapshots
            .FirstOrDefaultAsync(s => s.TenantId == context.TenantId, cancellationToken);

        if (snapshot is null && fetch.AvailabilityState == MarketRatesAvailabilityStates.NotConfigured)
        {
            return;
        }

        if (snapshot is null)
        {
            snapshot = new MarketRateSnapshot
            {
                TenantId = context.TenantId,
                CreatedAtUtc = now,
            };
            dbContext.MarketRateSnapshots.Add(snapshot);
        }

        snapshot.ProviderKey = fetch.ProviderKey;
        snapshot.SourceLabel = string.IsNullOrWhiteSpace(fetch.SourceLabel) ? fetch.ProviderKey : fetch.SourceLabel;
        snapshot.SourceAttributionUrl = fetch.SourceAttributionUrl;
        if (fetch.ProviderObservedAtUtc is not null)
        {
            snapshot.ProviderObservedAtUtc = fetch.ProviderObservedAtUtc;
        }

        snapshot.SynchronizedAtUtc = snapshot.SynchronizedAtUtc ?? now;
        snapshot.FreshnessState = fetch.ProviderObservedAtUtc is null
            ? snapshot.FreshnessState
            : fetch.FreshnessState;
        snapshot.AvailabilityState = fetch.AvailabilityState;
        snapshot.ConfigurationMessage = fetch.ConfigurationMessage;
        snapshot.LastFetchFailureCode = fetch.FailureCode;
        snapshot.SourceFingerprint = fetch.SourceFingerprint;
        snapshot.UpdatedAtUtc = now;

        dbContext.MarketRateAuditEvents.Add(BuildAuditEvent(
            context,
            MarketRatesAuditEventTypes.SnapshotRefreshFailed,
            new
            {
                providerKey = fetch.ProviderKey,
                failureCode = fetch.FailureCode,
                availabilityState = fetch.AvailabilityState,
            },
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MarketRateAuditEvent BuildAuditEvent(
        MarketRatesOperationContext context,
        string eventType,
        object payload,
        DateTimeOffset occurredAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorTenantMembershipId = context.MembershipId.ToString(),
            ActorLabel = context.MembershipId.ToString(),
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
        };
}
