using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportBrokerReadRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportBrokerReadRepository
{
    public async Task<ExportBrokerWorkspaceResult> GetWorkspaceAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var workspace = await dbContext.ExportBrokerWorkspaces
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            w => w.TenantId == context.TenantId
                                && w.FacilityId == context.FacilityId
                                && w.ExportCaseId == exportCaseId
                                && w.Status != ExportBrokerWorkspaceStatus.Cancelled
                                && w.Status != ExportBrokerWorkspaceStatus.Closed,
                            ct);

                    if (workspace is null)
                    {
                        return ExportBrokerWorkspaceResult.Missing();
                    }

                    var packageVersions = await dbContext.ExportBrokerPackageVersions
                        .AsNoTracking()
                        .Where(v => v.TenantId == context.TenantId && v.BrokerWorkspaceId == workspace.Id)
                        .OrderBy(v => v.VersionNumber)
                        .ToListAsync(ct);

                    var allVersionIds = packageVersions.Select(v => v.Id).ToList();

                    var allDocuments = allVersionIds.Count > 0
                        ? await dbContext.ExportBrokerPackageDocuments
                            .AsNoTracking()
                            .Where(d => allVersionIds.Contains(d.PackageVersionId))
                            .ToListAsync(ct)
                        : new List<ExportBrokerPackageDocument>();

                    var corrections = await dbContext.ExportBrokerCorrections
                        .AsNoTracking()
                        .Where(c => c.TenantId == context.TenantId && c.BrokerWorkspaceId == workspace.Id)
                        .OrderBy(c => c.CreatedAtUtc)
                        .ToListAsync(ct);

                    var versionModels = packageVersions.Select(v =>
                    {
                        var docs = allDocuments
                            .Where(d => d.PackageVersionId == v.Id)
                            .Select(d => new ExportBrokerPackageDocumentReadModel(
                                d.Id,
                                d.DocumentId,
                                d.DocumentType,
                                d.DocumentStatusSnapshot,
                                d.DisplayNameSnapshot,
                                d.Included))
                            .ToList();

                        return new ExportBrokerPackageVersionReadModel(
                            v.Id,
                            v.VersionNumber,
                            ExportValueConversions.ToStorage(v.Status),
                            v.Notes,
                            v.CreatedAtUtc,
                            v.SentAtUtc,
                            v.SentBySubjectId,
                            docs);
                    }).ToList();

                    var correctionModels = corrections.Select(c => new ExportBrokerCorrectionReadModel(
                        c.Id,
                        c.BrokerWorkspaceId,
                        c.Category,
                        c.Title,
                        c.Description,
                        ExportValueConversions.ToStorage(c.Severity),
                        ExportValueConversions.ToStorage(c.Status),
                        c.RelatedDocumentId,
                        c.CreatedAtUtc,
                        c.ResolvedAtUtc,
                        c.ResolvedBySubjectId,
                        c.ResolutionNote)).ToList();

                    var model = new ExportBrokerWorkspaceReadModel(
                        workspace.Id,
                        workspace.ExportCaseId,
                        workspace.ShipmentId,
                        workspace.BrokerCompanyName,
                        workspace.BrokerContactName,
                        workspace.BrokerEmail,
                        workspace.BrokerPhone,
                        ExportValueConversions.ToStorage(workspace.Status),
                        workspace.PreparedAtUtc,
                        workspace.PreparedBySubjectId,
                        workspace.SentAtUtc,
                        workspace.SentBySubjectId,
                        workspace.AcknowledgedAtUtc,
                        workspace.CompletedAtUtc,
                        workspace.BrokerReference,
                        workspace.Notes,
                        workspace.Version,
                        workspace.CreatedAtUtc,
                        workspace.UpdatedAtUtc,
                        versionModels,
                        correctionModels);

                    return ExportBrokerWorkspaceResult.Success(model);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerWorkspaceResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }
}

public sealed class UnavailableExportBrokerReadRepository : IExportBrokerReadRepository
{
    private static readonly ExportBrokerWorkspaceResult Unavailable =
        ExportBrokerWorkspaceResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker persistence is disabled.");

    public Task<ExportBrokerWorkspaceResult> GetWorkspaceAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable);
}
