namespace NevaxisZentra.Persistence.Export;

public interface IExportPackingWriteRepository
{
    Task<ExportPackingUnitWriteResult> CreateUnitAsync(
        ExportOperationContext context,
        CreateExportPackingUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportPackingUnitWriteResult> UpdateUnitAsync(
        ExportOperationContext context,
        UpdateExportPackingUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportPackingItemWriteResult> AddItemAsync(
        ExportOperationContext context,
        AddExportPackingItemCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportPackingStatusWriteResult> CompletePackingAsync(
        ExportOperationContext context,
        CompleteExportPackingCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportPackingStatusWriteResult> VerifyPackingAsync(
        ExportOperationContext context,
        VerifyExportPackingCommand command,
        CancellationToken cancellationToken = default);
}
