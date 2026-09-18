namespace NevaxisZentra.Persistence.Export;

public interface IExportBrokerWriteRepository
{
    Task<ExportBrokerWorkspaceWriteResult> CreateWorkspaceAsync(
        ExportOperationContext context,
        CreateExportBrokerWorkspaceCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportBrokerPackageWriteResult> PreparePackageAsync(
        ExportOperationContext context,
        PrepareExportBrokerPackageCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportBrokerPackageWriteResult> SendPackageAsync(
        ExportOperationContext context,
        SendExportBrokerPackageCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportBrokerWorkspaceWriteResult> AcknowledgeAsync(
        ExportOperationContext context,
        AcknowledgeExportBrokerCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportBrokerCorrectionWriteResult> CreateCorrectionAsync(
        ExportOperationContext context,
        CreateExportBrokerCorrectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ExportBrokerCorrectionWriteResult> ResolveCorrectionAsync(
        ExportOperationContext context,
        ResolveExportBrokerCorrectionCommand command,
        CancellationToken cancellationToken = default);
}
