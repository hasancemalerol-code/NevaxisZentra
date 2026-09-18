namespace NevaxisZentra.Persistence.Export;

public interface IExportShipmentWriteRepository
{
    Task<ExportShipmentWriteResult> CreateAsync(
        ExportOperationContext context,
        CreateExportShipmentCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportShipmentWriteResult> UpdateAsync(
        ExportOperationContext context,
        UpdateExportShipmentCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportShipmentWriteResult> TransitionAsync(
        ExportOperationContext context,
        TransitionExportShipmentCommand command,
        CancellationToken cancellationToken = default);
}
