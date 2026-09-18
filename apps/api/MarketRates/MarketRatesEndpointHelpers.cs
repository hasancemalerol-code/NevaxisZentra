using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.MarketRates;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.MarketRates;

internal static class MarketRatesEndpointHelpers
{
    internal static async Task<(MarketRatesOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "Tesis tanımlayıcısı gerekli."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, MarketRatesPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static object MapSummary(MarketRateSummaryReadModel summary) =>
        new
        {
            providerKey = summary.ProviderKey,
            sourceLabel = summary.SourceLabel,
            sourceAttributionUrl = summary.SourceAttributionUrl,
            providerObservedAtUtc = summary.ProviderObservedAtUtc,
            synchronizedAtUtc = summary.SynchronizedAtUtc,
            freshnessState = summary.FreshnessState,
            freshnessLabel = summary.FreshnessLabel,
            availabilityState = summary.AvailabilityState,
            availabilityLabel = summary.AvailabilityLabel,
            configurationMessage = summary.ConfigurationMessage,
            missingConfigurationKeys = summary.MissingConfigurationKeys,
            lastFetchFailureCode = summary.LastFetchFailureCode,
            disclaimer = summary.Disclaimer,
            quotes = summary.Quotes.Select(q => new
            {
                pairLabel = q.PairLabel,
                baseCurrency = q.BaseCurrency,
                quoteCurrency = q.QuoteCurrency,
                rate = q.Rate,
                changePercent = q.ChangePercent,
                changePercentAvailable = q.ChangePercentAvailable,
            }),
        };

    internal static IResult MapRefreshResult(MarketRateRefreshResult result) =>
        TypedResults.Ok(new
        {
            success = result.Success,
            failureCode = result.FailureCode,
            summary = MapSummary(result.Summary),
        });

    private static MarketRatesOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string code, string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Erişim reddedildi",
            detail: detail,
            extensions: FailureExt(code));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Kimlik doğrulama gerekli",
                detail: accessResult.InternalReason,
                extensions: FailureExt("dashboard.market-rates.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Yetki yok",
                detail: accessResult.InternalReason,
                extensions: FailureExt("dashboard.market-rates.permission-denied")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tesis gerekli",
                detail: accessResult.InternalReason,
                extensions: FailureExt("dashboard.market-rates.facility-missing")),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Erişim reddedildi",
                detail: accessResult.InternalReason,
                extensions: FailureExt("dashboard.market-rates.facility-access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new(StringComparer.Ordinal) { ["failureCode"] = code };
}
