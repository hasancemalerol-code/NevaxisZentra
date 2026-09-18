namespace NevaxisZentra.Persistence.Export;

public interface IExportBrokerReadRepository
{
    Task<ExportBrokerWorkspaceResult> GetWorkspaceAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default);
}
