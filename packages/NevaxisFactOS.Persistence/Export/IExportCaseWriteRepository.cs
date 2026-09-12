namespace NevaxisFactOS.Persistence.Export;

public interface IExportCaseWriteRepository
{
    Task<ExportCaseWriteResult> EnsureExportCaseForOrderAsync(
        ExportOperationContext context,
        EnsureExportCaseForOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportCaseWriteResult> UpdateExportCaseAsync(
        ExportOperationContext context,
        UpdateExportCaseCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportDocumentWriteResult> RegisterDocumentAsync(
        ExportOperationContext context,
        RegisterExportDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportDocumentWriteResult> UpdateDocumentAsync(
        ExportOperationContext context,
        UpdateExportDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportDocumentWriteResult> VerifyDocumentAsync(
        ExportOperationContext context,
        VerifyExportDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportDocumentWriteResult> RejectDocumentAsync(
        ExportOperationContext context,
        RejectExportDocumentCommand command,
        CancellationToken cancellationToken = default);
}
