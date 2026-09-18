namespace NevaxisZentra.Persistence.Export;

public interface IExportPackingReadRepository
{
    Task<ExportPackingResult> GetPackingAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportPackingReconciliationResult> GetReconciliationAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        decimal? orderedQuantityTotal,
        CancellationToken cancellationToken = default);
}
