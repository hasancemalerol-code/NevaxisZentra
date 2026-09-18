using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportTraceabilityReadRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportTraceabilityReadRepository
{
    public async Task<ExportTraceabilityResult> GetTraceabilityAsync(
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
                    var exportCase = await dbContext.ExportCases
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            c => c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    if (exportCase is null)
                    {
                        return ExportTraceabilityResult.Missing();
                    }

                    var documents = await dbContext.ExportDocuments
                        .AsNoTracking()
                        .Where(d =>
                            d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.ExportCaseId == exportCaseId)
                        .OrderBy(d => d.DocumentType)
                        .ThenBy(d => d.CreatedAtUtc)
                        .ToListAsync(ct);

                    var packingUnits = await dbContext.ExportPackingUnits
                        .AsNoTracking()
                        .Where(u =>
                            u.TenantId == context.TenantId
                            && u.FacilityId == context.FacilityId
                            && u.ExportCaseId == exportCaseId)
                        .OrderBy(u => u.UnitCode)
                        .ToListAsync(ct);

                    var shipments = await dbContext.ExportShipments
                        .AsNoTracking()
                        .Where(s =>
                            s.TenantId == context.TenantId
                            && s.FacilityId == context.FacilityId
                            && s.ExportCaseId == exportCaseId)
                        .OrderByDescending(s => s.UpdatedAtUtc)
                        .ToListAsync(ct);

                    var workspace = await dbContext.ExportBrokerWorkspaces
                        .AsNoTracking()
                        .Where(w =>
                            w.TenantId == context.TenantId
                            && w.FacilityId == context.FacilityId
                            && w.ExportCaseId == exportCaseId
                            && w.Status != ExportBrokerWorkspaceStatus.Cancelled
                            && w.Status != ExportBrokerWorkspaceStatus.Closed)
                        .OrderByDescending(w => w.UpdatedAtUtc)
                        .FirstOrDefaultAsync(ct);

                    ExportTraceabilityBrokerRef? brokerRef = null;
                    if (workspace is not null)
                    {
                        var latestPackageVersion = await dbContext.ExportBrokerPackageVersions
                            .AsNoTracking()
                            .Where(v =>
                                v.TenantId == context.TenantId
                                && v.BrokerWorkspaceId == workspace.Id)
                            .OrderByDescending(v => v.VersionNumber)
                            .FirstOrDefaultAsync(ct);

                        brokerRef = new ExportTraceabilityBrokerRef(
                            workspace.Id,
                            latestPackageVersion?.VersionNumber,
                            ExportValueConversions.ToStorage(workspace.Status));
                    }

                    ExportTraceabilityComplianceRef? complianceRef = null;
                    if (exportCase.CurrentComplianceEvaluationId.HasValue)
                    {
                        var evaluation = await dbContext.ExportComplianceEvaluations
                            .AsNoTracking()
                            .SingleOrDefaultAsync(
                                e => e.TenantId == context.TenantId
                                    && e.FacilityId == context.FacilityId
                                    && e.Id == exportCase.CurrentComplianceEvaluationId.Value,
                                ct);

                        if (evaluation is not null)
                        {
                            complianceRef = new ExportTraceabilityComplianceRef(
                                evaluation.Id,
                                evaluation.EvaluationNumber,
                                ExportValueConversions.ToStorage(evaluation.Result),
                                evaluation.RulePackVersionNumber);
                        }
                    }

                    ExportTraceabilityResearchRef? researchRef = null;
                    if (exportCase.LinkedResearchCaseId.HasValue)
                    {
                        var researchCase = await dbContext.ExportCountryResearchCases
                            .AsNoTracking()
                            .SingleOrDefaultAsync(
                                r => r.TenantId == context.TenantId
                                    && r.FacilityId == context.FacilityId
                                    && r.Id == exportCase.LinkedResearchCaseId.Value,
                                ct);

                        if (researchCase is not null)
                        {
                            researchRef = new ExportTraceabilityResearchRef(
                                researchCase.Id,
                                researchCase.ResearchNumber,
                                ExportValueConversions.ToStorage(researchCase.Status));
                        }
                    }

                    var activityCount = await dbContext.ExportCaseActivities
                        .AsNoTracking()
                        .CountAsync(
                            a => a.TenantId == context.TenantId
                                && a.FacilityId == context.FacilityId
                                && a.ExportCaseId == exportCaseId,
                            ct);

                    var model = new ExportTraceabilityReadModel(
                        exportCase.Id,
                        exportCase.CaseNumber,
                        exportCase.SourceOrderId,
                        exportCase.SourceOrderReference,
                        exportCase.CustomerName,
                        exportCase.DestinationCountryCode,
                        documents.Select(d => new ExportTraceabilityDocumentRef(
                            d.Id,
                            d.DocumentType,
                            ExportValueConversions.ToStorage(d.DocumentStatus),
                            d.Version)).ToList(),
                        packingUnits.Select(u => new ExportTraceabilityPackingRef(
                            u.Id,
                            u.UnitCode,
                            ExportValueConversions.ToStorage(u.Status))).ToList(),
                        shipments.Select(s => new ExportTraceabilityShipmentRef(
                            s.Id,
                            s.ShipmentReference,
                            ExportValueConversions.ToStorage(s.Status),
                            s.Version)).ToList(),
                        brokerRef,
                        complianceRef,
                        researchRef,
                        activityCount);

                    return ExportTraceabilityResult.Success(model);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportTraceabilityResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export traceability service is unavailable.");
        }
    }
}

public sealed class UnavailableExportTraceabilityReadRepository : IExportTraceabilityReadRepository
{
    private static readonly ExportTraceabilityResult Unavailable =
        ExportTraceabilityResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export traceability persistence is disabled.");

    public Task<ExportTraceabilityResult> GetTraceabilityAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable);
}
