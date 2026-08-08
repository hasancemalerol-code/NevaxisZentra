using System.Text.RegularExpressions;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportCountryResearchWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCountryResearchWriteRepository
{
    private static readonly Regex CountryCodePattern = new("^[A-Z]{2}$", RegexOptions.CultureInvariant);

    public async Task<ExportCountryResearchWriteResult> CreateResearchCaseAsync(
        ExportOperationContext context,
        CreateCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var countryCode = NormalizeCountryCode(command.CountryCode);
        if (countryCode is null)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.InvalidDestinationCountry,
                "Country code must be a 2-letter ISO-style uppercase code.");
        }

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var year = DateTime.UtcNow.Year;
                    var prefix = $"RES-{countryCode}-{year}-";
                    var existingCount = await dbContext.ExportCountryResearchCases
                        .CountAsync(
                            r => r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && r.ResearchNumber.StartsWith(prefix),
                            ct);

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var researchCase = new ExportCountryResearchCase
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ResearchNumber = $"{prefix}{existingCount + 1}",
                        CountryCode = countryCode,
                        ProductReference = string.IsNullOrWhiteSpace(command.ProductReference)
                            ? null
                            : command.ProductReference.Trim(),
                        ProductDescription = string.IsNullOrWhiteSpace(command.ProductDescription)
                            ? null
                            : command.ProductDescription.Trim(),
                        ProductGroup = string.IsNullOrWhiteSpace(command.ProductGroup)
                            ? null
                            : command.ProductGroup.Trim(),
                        HsCode = string.IsNullOrWhiteSpace(command.HsCode) ? null : command.HsCode.Trim(),
                        OriginCountryCode = NormalizeCountryCode(command.OriginCountryCode),
                        Status = ExportCountryResearchCaseStatus.Draft,
                        PurposeNotes = string.IsNullOrWhiteSpace(command.PurposeNotes)
                            ? null
                            : command.PurposeNotes.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportCountryResearchCases.Add(researchCase);

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryResearchCreated,
                        ExportAuditActions.ExportCountryResearchCreated,
                        "export.country.research.case",
                        researchCase.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            researchCaseId = researchCase.Id.ToString(),
                            researchNumber = researchCase.ResearchNumber,
                            countryCode = researchCase.CountryCode,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryResearchWriteResult.Success(researchCase.Id, researchCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "A research case number conflict occurred. Please reload and try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The research case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    public async Task<ExportCountryResearchFindingWriteResult> AddFindingAsync(
        ExportOperationContext context,
        AddCountryResearchFindingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var title = (command.Title ?? string.Empty).Trim();
        var summary = (command.Summary ?? string.Empty).Trim();
        var sourcePublisher = (command.SourcePublisher ?? string.Empty).Trim();
        var sourceTitle = (command.SourceTitle ?? string.Empty).Trim();

        if (title.Length == 0 || summary.Length == 0 || sourcePublisher.Length == 0 || sourceTitle.Length == 0)
        {
            return ExportCountryResearchFindingWriteResult.Fail(
                Export3LFailureCodes.SourceRequired,
                "Title, summary, source publisher, and source title are required.");
        }

        if (command.Confidence is < 0 or > 100)
        {
            return ExportCountryResearchFindingWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Confidence must be between 0 and 100.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var researchCase = await LoadResearchCaseAsync(context, command.ResearchCaseId, ct);
                    if (researchCase is null)
                    {
                        return ExportCountryResearchFindingWriteResult.Fail(
                            Export3LFailureCodes.ResearchCaseNotFound,
                            "Research case was not found.");
                    }

                    if (researchCase.Version != command.ExpectedResearchVersion)
                    {
                        return ExportCountryResearchFindingWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The research case has been modified. Please reload and try again.");
                    }

                    if (researchCase.Status is ExportCountryResearchCaseStatus.Completed
                        or ExportCountryResearchCaseStatus.Archived)
                    {
                        return ExportCountryResearchFindingWriteResult.Fail(
                            Export3LFailureCodes.InvalidResearchStatus,
                            "Findings cannot be added to a completed or archived research case.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var finding = new ExportCountryResearchFinding
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        CountryResearchCaseId = researchCase.Id,
                        Category = command.Category,
                        Title = title,
                        Summary = summary,
                        BlockingPotential = command.BlockingPotential,
                        SourcePublisher = sourcePublisher,
                        SourceTitle = sourceTitle,
                        SourceUrl = string.IsNullOrWhiteSpace(command.SourceUrl) ? null : command.SourceUrl.Trim(),
                        SourcePublicationDate = command.SourcePublicationDate,
                        SourceAccessedAtUtc = now,
                        Confidence = command.Confidence,
                        VerificationStatus = ExportSourceVerificationStatus.Unverified,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportCountryResearchFindings.Add(finding);

                    if (researchCase.Status == ExportCountryResearchCaseStatus.Draft)
                    {
                        researchCase.Status = ExportCountryResearchCaseStatus.Researching;
                    }

                    researchCase.Version += 1;
                    researchCase.UpdatedAtUtc = now;
                    researchCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryResearchFindingAdded,
                        ExportAuditActions.ExportCountryResearchFindingAdded,
                        "export.country.research.finding",
                        finding.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            researchCaseId = researchCase.Id.ToString(),
                            findingId = finding.Id.ToString(),
                            category = ExportValueConversions.ToStorage(finding.Category),
                            title = finding.Title,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryResearchFindingWriteResult.Success(finding.Id, researchCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryResearchFindingWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The research case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchFindingWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    public async Task<ExportCountryResearchWriteResult> ReviewResearchCaseAsync(
        ExportOperationContext context,
        ReviewCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var researchCase = await LoadResearchCaseAsync(context, command.ResearchCaseId, ct);
                    if (researchCase is null)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            Export3LFailureCodes.ResearchCaseNotFound,
                            "Research case was not found.");
                    }

                    if (researchCase.Version != command.ExpectedVersion)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The research case has been modified. Please reload and try again.");
                    }

                    if (researchCase.Status is not (ExportCountryResearchCaseStatus.Researching
                        or ExportCountryResearchCaseStatus.Draft))
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            Export3LFailureCodes.InvalidResearchStatus,
                            "Only draft or researching cases can be submitted for review.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    researchCase.Status = ExportCountryResearchCaseStatus.ReviewRequired;
                    researchCase.ReviewedAtUtc = now;
                    researchCase.ReviewedBySubjectId = actorSubjectId;
                    researchCase.Version += 1;
                    researchCase.UpdatedAtUtc = now;
                    researchCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryResearchReviewed,
                        ExportAuditActions.ExportCountryResearchReviewed,
                        "export.country.research.case",
                        researchCase.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            researchCaseId = researchCase.Id.ToString(),
                            researchNumber = researchCase.ResearchNumber,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryResearchWriteResult.Success(researchCase.Id, researchCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The research case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    public async Task<ExportCountryResearchWriteResult> CompleteResearchCaseAsync(
        ExportOperationContext context,
        CompleteCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var researchCase = await LoadResearchCaseAsync(context, command.ResearchCaseId, ct);
                    if (researchCase is null)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            Export3LFailureCodes.ResearchCaseNotFound,
                            "Research case was not found.");
                    }

                    if (researchCase.Version != command.ExpectedVersion)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The research case has been modified. Please reload and try again.");
                    }

                    if (researchCase.Status is not (ExportCountryResearchCaseStatus.ReviewRequired
                        or ExportCountryResearchCaseStatus.Verified))
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            Export3LFailureCodes.InvalidResearchStatus,
                            "Only review-required or verified research cases can be completed.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    researchCase.Status = ExportCountryResearchCaseStatus.Completed;
                    researchCase.CompletedAtUtc = now;
                    researchCase.Version += 1;
                    researchCase.UpdatedAtUtc = now;
                    researchCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryResearchCompleted,
                        ExportAuditActions.ExportCountryResearchCompleted,
                        "export.country.research.case",
                        researchCase.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            researchCaseId = researchCase.Id.ToString(),
                            researchNumber = researchCase.ResearchNumber,
                        });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportCountryResearchCompleted,
                        "export-country-research-case",
                        researchCase.Id.ToString(),
                        correlationId,
                        $"{researchCase.Id}:{researchCase.Version}:{ExportOutboxMessageTypes.ExportCountryResearchCompleted}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["researchCaseId"] = researchCase.Id.ToString(),
                            ["researchNumber"] = researchCase.ResearchNumber,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryResearchWriteResult.Success(researchCase.Id, researchCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The research case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    public async Task<ExportCountryResearchWriteResult> LinkToExportCaseAsync(
        ExportOperationContext context,
        LinkResearchToExportCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var researchCase = await LoadResearchCaseAsync(context, command.ResearchCaseId, ct);
                    if (researchCase is null)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            Export3LFailureCodes.ResearchCaseNotFound,
                            "Research case was not found.");
                    }

                    if (researchCase.Version != command.ExpectedResearchVersion)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The research case has been modified. Please reload and try again.");
                    }

                    var exportCase = await dbContext.ExportCases
                        .SingleOrDefaultAsync(
                            c => c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == command.ExportCaseId,
                            ct);

                    if (exportCase is null)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    if (exportCase.Version != command.ExpectedCaseVersion)
                    {
                        return ExportCountryResearchWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export case has been modified. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    researchCase.LinkedExportCaseId = exportCase.Id;
                    researchCase.Version += 1;
                    researchCase.UpdatedAtUtc = now;
                    researchCase.LastModifiedBySubjectId = actorSubjectId;

                    exportCase.LinkedResearchCaseId = researchCase.Id;
                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.CountryResearchLinked,
                        "Country research linked",
                        researchCase.ResearchNumber,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.country.research.case",
                        researchCase.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryResearchLinked,
                        ExportAuditActions.ExportCountryResearchLinked,
                        "export.country.research.case",
                        researchCase.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            researchCaseId = researchCase.Id.ToString(),
                            exportCaseId = exportCase.Id.ToString(),
                            researchNumber = researchCase.ResearchNumber,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryResearchWriteResult.Success(researchCase.Id, researchCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The research case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryResearchWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country research service is unavailable.");
        }
    }

    private async Task<ExportCountryResearchCase?> LoadResearchCaseAsync(
        ExportOperationContext context,
        Guid researchCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCountryResearchCases
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.Id == researchCaseId,
                cancellationToken);

    private static string? NormalizeCountryCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return CountryCodePattern.IsMatch(normalized) ? normalized : null;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

public sealed class UnavailableExportCountryResearchWriteRepository : IExportCountryResearchWriteRepository
{
    private static ExportCountryResearchWriteResult CaseUnavailable() =>
        ExportCountryResearchWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country research persistence is disabled.");

    private static ExportCountryResearchFindingWriteResult FindingUnavailable() =>
        ExportCountryResearchFindingWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country research persistence is disabled.");

    public Task<ExportCountryResearchWriteResult> CreateResearchCaseAsync(
        ExportOperationContext context,
        CreateCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CaseUnavailable());

    public Task<ExportCountryResearchFindingWriteResult> AddFindingAsync(
        ExportOperationContext context,
        AddCountryResearchFindingCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(FindingUnavailable());

    public Task<ExportCountryResearchWriteResult> ReviewResearchCaseAsync(
        ExportOperationContext context,
        ReviewCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CaseUnavailable());

    public Task<ExportCountryResearchWriteResult> CompleteResearchCaseAsync(
        ExportOperationContext context,
        CompleteCountryResearchCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CaseUnavailable());

    public Task<ExportCountryResearchWriteResult> LinkToExportCaseAsync(
        ExportOperationContext context,
        LinkResearchToExportCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CaseUnavailable());
}
