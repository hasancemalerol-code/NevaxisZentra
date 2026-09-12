using System.Globalization;
using System.Text.RegularExpressions;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportCountryRulePackWriteRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCountryRulePackWriteRepository
{
    private static readonly Regex CountryCodePattern = new("^[A-Z]{2}$", RegexOptions.CultureInvariant);

    public async Task<ExportCountryRulePackWriteResult> CreateRulePackAsync(
        ExportOperationContext context,
        CreateCountryRulePackCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var countryCode = NormalizeCountryCode(command.CountryCode);
        if (countryCode is null)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.InvalidDestinationCountry,
                "Country code must be a 2-letter ISO-style uppercase code.");
        }

        var name = (command.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Rule pack name is required.");
        }

        var sourceTitle = (command.SourceTitle ?? string.Empty).Trim();
        var sourcePublisher = (command.SourcePublisher ?? string.Empty).Trim();
        if (sourceTitle.Length == 0 || sourcePublisher.Length == 0)
        {
            return ExportCountryRulePackWriteResult.Fail(
                Export3LFailureCodes.SourceRequired,
                "Source title and publisher are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var maxVersionNumber = await dbContext.ExportCountryRulePacks
                        .Where(p => p.TenantId == context.TenantId && p.CountryCode == countryCode)
                        .Select(p => (int?)p.VersionNumber)
                        .MaxAsync(ct) ?? 0;

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var rulePack = new ExportCountryRulePack
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = null,
                        CountryCode = countryCode,
                        Name = name,
                        Status = ExportCountryRulePackStatus.Draft,
                        VersionNumber = maxVersionNumber + 1,
                        EffectiveFrom = command.EffectiveFrom,
                        EffectiveTo = command.EffectiveTo,
                        SourceTitle = sourceTitle,
                        SourceUrl = string.IsNullOrWhiteSpace(command.SourceUrl) ? null : command.SourceUrl.Trim(),
                        SourcePublisher = sourcePublisher,
                        SourcePublicationDate = command.SourcePublicationDate,
                        SourceAccessedAtUtc = now,
                        SourceVersion = string.IsNullOrWhiteSpace(command.SourceVersion) ? null : command.SourceVersion.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportCountryRulePacks.Add(rulePack);

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryRulePackCreated,
                        ExportAuditActions.ExportCountryRulePackCreated,
                        "export.country.rule.pack",
                        rulePack.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            rulePackId = rulePack.Id.ToString(),
                            countryCode = rulePack.CountryCode,
                            versionNumber = rulePack.VersionNumber,
                            name = rulePack.Name,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryRulePackWriteResult.Success(rulePack.Id, rulePack.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "A rule pack version already exists for this country. Please reload and try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The rule pack has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country rule pack service is unavailable.");
        }
    }

    public async Task<ExportCountryComplianceRuleWriteResult> AddRuleAsync(
        ExportOperationContext context,
        AddCountryComplianceRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var ruleCode = (command.RuleCode ?? string.Empty).Trim();
        var title = (command.Title ?? string.Empty).Trim();
        var description = (command.Description ?? string.Empty).Trim();
        var sourceReference = (command.SourceReference ?? string.Empty).Trim();

        if (ruleCode.Length == 0 || title.Length == 0 || description.Length == 0 || sourceReference.Length == 0)
        {
            return ExportCountryComplianceRuleWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Rule code, title, description, and source reference are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var rulePack = await dbContext.ExportCountryRulePacks
                        .SingleOrDefaultAsync(
                            p => p.TenantId == context.TenantId && p.Id == command.RulePackId,
                            ct);

                    if (rulePack is null)
                    {
                        return ExportCountryComplianceRuleWriteResult.Fail(
                            Export3LFailureCodes.RulePackNotFound,
                            "Rule pack was not found.");
                    }

                    if (rulePack.Version != command.ExpectedRulePackVersion)
                    {
                        return ExportCountryComplianceRuleWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The rule pack has been modified. Please reload and try again.");
                    }

                    if (rulePack.Status is not (ExportCountryRulePackStatus.Draft or ExportCountryRulePackStatus.UnderReview))
                    {
                        return ExportCountryComplianceRuleWriteResult.Fail(
                            Export3LFailureCodes.InvalidRulePackStatus,
                            "Rules can only be added to draft or under-review rule packs.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var rule = new ExportCountryComplianceRule
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        RulePackId = rulePack.Id,
                        RuleCode = ruleCode,
                        Title = title,
                        Description = description,
                        RuleType = command.RuleType,
                        Severity = command.Severity,
                        RequirementMode = command.RequirementMode,
                        Blocking = command.Blocking,
                        RequiredDocumentType = string.IsNullOrWhiteSpace(command.RequiredDocumentType)
                            ? null
                            : command.RequiredDocumentType.Trim(),
                        ProductScope = string.IsNullOrWhiteSpace(command.ProductScope) ? null : command.ProductScope.Trim(),
                        ProductGroup = string.IsNullOrWhiteSpace(command.ProductGroup) ? null : command.ProductGroup.Trim(),
                        HsCodePattern = string.IsNullOrWhiteSpace(command.HsCodePattern) ? null : command.HsCodePattern.Trim(),
                        SourceReference = sourceReference,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportCountryComplianceRules.Add(rule);

                    rulePack.Version += 1;
                    rulePack.UpdatedAtUtc = now;
                    rulePack.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryRuleAdded,
                        ExportAuditActions.ExportCountryRuleAdded,
                        "export.country.compliance.rule",
                        rule.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            rulePackId = rulePack.Id.ToString(),
                            ruleId = rule.Id.ToString(),
                            ruleCode = rule.RuleCode,
                            title = rule.Title,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryComplianceRuleWriteResult.Success(rule.Id, rulePack.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportCountryComplianceRuleWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                $"A rule with code '{ruleCode}' already exists in this rule pack.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryComplianceRuleWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The rule pack has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryComplianceRuleWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country rule pack service is unavailable.");
        }
    }

    public async Task<ExportCountryRulePackWriteResult> VerifyRulePackAsync(
        ExportOperationContext context,
        VerifyCountryRulePackCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var rulePack = await dbContext.ExportCountryRulePacks
                        .SingleOrDefaultAsync(
                            p => p.TenantId == context.TenantId && p.Id == command.RulePackId,
                            ct);

                    if (rulePack is null)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.RulePackNotFound,
                            "Rule pack was not found.");
                    }

                    if (rulePack.Version != command.ExpectedVersion)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The rule pack has been modified. Please reload and try again.");
                    }

                    if (rulePack.Status == ExportCountryRulePackStatus.Verified)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.AlreadyVerified,
                            "Rule pack is already verified.");
                    }

                    if (rulePack.Status is not (ExportCountryRulePackStatus.Draft or ExportCountryRulePackStatus.UnderReview))
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.InvalidRulePackStatus,
                            "Only draft or under-review rule packs can be verified.");
                    }

                    var hasRules = await dbContext.ExportCountryComplianceRules
                        .AnyAsync(r => r.TenantId == context.TenantId && r.RulePackId == rulePack.Id, ct);

                    if (!hasRules)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            ExportFailureCodes.ValidationFailed,
                            "Rule pack must contain at least one rule before it can be verified.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    rulePack.Status = ExportCountryRulePackStatus.Verified;
                    rulePack.VerifiedAtUtc = now;
                    rulePack.VerifiedBySubjectId = actorSubjectId;
                    rulePack.Version += 1;
                    rulePack.UpdatedAtUtc = now;
                    rulePack.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryRulePackVerified,
                        ExportAuditActions.ExportCountryRulePackVerified,
                        "export.country.rule.pack",
                        rulePack.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            rulePackId = rulePack.Id.ToString(),
                            countryCode = rulePack.CountryCode,
                            versionNumber = rulePack.VersionNumber,
                        });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportCountryRulePackVerified,
                        "export-country-rule-pack",
                        rulePack.Id.ToString(),
                        correlationId,
                        $"{rulePack.Id}:{rulePack.Version}:{ExportOutboxMessageTypes.ExportCountryRulePackVerified}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["rulePackId"] = rulePack.Id.ToString(),
                            ["countryCode"] = rulePack.CountryCode,
                            ["versionNumber"] = rulePack.VersionNumber.ToString(CultureInfo.InvariantCulture),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryRulePackWriteResult.Success(rulePack.Id, rulePack.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The rule pack has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country rule pack service is unavailable.");
        }
    }

    public async Task<ExportCountryRulePackWriteResult> SupersedeRulePackAsync(
        ExportOperationContext context,
        SupersedeCountryRulePackCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var oldPack = await dbContext.ExportCountryRulePacks
                        .SingleOrDefaultAsync(
                            p => p.TenantId == context.TenantId && p.Id == command.RulePackId,
                            ct);

                    if (oldPack is null)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.RulePackNotFound,
                            "Rule pack was not found.");
                    }

                    if (oldPack.Version != command.ExpectedVersion)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The rule pack has been modified. Please reload and try again.");
                    }

                    if (oldPack.Status != ExportCountryRulePackStatus.Verified)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.InvalidRulePackStatus,
                            "Only verified rule packs can be superseded.");
                    }

                    var newPack = await dbContext.ExportCountryRulePacks
                        .SingleOrDefaultAsync(
                            p => p.TenantId == context.TenantId && p.Id == command.NewRulePackId,
                            ct);

                    if (newPack is null)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.RulePackNotFound,
                            "Replacement rule pack was not found.");
                    }

                    if (newPack.Status != ExportCountryRulePackStatus.Verified)
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            Export3LFailureCodes.InvalidRulePackStatus,
                            "Replacement rule pack must be verified before superseding.");
                    }

                    if (!string.Equals(oldPack.CountryCode, newPack.CountryCode, StringComparison.Ordinal))
                    {
                        return ExportCountryRulePackWriteResult.Fail(
                            ExportFailureCodes.ValidationFailed,
                            "Replacement rule pack must target the same country.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    oldPack.Status = ExportCountryRulePackStatus.Superseded;
                    oldPack.Version += 1;
                    oldPack.UpdatedAtUtc = now;
                    oldPack.LastModifiedBySubjectId = actorSubjectId;

                    newPack.SupersedesRulePackId = oldPack.Id;
                    newPack.Version += 1;
                    newPack.UpdatedAtUtc = now;
                    newPack.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCountryRulePackSuperseded,
                        ExportAuditActions.ExportCountryRulePackSuperseded,
                        "export.country.rule.pack",
                        oldPack.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            rulePackId = oldPack.Id.ToString(),
                            newRulePackId = newPack.Id.ToString(),
                            countryCode = oldPack.CountryCode,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportCountryRulePackWriteResult.Success(oldPack.Id, oldPack.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The rule pack has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCountryRulePackWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export country rule pack service is unavailable.");
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

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

public sealed class UnavailableExportCountryRulePackWriteRepository : IExportCountryRulePackWriteRepository
{
    private static ExportCountryRulePackWriteResult PackUnavailable() =>
        ExportCountryRulePackWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country rule pack persistence is disabled.");

    private static ExportCountryComplianceRuleWriteResult RuleUnavailable() =>
        ExportCountryComplianceRuleWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export country rule pack persistence is disabled.");

    public Task<ExportCountryRulePackWriteResult> CreateRulePackAsync(
        ExportOperationContext context,
        CreateCountryRulePackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PackUnavailable());

    public Task<ExportCountryComplianceRuleWriteResult> AddRuleAsync(
        ExportOperationContext context,
        AddCountryComplianceRuleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RuleUnavailable());

    public Task<ExportCountryRulePackWriteResult> VerifyRulePackAsync(
        ExportOperationContext context,
        VerifyCountryRulePackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PackUnavailable());

    public Task<ExportCountryRulePackWriteResult> SupersedeRulePackAsync(
        ExportOperationContext context,
        SupersedeCountryRulePackCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PackUnavailable());
}
