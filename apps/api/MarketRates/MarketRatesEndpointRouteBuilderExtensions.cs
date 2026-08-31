using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.MarketRates;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.MarketRates;

public static class MarketRatesEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketRatesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard/market-rates").RequireAuthorization();
        group.MapGet("/", GetSummaryAsync);
        group.MapPost("/refresh", RefreshAsync);
        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMarketRateRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MarketRatesEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var summary = await repository.GetSummaryAsync(context, cancellationToken);
        return TypedResults.Ok(MarketRatesEndpointHelpers.MapSummary(summary));
    }

    private static async Task<IResult> RefreshAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMarketRateRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MarketRatesEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RefreshAsync(context, cancellationToken);
        return MarketRatesEndpointHelpers.MapRefreshResult(result);
    }
}
