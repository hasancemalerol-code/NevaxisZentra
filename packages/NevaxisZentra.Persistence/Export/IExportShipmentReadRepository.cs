namespace NevaxisZentra.Persistence.Export;

public interface IExportShipmentReadRepository
{
    Task<ExportShipmentListResult> ListAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportShipmentDetailResult> GetAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    Task<ExportShipmentReadinessResult> GetReadinessAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        Guid shipmentId,
        CancellationToken cancellationToken = default);
}
