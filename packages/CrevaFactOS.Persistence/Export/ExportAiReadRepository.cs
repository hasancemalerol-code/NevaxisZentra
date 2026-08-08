using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportAiReadRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportAiReadRepository
{
    public async Task<ExportAiRunListResult> ListRunsForExportCaseAsync(
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
                    var exportCase = await LoadExportCaseAsync(context, exportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportAiRunListResult.Missing();
                    }

                    var runs = await dbContext.ExportAiAnalysisRuns
                        .AsNoTracking()
                        .Where(r =>
                            r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && r.ExportCaseId == exportCaseId)
                        .OrderByDescending(r => r.StartedAtUtc)
                        .ToListAsync(ct);

                    return ExportAiRunListResult.Success(runs.Select(Export4LMapping.MapRun).ToArray());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportAiRunListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export AI service is unavailable.");
        }
    }

    public async Task<ExportAiRunListResult> ListRunsForResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var researchCase = await dbContext.ExportCountryResearchCases
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            r => r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && r.Id == researchCaseId,
                            ct);

                    if (researchCase is null)
                    {
                        return ExportAiRunListResult.Missing();
                    }

                    var runs = await dbContext.ExportAiAnalysisRuns
                        .AsNoTracking()
                        .Where(r =>
                            r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && r.CountryResearchCaseId == researchCaseId)
                        .OrderByDescending(r => r.StartedAtUtc)
                        .ToListAsync(ct);

                    return ExportAiRunListResult.Success(runs.Select(Export4LMapping.MapRun).ToArray());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportAiRunListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export AI service is unavailable.");
        }
    }

    public async Task<ExportAiRunDetailResult> GetRunDetailAsync(
        ExportOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var run = await dbContext.ExportAiAnalysisRuns
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            r => r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && r.Id == runId,
                            ct);

                    if (run is null)
                    {
                        return ExportAiRunDetailResult.Missing();
                    }

                    var findings = await dbContext.ExportAiFindings
                        .AsNoTracking()
                        .Where(f =>
                            f.TenantId == context.TenantId
                            && f.FacilityId == context.FacilityId
                            && f.AnalysisRunId == runId)
                        .OrderBy(f => f.CreatedAtUtc)
                        .ToListAsync(ct);

                    var recommendations = await dbContext.ExportAiRecommendations
                        .AsNoTracking()
                        .Where(r =>
                            r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && r.AnalysisRunId == runId)
                        .OrderBy(r => r.CreatedAtUtc)
                        .ToListAsync(ct);

                    ExportAiContextSnapshotModel? contextSnapshot = null;
                    var snapshot = await dbContext.ExportAiContextSnapshots
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            s => s.TenantId == context.TenantId
                                && s.FacilityId == context.FacilityId
                                && s.Id == run.ContextSnapshotId,
                            ct);

                    if (snapshot is not null)
                    {
                        contextSnapshot = ExportAiContextBuilder.Deserialize(snapshot.ContextJson);
                    }

                    return ExportAiRunDetailResult.Success(
                        new ExportAiRunDetailReadModel(
                            Export4LMapping.MapRun(run),
                            findings.Select(Export4LMapping.MapFinding).ToArray(),
                            recommendations.Select(Export4LMapping.MapRecommendation).ToArray(),
                            contextSnapshot));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportAiRunDetailResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export AI service is unavailable.");
        }
    }

    public async Task<ExportAiRecommendationListResult> ListRecommendationsForExportCaseAsync(
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
                    var exportCase = await LoadExportCaseAsync(context, exportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportAiRecommendationListResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    var recommendations = await (
                        from rec in dbContext.ExportAiRecommendations.AsNoTracking()
                        join run in dbContext.ExportAiAnalysisRuns.AsNoTracking()
                            on rec.AnalysisRunId equals run.Id
                        where rec.TenantId == context.TenantId
                            && rec.FacilityId == context.FacilityId
                            && run.ExportCaseId == exportCaseId
                        orderby rec.CreatedAtUtc descending
                        select rec).ToListAsync(ct);

                    return ExportAiRecommendationListResult.Success(
                        recommendations.Select(Export4LMapping.MapRecommendation).ToArray());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportAiRecommendationListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export AI service is unavailable.");
        }
    }

    private async Task<ExportCase?> LoadExportCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCases
            .AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.Id == exportCaseId,
                cancellationToken);
}

public sealed class UnavailableExportAiReadRepository : IExportAiReadRepository
{
    private static readonly ExportAiRunListResult RunsUnavailable =
        ExportAiRunListResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export AI persistence is disabled.");

    private static readonly ExportAiRunDetailResult DetailUnavailable =
        ExportAiRunDetailResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export AI persistence is disabled.");

    private static readonly ExportAiRecommendationListResult RecommendationsUnavailable =
        ExportAiRecommendationListResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export AI persistence is disabled.");

    public Task<ExportAiRunListResult> ListRunsForExportCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RunsUnavailable);

    public Task<ExportAiRunListResult> ListRunsForResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RunsUnavailable);

    public Task<ExportAiRunDetailResult> GetRunDetailAsync(
        ExportOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(DetailUnavailable);

    public Task<ExportAiRecommendationListResult> ListRecommendationsForExportCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RecommendationsUnavailable);
}
