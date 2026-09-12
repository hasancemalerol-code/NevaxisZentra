namespace NevaxisFactOS.Persistence.Export;

public interface IExportCaseReadRepository
{
    Task<ExportCaseListResult> ListAsync(
        ExportOperationContext context,
        string? statusFilter,
        string? readinessFilter,
        string? searchFilter,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<ExportCaseDetailResult> FindByIdAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportCaseDetailResult> FindBySourceOrderIdAsync(
        ExportOperationContext context,
        Guid sourceOrderId,
        CancellationToken cancellationToken = default);

    Task<ExportReadinessResult> GetReadinessAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportDocumentListResult> ListDocumentsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);

    Task<ExportActivityListResult> ListActivityAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        int? limit,
        CancellationToken cancellationToken = default);
}
