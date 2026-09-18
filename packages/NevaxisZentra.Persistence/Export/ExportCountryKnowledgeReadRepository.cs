using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportCountryKnowledgeReadRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCountryKnowledgeReadRepository
{
    public async Task<ExportCountryKnowledgeListResult> ListKnowledgeEntriesAsync(
        ExportOperationContext context,
        string? countryCode,
        string? verificationStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var query = dbContext.ExportCountryKnowledgeEntries
                        .AsNoTracking()
                        .Where(e => e.TenantId == context.TenantId);

                    if (!string.IsNullOrWhiteSpace(countryCode))
                    {
                        var normalizedCountry = countryCode.Trim().ToUpperInvariant();
                        query = query.Where(e => e.CountryCode == normalizedCountry);
                    }

                    if (!string.IsNullOrWhiteSpace(verificationStatus)
                        && TryParseVerificationStatus(verificationStatus, out var parsedStatus))
                    {
                        query = query.Where(e => e.VerificationStatus == parsedStatus);
                    }

                    var entries = await query
                        .OrderByDescending(e => e.UpdatedAtUtc)
                        .ThenBy(e => e.Title)
                        .ToListAsync(ct);

                    var items = entries.Select(Export3LMapping.MapKnowledgeEntry).ToList();
                    return ExportCountryKnowledgeListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryKnowledgeListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country knowledge service is unavailable.");
        }
    }

    public async Task<ExportCountryKnowledgeListResult> MatchKnowledgeAsync(
        ExportOperationContext context,
        string countryCode,
        string? hsCode,
        string? productScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var normalizedCountry = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCountry.Length != 2)
        {
            return ExportCountryKnowledgeListResult.Fail(
                ExportFailureCodes.InvalidDestinationCountry,
                "Country code must be a 2-letter ISO-style uppercase code.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var entries = await dbContext.ExportCountryKnowledgeEntries
                        .AsNoTracking()
                        .Where(e =>
                            e.TenantId == context.TenantId
                            && e.CountryCode == normalizedCountry
                            && e.VerificationStatus == ExportSourceVerificationStatus.Verified
                            && (e.EffectiveFrom == null || e.EffectiveFrom <= today)
                            && (e.EffectiveTo == null || e.EffectiveTo >= today))
                        .OrderByDescending(e => e.UpdatedAtUtc)
                        .ToListAsync(ct);

                    var normalizedProductScope = string.IsNullOrWhiteSpace(productScope)
                        ? null
                        : productScope.Trim();
                    var normalizedHsCode = string.IsNullOrWhiteSpace(hsCode) ? null : hsCode.Trim();

                    var matched = entries
                        .Where(e => MatchesProductScope(e, normalizedProductScope))
                        .Where(e => MatchesHsCode(e, normalizedHsCode))
                        .Select(Export3LMapping.MapKnowledgeEntry)
                        .ToList();

                    return ExportCountryKnowledgeListResult.Success(matched);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryKnowledgeListResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country knowledge service is unavailable.");
        }
    }

    private static bool MatchesProductScope(ExportCountryKnowledgeEntry entry, string? productScope)
    {
        if (string.IsNullOrWhiteSpace(entry.ProductScope))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(productScope))
        {
            return false;
        }

        return string.Equals(entry.ProductScope, productScope, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesHsCode(ExportCountryKnowledgeEntry entry, string? hsCode)
    {
        if (string.IsNullOrWhiteSpace(entry.HsCodePattern))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(hsCode))
        {
            return false;
        }

        if (entry.HsCodePattern.EndsWith(".xx", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = entry.HsCodePattern[..^3];
            return hsCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(hsCode, entry.HsCodePattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseVerificationStatus(string value, out ExportSourceVerificationStatus status)
    {
        var normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "unverified":
                status = ExportSourceVerificationStatus.Unverified;
                return true;
            case "under-review":
                status = ExportSourceVerificationStatus.UnderReview;
                return true;
            case "verified":
                status = ExportSourceVerificationStatus.Verified;
                return true;
            case "superseded":
                status = ExportSourceVerificationStatus.Superseded;
                return true;
            case "revalidation-required":
                status = ExportSourceVerificationStatus.RevalidationRequired;
                return true;
            default:
                status = default;
                return false;
        }
    }
}

public sealed class UnavailableExportCountryKnowledgeReadRepository : IExportCountryKnowledgeReadRepository
{
    private static readonly ExportCountryKnowledgeListResult Unavailable =
        ExportCountryKnowledgeListResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country knowledge persistence is disabled.");

    public Task<ExportCountryKnowledgeListResult> ListKnowledgeEntriesAsync(
        ExportOperationContext context,
        string? countryCode,
        string? verificationStatus,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable);

    public Task<ExportCountryKnowledgeListResult> MatchKnowledgeAsync(
        ExportOperationContext context,
        string countryCode,
        string? hsCode,
        string? productScope,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable);
}
