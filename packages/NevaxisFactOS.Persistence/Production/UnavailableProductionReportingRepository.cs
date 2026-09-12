namespace NevaxisFactOS.Persistence.Production;

public sealed class UnavailableProductionReportingRepository : IProductionReportingRepository
{
    private static Task<ProductionReportingReadResult<T>> Unavailable<T>() =>
        Task.FromResult(ProductionReportingResults.Fail<T>(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production reporting is unavailable."));

    public Task<ProductionReportingReadResult<ProductionOperationalOverviewReadModel>> GetOverviewAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<ProductionOperationalOverviewReadModel>();

    public Task<ProductionReportingReadResult<IReadOnlyList<ProductionOrderPerformanceReadModel>>> ListOrderPerformanceAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<IReadOnlyList<ProductionOrderPerformanceReadModel>>();

    public Task<ProductionReportingReadResult<ProductionDowntimeAnalyticsReadModel>> GetDowntimeAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<ProductionDowntimeAnalyticsReadModel>();

    public Task<ProductionReportingReadResult<ProductionScrapAnalyticsReadModel>> GetScrapAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<ProductionScrapAnalyticsReadModel>();

    public Task<ProductionReportingReadResult<ProductionSetupAnalyticsReadModel>> GetSetupAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<ProductionSetupAnalyticsReadModel>();

    public Task<ProductionReportingReadResult<ProductionCycleAnalyticsReadModel>> GetCycleAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<ProductionCycleAnalyticsReadModel>();

    public Task<ProductionReportingReadResult<IReadOnlyList<ProductionShiftReportingReadModel>>> ListShiftAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<IReadOnlyList<ProductionShiftReportingReadModel>>();

    public Task<ProductionReportingReadResult<ProductionExceptionAnalyticsReadModel>> GetExceptionAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default) => Unavailable<ProductionExceptionAnalyticsReadModel>();
}
