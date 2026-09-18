using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportCountryRulePackReadRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCountryRulePackReadRepository
{
    public async Task<ExportCountryRulePackListResult> ListRulePacksAsync(
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
                    var query = dbContext.ExportCountryRulePacks
                        .AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId);

                    if (!string.IsNullOrWhiteSpace(countryCode))
                    {
                        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
                        query = query.Where(p => p.CountryCode == normalizedCountry);
                    }

                    if (!string.IsNullOrWhiteSpace(status)
                        && TryParseRulePackStatus(status, out var parsedStatus))
                    {
                        query = query.Where(p => p.Status == parsedStatus);
                    }

                    var packs = await query
                        .OrderByDescending(p => p.UpdatedAtUtc)
                        .ThenByDescending(p => p.VersionNumber)
                        .ToListAsync(ct);

                    var items = packs.Select(Export3LMapping.MapRulePack).ToList();
                    return ExportCountryRulePackListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryRulePackListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country rule pack service is unavailable.");
        }
    }

    public async Task<ExportCountryRulePackDetailResult> GetRulePackAsync(
        ExportOperationContext context,
        Guid rulePackId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var pack = await dbContext.ExportCountryRulePacks
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            p => p.TenantId == context.TenantId && p.Id == rulePackId,
                            ct);

                    if (pack is null)
                    {
                        return ExportCountryRulePackDetailResult.Missing();
                    }

                    var rules = await dbContext.ExportCountryComplianceRules
                        .AsNoTracking()
                        .Where(r => r.TenantId == context.TenantId && r.RulePackId == rulePackId)
                        .OrderBy(r => r.RuleCode)
                        .ToListAsync(ct);

                    return ExportCountryRulePackDetailResult.Success(
                        Export3LMapping.MapRulePack(pack),
                        rules.Select(Export3LMapping.MapRule).ToList());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryRulePackDetailResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country rule pack service is unavailable.");
        }
    }

    private static bool TryParseRulePackStatus(string value, out ExportCountryRulePackStatus status)
    {
        var normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "draft":
                status = ExportCountryRulePackStatus.Draft;
                return true;
            case "under-review":
                status = ExportCountryRulePackStatus.UnderReview;
                return true;
            case "verified":
                status = ExportCountryRulePackStatus.Verified;
                return true;
            case "superseded":
                status = ExportCountryRulePackStatus.Superseded;
                return true;
            case "archived":
                status = ExportCountryRulePackStatus.Archived;
                return true;
            default:
                status = default;
                return false;
        }
    }
}

public sealed class UnavailableExportCountryRulePackReadRepository : IExportCountryRulePackReadRepository
{
    private static readonly ExportCountryRulePackListResult ListUnavailable =
        ExportCountryRulePackListResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country rule pack persistence is disabled.");

    private static readonly ExportCountryRulePackDetailResult DetailUnavailable =
        ExportCountryRulePackDetailResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country rule pack persistence is disabled.");

    public Task<ExportCountryRulePackListResult> ListRulePacksAsync(
        ExportOperationContext context,
        string? countryCode,
        string? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ListUnavailable);

    public Task<ExportCountryRulePackDetailResult> GetRulePackAsync(
        ExportOperationContext context,
        Guid rulePackId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(DetailUnavailable);
}
