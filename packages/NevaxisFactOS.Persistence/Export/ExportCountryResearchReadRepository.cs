using System.Text.RegularExpressions;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportCountryResearchReadRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCountryResearchReadRepository
{
    public async Task<ExportCountryResearchListResult> ListResearchCasesAsync(
        ExportOperationContext context,
        string? countryCode,
        string? status,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var query = dbContext.ExportCountryResearchCases
                        .AsNoTracking()
                        .Where(r =>
                            r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId);

                    if (!string.IsNullOrWhiteSpace(countryCode))
                    {
                        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
                        query = query.Where(r => r.CountryCode == normalizedCountry);
                    }

                    if (!string.IsNullOrWhiteSpace(status)
                        && TryParseResearchStatus(status, out var parsedStatus))
                    {
                        query = query.Where(r => r.Status == parsedStatus);
                    }

                    var cases = await query
                        .OrderByDescending(r => r.UpdatedAtUtc)
                        .ThenByDescending(r => r.CreatedAtUtc)
                        .ToListAsync(ct);

                    var items = cases.Select(Export3LMapping.MapResearchCase).ToList();
                    return ExportCountryResearchListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    public async Task<ExportCountryResearchDetailResult> GetResearchCaseAsync(
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
                        return ExportCountryResearchDetailResult.Missing();
                    }

                    var findings = await dbContext.ExportCountryResearchFindings
                        .AsNoTracking()
                        .Where(f =>
                            f.TenantId == context.TenantId
                            && f.CountryResearchCaseId == researchCaseId)
                        .OrderBy(f => f.CreatedAtUtc)
                        .ToListAsync(ct);

                    return ExportCountryResearchDetailResult.Success(
                        Export3LMapping.MapResearchCase(researchCase),
                        findings.Select(Export3LMapping.MapFinding).ToList());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchDetailResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    private static bool TryParseResearchStatus(string value, out ExportCountryResearchCaseStatus status)
    {
        var normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "draft":
                status = ExportCountryResearchCaseStatus.Draft;
                return true;
            case "researching":
                status = ExportCountryResearchCaseStatus.Researching;
                return true;
            case "review-required":
                status = ExportCountryResearchCaseStatus.ReviewRequired;
                return true;
            case "verified":
                status = ExportCountryResearchCaseStatus.Verified;
                return true;
            case "completed":
                status = ExportCountryResearchCaseStatus.Completed;
                return true;
            case "archived":
                status = ExportCountryResearchCaseStatus.Archived;
                return true;
            default:
                status = default;
                return false;
        }
    }
}

public sealed class UnavailableExportCountryResearchReadRepository : IExportCountryResearchReadRepository
{
    private static readonly ExportCountryResearchListResult ListUnavailable =
        ExportCountryResearchListResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country research persistence is disabled.");

    private static readonly ExportCountryResearchDetailResult DetailUnavailable =
        ExportCountryResearchDetailResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country research persistence is disabled.");

    public Task<ExportCountryResearchListResult> ListResearchCasesAsync(
        ExportOperationContext context,
        string? countryCode,
        string? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ListUnavailable);

    public Task<ExportCountryResearchDetailResult> GetResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(DetailUnavailable);
}
