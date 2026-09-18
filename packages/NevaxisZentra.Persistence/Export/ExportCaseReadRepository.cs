using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportCaseReadRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCaseReadRepository
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;
    private const int DefaultActivityLimit = 50;

    public async Task<ExportCaseListResult> ListAsync(
        ExportOperationContext context,
        string? statusFilter,
        string? readinessFilter,
        string? searchFilter,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportCaseListResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var effectiveLimit = limit.HasValue
                        ? Math.Clamp(limit.Value, 1, MaxLimit)
                        : DefaultLimit;

                    var query = dbContext.ExportCases
                        .AsNoTracking()
                        .Where(c =>
                            c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId);

                    if (!string.IsNullOrWhiteSpace(statusFilter)
                        && ExportValueConversions.TryCaseStatusFromStorage(
                            statusFilter.Trim().ToLowerInvariant(),
                            out var status))
                    {
                        query = query.Where(c => c.Status == status);
                    }

                    if (!string.IsNullOrWhiteSpace(readinessFilter))
                    {
                        var normalized = readinessFilter.Trim().ToLowerInvariant();
                        try
                        {
                            var readiness = ExportValueConversions.ReadinessStatusFromStorage(normalized);
                            query = query.Where(c => c.ReadinessStatus == readiness);
                        }
                        catch (InvalidOperationException)
                        {
                            // Ignore unknown readiness filter.
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(searchFilter))
                    {
                        var pattern = $"%{searchFilter.Trim()}%";
                        query = query.Where(c =>
                            EF.Functions.ILike(c.CaseNumber, pattern)
                            || EF.Functions.ILike(c.SourceOrderReference, pattern)
                            || EF.Functions.ILike(c.CustomerName, pattern));
                    }

                    var entities = await query
                        .OrderByDescending(c => c.UpdatedAtUtc)
                        .ThenBy(c => c.CaseNumber)
                        .Take(effectiveLimit)
                        .ToListAsync(ct);

                    return ExportCaseListResult.Success(entities.Select(MapList).ToList());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCaseListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export case list is unavailable.");
        }
    }

    public async Task<ExportCaseDetailResult> FindByIdAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportCaseDetailResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var entity = await dbContext.ExportCases
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            c =>
                                c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    return entity is null
                        ? ExportCaseDetailResult.Missing()
                        : ExportCaseDetailResult.Success(MapDetail(entity));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCaseDetailResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export case detail is unavailable.");
        }
    }

    public async Task<ExportCaseDetailResult> FindBySourceOrderIdAsync(
        ExportOperationContext context,
        Guid sourceOrderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportCaseDetailResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var entity = await dbContext.ExportCases
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            c =>
                                c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.SourceOrderId == sourceOrderId,
                            ct);

                    return entity is null
                        ? ExportCaseDetailResult.Missing()
                        : ExportCaseDetailResult.Success(MapDetail(entity));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCaseDetailResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export case lookup by order is unavailable.");
        }
    }

    public async Task<ExportReadinessResult> GetReadinessAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportReadinessResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var exportCase = await dbContext.ExportCases
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            c =>
                                c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    if (exportCase is null)
                    {
                        return ExportReadinessResult.Missing();
                    }

                    var documents = await dbContext.ExportDocuments
                        .AsNoTracking()
                        .Where(d =>
                            d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.ExportCaseId == exportCaseId)
                        .ToListAsync(ct);

                    var helper = new ExportReadinessPersistence(dbContext);
                    var operationalContext = await helper.BuildOperationalContextAsync(
                        context, exportCase, documents, ct);
                    var calc = ExportReadinessEngine.Calculate(exportCase, documents, operationalContext);
                    return ExportReadinessResult.Success(new ExportReadinessReadModel(
                        exportCase.Id,
                        ExportValueConversions.ToStorage(calc.ReadinessStatus),
                        calc.ReadinessPercent,
                        calc.BlockingIssueCount,
                        calc.MissingRequirementCount,
                        ExportValueConversions.ToStorage(calc.SuggestedCaseStatus),
                        calc.Requirements));
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportReadinessResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export readiness is unavailable.");
        }
    }

    public async Task<ExportDocumentListResult> ListDocumentsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportDocumentListResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var exists = await dbContext.ExportCases
                        .AsNoTracking()
                        .AnyAsync(
                            c =>
                                c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    if (!exists)
                    {
                        return ExportDocumentListResult.Missing();
                    }

                    var documents = await dbContext.ExportDocuments
                        .AsNoTracking()
                        .Where(d =>
                            d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.ExportCaseId == exportCaseId)
                        .OrderBy(d => d.DocumentType)
                        .ThenByDescending(d => d.UpdatedAtUtc)
                        .ToListAsync(ct);

                    var items = documents.Select(d => new ExportDocumentReadModel(
                        d.Id,
                        d.ExportCaseId,
                        d.DocumentType,
                        ExportValueConversions.ToStorage(d.DocumentStatus),
                        d.DisplayName,
                        d.FileReference,
                        d.DocumentNumber,
                        d.IssuedAtUtc,
                        d.ExpiresAtUtc,
                        d.Source,
                        d.RejectionReason,
                        d.VerifiedAtUtc,
                        d.VerifiedBySubjectId,
                        d.Version,
                        d.CreatedAtUtc,
                        d.UpdatedAtUtc)).ToList();

                    return ExportDocumentListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportDocumentListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export documents are unavailable.");
        }
    }

    public async Task<ExportActivityListResult> ListActivityAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportActivityListResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var exists = await dbContext.ExportCases
                        .AsNoTracking()
                        .AnyAsync(
                            c =>
                                c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == exportCaseId,
                            ct);

                    if (!exists)
                    {
                        return ExportActivityListResult.Missing();
                    }

                    var effectiveLimit = limit.HasValue
                        ? Math.Clamp(limit.Value, 1, MaxLimit)
                        : DefaultActivityLimit;

                    var activities = await dbContext.ExportCaseActivities
                        .AsNoTracking()
                        .Where(a =>
                            a.TenantId == context.TenantId
                            && a.FacilityId == context.FacilityId
                            && a.ExportCaseId == exportCaseId)
                        .OrderByDescending(a => a.OccurredAtUtc)
                        .Take(effectiveLimit)
                        .ToListAsync(ct);

                    var items = activities.Select(a => new ExportCaseActivityReadModel(
                        a.Id,
                        a.ExportCaseId,
                        a.EventType,
                        a.Title,
                        a.Summary,
                        a.OccurredAtUtc,
                        ExportValueConversions.ToStorage(a.SourceType),
                        a.ActorSubjectId,
                        a.RelatedEntityType,
                        a.RelatedEntityId,
                        a.CorrelationId)).ToList();

                    return ExportActivityListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportActivityListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export activity is unavailable.");
        }
    }

    private static ExportCaseListReadModel MapList(ExportCase c) =>
        new(
            c.Id,
            c.FacilityId,
            c.SourceOrderId,
            c.CaseNumber,
            c.SourceOrderReference,
            c.CustomerName,
            c.DestinationCountryCode,
            c.OriginCountryCode,
            c.IncotermCode,
            ExportValueConversions.ToStorage(c.Status),
            ExportValueConversions.ToStorage(c.ReadinessStatus),
            c.ReadinessPercent,
            c.BlockingIssueCount,
            c.MissingRequirementCount,
            c.Version,
            c.UpdatedAtUtc);

    private static ExportCaseDetailReadModel MapDetail(ExportCase c) =>
        new(
            c.Id,
            c.FacilityId,
            c.SourceOrderId,
            c.CaseNumber,
            c.SourceOrderReference,
            c.CustomerName,
            c.DestinationCountryCode,
            c.OriginCountryCode,
            c.IncotermCode,
            ExportValueConversions.ToStorage(c.Status),
            ExportValueConversions.ToStorage(c.ReadinessStatus),
            c.ReadinessPercent,
            c.BlockingIssueCount,
            c.MissingRequirementCount,
            c.CreatedBySubjectId,
            c.LastModifiedBySubjectId,
            c.Version,
            c.CreatedAtUtc,
            c.UpdatedAtUtc);
}

public sealed class UnavailableExportCaseReadRepository : IExportCaseReadRepository
{
    private static readonly ExportCaseListResult UnavailableList =
        ExportCaseListResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    private static readonly ExportCaseDetailResult UnavailableDetail =
        ExportCaseDetailResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    private static readonly ExportReadinessResult UnavailableReadiness =
        ExportReadinessResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    private static readonly ExportDocumentListResult UnavailableDocuments =
        ExportDocumentListResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    private static readonly ExportActivityListResult UnavailableActivity =
        ExportActivityListResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    public Task<ExportCaseListResult> ListAsync(
        ExportOperationContext context,
        string? statusFilter,
        string? readinessFilter,
        string? searchFilter,
        int? limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableList);

    public Task<ExportCaseDetailResult> FindByIdAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDetail);

    public Task<ExportCaseDetailResult> FindBySourceOrderIdAsync(
        ExportOperationContext context,
        Guid sourceOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDetail);

    public Task<ExportReadinessResult> GetReadinessAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableReadiness);

    public Task<ExportDocumentListResult> ListDocumentsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDocuments);

    public Task<ExportActivityListResult> ListActivityAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        int? limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableActivity);
}
