using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportComplianceReadRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportComplianceReadRepository
{
    public async Task<ExportComplianceSummaryResult> GetComplianceSummaryAsync(
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
                    var exportCase = await LoadCaseAsync(context, exportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportComplianceSummaryResult.Missing();
                    }

                    var summary = await BuildSummaryAsync(context, exportCase, ct);
                    return ExportComplianceSummaryResult.Success(summary);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportComplianceSummaryResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export compliance service is unavailable.");
        }
    }

    public async Task<ExportComplianceEvaluationListResult> ListEvaluationsAsync(
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
                    var exportCase = await LoadCaseAsync(context, exportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportComplianceEvaluationListResult.Missing();
                    }

                    var evaluations = await dbContext.ExportComplianceEvaluations
                        .AsNoTracking()
                        .Where(e =>
                            e.TenantId == context.TenantId
                            && e.FacilityId == context.FacilityId
                            && e.ExportCaseId == exportCaseId)
                        .OrderByDescending(e => e.EvaluationNumber)
                        .ToListAsync(ct);

                    return ExportComplianceEvaluationListResult.Success(
                        evaluations.Select(Export3LMapping.MapEvaluation).ToArray());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportComplianceEvaluationListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export compliance service is unavailable.");
        }
    }

    private async Task<ExportComplianceSummaryReadModel> BuildSummaryAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        CancellationToken cancellationToken)
    {
        if (!exportCase.CurrentComplianceEvaluationId.HasValue)
        {
            return new ExportComplianceSummaryReadModel(
                exportCase.Id,
                ExportValueConversions.ToStorage(exportCase.ComplianceStatus),
                0,
                exportCase.ComplianceRevalidationRequired,
                null,
                0,
                0,
                Array.Empty<ExportComplianceRequirementReadModel>(),
                null);
        }

        var evaluation = await dbContext.ExportComplianceEvaluations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                e => e.TenantId == context.TenantId
                    && e.FacilityId == context.FacilityId
                    && e.Id == exportCase.CurrentComplianceEvaluationId.Value,
                cancellationToken);

        if (evaluation is null)
        {
            return new ExportComplianceSummaryReadModel(
                exportCase.Id,
                ExportValueConversions.ToStorage(exportCase.ComplianceStatus),
                0,
                exportCase.ComplianceRevalidationRequired,
                exportCase.CurrentComplianceEvaluationId,
                0,
                0,
                Array.Empty<ExportComplianceRequirementReadModel>(),
                null);
        }

        return await BuildSummaryForEvaluationAsync(context, exportCase, evaluation, cancellationToken);
    }

    private async Task<ExportComplianceSummaryReadModel> BuildSummaryForEvaluationAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        ExportComplianceEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        var requirements = await dbContext.ExportComplianceRequirements
            .AsNoTracking()
            .Where(r =>
                r.TenantId == context.TenantId
                && r.FacilityId == context.FacilityId
                && r.EvaluationId == evaluation.Id)
            .OrderBy(r => r.RequirementCode)
            .ToListAsync(cancellationToken);

        var requirementModels = requirements.Select(Export3LMapping.MapRequirement).ToList();
        var blockingCount = requirements.Count(r =>
            r.Blocking
            && r.Status is not ExportComplianceRequirementStatus.Satisfied
                and not ExportComplianceRequirementStatus.NotApplicable);
        var missingCount = requirements.Count(r =>
            r.Status is ExportComplianceRequirementStatus.Missing or ExportComplianceRequirementStatus.Pending);

        CountryRulePackReadModel? rulePackModel = null;
        if (evaluation.RulePackId.HasValue)
        {
            var rulePack = await dbContext.ExportCountryRulePacks
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId && p.Id == evaluation.RulePackId.Value,
                    cancellationToken);

            if (rulePack is not null)
            {
                rulePackModel = Export3LMapping.MapRulePack(rulePack);
            }
        }

        return new ExportComplianceSummaryReadModel(
            exportCase.Id,
            ExportValueConversions.ToStorage(evaluation.Result),
            evaluation.ReadinessPercent,
            exportCase.ComplianceRevalidationRequired || evaluation.RevalidationRequired || evaluation.IsStale,
            evaluation.Id,
            blockingCount,
            missingCount,
            requirementModels,
            rulePackModel);
    }

    private async Task<ExportCase?> LoadCaseAsync(
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

public sealed class UnavailableExportComplianceReadRepository : IExportComplianceReadRepository
{
    private static readonly ExportComplianceSummaryResult Unavailable =
        ExportComplianceSummaryResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export compliance persistence is disabled.");

    public Task<ExportComplianceSummaryResult> GetComplianceSummaryAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable);

    private static readonly ExportComplianceEvaluationListResult EvaluationsUnavailable =
        ExportComplianceEvaluationListResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export compliance persistence is disabled.");

    public Task<ExportComplianceEvaluationListResult> ListEvaluationsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EvaluationsUnavailable);
}
