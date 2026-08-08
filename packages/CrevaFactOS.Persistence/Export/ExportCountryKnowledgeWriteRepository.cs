using System.Text.RegularExpressions;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportCountryKnowledgeWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCountryKnowledgeWriteRepository
{
    private static readonly Regex CountryCodePattern = new("^[A-Z]{2}$", RegexOptions.CultureInvariant);

    public async Task<ExportCountryKnowledgeWriteResult> CreateKnowledgeEntryAsync(
        ExportOperationContext context,
        CreateCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var countryCode = NormalizeCountryCode(command.CountryCode);
        if (countryCode is null)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.InvalidDestinationCountry,
                "Country code must be a 2-letter ISO-style uppercase code.");
        }

        var title = (command.Title ?? string.Empty).Trim();
        var summary = (command.Summary ?? string.Empty).Trim();
        var sourcePublisher = (command.SourcePublisher ?? string.Empty).Trim();
        var sourceTitle = (command.SourceTitle ?? string.Empty).Trim();

        if (title.Length == 0 || summary.Length == 0 || sourcePublisher.Length == 0 || sourceTitle.Length == 0)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                Export3LFailureCodes.SourceRequired,
                "Title, summary, source publisher, and source title are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var entry = new ExportCountryKnowledgeEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        CountryCode = countryCode,
                        Category = command.Category,
                        Title = title,
                        Summary = summary,
                        SourcePublisher = sourcePublisher,
                        SourceTitle = sourceTitle,
                        SourceUrl = string.IsNullOrWhiteSpace(command.SourceUrl) ? null : command.SourceUrl.Trim(),
                        SourceAccessedAtUtc = now,
                        Authority = command.Authority,
                        VerificationStatus = ExportSourceVerificationStatus.Unverified,
                        ProductScope = string.IsNullOrWhiteSpace(command.ProductScope)
                            ? null
                            : command.ProductScope.Trim(),
                        ProductGroup = string.IsNullOrWhiteSpace(command.ProductGroup)
                            ? null
                            : command.ProductGroup.Trim(),
                        HsCodePattern = string.IsNullOrWhiteSpace(command.HsCodePattern)
                            ? null
                            : command.HsCodePattern.Trim(),
                        CreatedFromResearchCaseId = command.CreatedFromResearchCaseId,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportCountryKnowledgeEntries.Add(entry);

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryKnowledgeCreated,
                        ExportAuditActions.ExportCountryKnowledgeCreated,
                        "export.country.knowledge.entry",
                        entry.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            knowledgeEntryId = entry.Id.ToString(),
                            countryCode = entry.CountryCode,
                            category = ExportValueConversions.ToStorage(entry.Category),
                            title = entry.Title,
                        });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated,
                        "export-country-knowledge-entry",
                        entry.Id.ToString(),
                        correlationId,
                        $"{entry.Id}:{entry.Version}:{ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["knowledgeEntryId"] = entry.Id.ToString(),
                            ["countryCode"] = entry.CountryCode,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryKnowledgeWriteResult.Success(entry.Id, entry.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The knowledge entry has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country knowledge service is unavailable.");
        }
    }

    public async Task<ExportCountryKnowledgeWriteResult> VerifyKnowledgeEntryAsync(
        ExportOperationContext context,
        VerifyCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var entry = await dbContext.ExportCountryKnowledgeEntries
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId && e.Id == command.KnowledgeEntryId,
                            ct);

                    if (entry is null)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            Export3LFailureCodes.KnowledgeEntryNotFound,
                            "Knowledge entry was not found.");
                    }

                    if (entry.Version != command.ExpectedVersion)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The knowledge entry has been modified. Please reload and try again.");
                    }

                    if (entry.VerificationStatus == ExportSourceVerificationStatus.Verified)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            Export3LFailureCodes.AlreadyVerified,
                            "Knowledge entry is already verified.");
                    }

                    if (entry.VerificationStatus == ExportSourceVerificationStatus.Superseded)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            ExportFailureCodes.ValidationFailed,
                            "Superseded knowledge entries cannot be verified.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    entry.VerificationStatus = ExportSourceVerificationStatus.Verified;
                    entry.VerifiedAtUtc = now;
                    entry.VerifiedBySubjectId = actorSubjectId;
                    entry.Version += 1;
                    entry.UpdatedAtUtc = now;
                    entry.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryKnowledgeVerified,
                        ExportAuditActions.ExportCountryKnowledgeVerified,
                        "export.country.knowledge.entry",
                        entry.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            knowledgeEntryId = entry.Id.ToString(),
                            countryCode = entry.CountryCode,
                            title = entry.Title,
                        });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated,
                        "export-country-knowledge-entry",
                        entry.Id.ToString(),
                        correlationId,
                        $"{entry.Id}:{entry.Version}:{ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["knowledgeEntryId"] = entry.Id.ToString(),
                            ["countryCode"] = entry.CountryCode,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryKnowledgeWriteResult.Success(entry.Id, entry.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The knowledge entry has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country knowledge service is unavailable.");
        }
    }

    public async Task<ExportCountryKnowledgeWriteResult> SupersedeKnowledgeEntryAsync(
        ExportOperationContext context,
        SupersedeCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var oldEntry = await dbContext.ExportCountryKnowledgeEntries
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId && e.Id == command.KnowledgeEntryId,
                            ct);

                    if (oldEntry is null)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            Export3LFailureCodes.KnowledgeEntryNotFound,
                            "Knowledge entry was not found.");
                    }

                    if (oldEntry.Version != command.ExpectedVersion)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The knowledge entry has been modified. Please reload and try again.");
                    }

                    if (oldEntry.VerificationStatus != ExportSourceVerificationStatus.Verified)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            ExportFailureCodes.ValidationFailed,
                            "Only verified knowledge entries can be superseded.");
                    }

                    var newEntry = await dbContext.ExportCountryKnowledgeEntries
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId && e.Id == command.NewEntryId,
                            ct);

                    if (newEntry is null)
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            Export3LFailureCodes.KnowledgeEntryNotFound,
                            "Replacement knowledge entry was not found.");
                    }

                    if (!string.Equals(oldEntry.CountryCode, newEntry.CountryCode, StringComparison.Ordinal))
                    {
                        return ExportCountryKnowledgeWriteResult.Fail(
                            ExportFailureCodes.ValidationFailed,
                            "Replacement knowledge entry must target the same country.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    oldEntry.VerificationStatus = ExportSourceVerificationStatus.Superseded;
                    oldEntry.SupersededById = newEntry.Id;
                    oldEntry.Version += 1;
                    oldEntry.UpdatedAtUtc = now;
                    oldEntry.LastModifiedBySubjectId = actorSubjectId;

                    newEntry.Version += 1;
                    newEntry.UpdatedAtUtc = now;
                    newEntry.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryKnowledgeSuperseded,
                        ExportAuditActions.ExportCountryKnowledgeSuperseded,
                        "export.country.knowledge.entry",
                        oldEntry.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            knowledgeEntryId = oldEntry.Id.ToString(),
                            newEntryId = newEntry.Id.ToString(),
                            countryCode = oldEntry.CountryCode,
                        });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated,
                        "export-country-knowledge-entry",
                        oldEntry.Id.ToString(),
                        correlationId,
                        $"{oldEntry.Id}:{oldEntry.Version}:{ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["knowledgeEntryId"] = oldEntry.Id.ToString(),
                            ["newEntryId"] = newEntry.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryKnowledgeWriteResult.Success(oldEntry.Id, oldEntry.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The knowledge entry has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryKnowledgeWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country knowledge service is unavailable.");
        }
    }

    private static string? NormalizeCountryCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return CountryCodePattern.IsMatch(normalized) ? normalized : null;
    }
}

public sealed class UnavailableExportCountryKnowledgeWriteRepository : IExportCountryKnowledgeWriteRepository
{
    private static ExportCountryKnowledgeWriteResult Unavailable() =>
        ExportCountryKnowledgeWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country knowledge persistence is disabled.");

    public Task<ExportCountryKnowledgeWriteResult> CreateKnowledgeEntryAsync(
        ExportOperationContext context,
        CreateCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ExportCountryKnowledgeWriteResult> VerifyKnowledgeEntryAsync(
        ExportOperationContext context,
        VerifyCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<ExportCountryKnowledgeWriteResult> SupersedeKnowledgeEntryAsync(
        ExportOperationContext context,
        SupersedeCountryKnowledgeEntryCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
