namespace NevaxisZentra.Persistence.Production;

public interface IProductionReportingRepository
{
    Task<ProductionReportingReadResult<ProductionOperationalOverviewReadModel>> GetOverviewAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<IReadOnlyList<ProductionOrderPerformanceReadModel>>> ListOrderPerformanceAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<ProductionDowntimeAnalyticsReadModel>> GetDowntimeAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<ProductionScrapAnalyticsReadModel>> GetScrapAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<ProductionSetupAnalyticsReadModel>> GetSetupAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<ProductionCycleAnalyticsReadModel>> GetCycleAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<IReadOnlyList<ProductionShiftReportingReadModel>>> ListShiftAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductionReportingReadResult<ProductionExceptionAnalyticsReadModel>> GetExceptionAnalyticsAsync(
        ProductionOrderOperationContext context,
        ProductionReportingFilter filter,
        CancellationToken cancellationToken = default);
}
