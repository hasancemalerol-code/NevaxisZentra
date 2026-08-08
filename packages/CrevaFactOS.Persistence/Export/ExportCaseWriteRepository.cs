using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportCaseWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportCaseWriteRepository
{
    private static readonly Regex CountryCodePattern = new("^[A-Z]{2}$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<ExportCaseWriteResult> EnsureExportCaseForOrderAsync(
        ExportOperationContext context,
        EnsureExportCaseForOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (!command.IsExport)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.NotExportOrder,
                "Export cases can only be created for export orders.");
        }

        if (command.SourceOrderId == Guid.Empty)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Source order identifier is required.");
        }

        var destination = NormalizeCountryCode(command.DestinationCountryCode);
        if (destination is null)
        {
            return ExportCaseWriteResult.Fail(
                string.IsNullOrWhiteSpace(command.DestinationCountryCode)
                    ? ExportFailureCodes.DestinationCountryRequired
                    : ExportFailureCodes.InvalidDestinationCountry,
                "Destination country code must be a 2-letter ISO-style uppercase code.");
        }

        string? origin = null;
        if (!string.IsNullOrWhiteSpace(command.OriginCountryCode))
        {
            origin = NormalizeCountryCode(command.OriginCountryCode);
            if (origin is null)
            {
                return ExportCaseWriteResult.Fail(
                    ExportFailureCodes.InvalidOriginCountry,
                    "Origin country code must be a 2-letter ISO-style uppercase code.");
            }
        }

        var sourceOrderReference = (command.SourceOrderReference ?? string.Empty).Trim();
        var customerName = (command.CustomerName ?? string.Empty).Trim();
        if (sourceOrderReference.Length == 0 || customerName.Length == 0)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Source order reference and customer name are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var existing = await FindBySourceOrderAsync(
                        context.TenantId,
                        context.FacilityId,
                        command.SourceOrderId,
                        ct);

                    if (existing is not null)
                    {
                        return ToWriteSuccess(existing, alreadyExisted: true);
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    // Case numbers come from PostgreSQL sequence export_case_number_seq:
                    // SELECT nextval('export_case_number_seq') → EXP-{yyyy}-{number:D6}
                    var nextNumber = await dbContext.Database
                        .SqlQueryRaw<long>("SELECT nextval('export_case_number_seq') AS \"Value\"")
                        .SingleAsync(ct);
                    var caseNumber = $"EXP-{now.UtcDateTime.Year}-{nextNumber:D6}";

                    var exportCase = new ExportCase
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        SourceOrderId = command.SourceOrderId,
                        CaseNumber = caseNumber,
                        SourceOrderReference = sourceOrderReference,
                        CustomerName = customerName,
                        DestinationCountryCode = destination,
                        OriginCountryCode = origin,
                        IncotermCode = string.IsNullOrWhiteSpace(command.IncotermCode)
                            ? null
                            : command.IncotermCode.Trim().ToUpperInvariant(),
                        Status = ExportCaseStatus.Open,
                        ReadinessStatus = ExportReadinessStatus.NotReady,
                        ReadinessPercent = 0,
                        BlockingIssueCount = 0,
                        MissingRequirementCount = 0,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportCases.Add(exportCase);

                    AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.ExportCaseCreated,
                        "Export case created",
                        $"Case {caseNumber} linked to order {sourceOrderReference}.",
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.case",
                        exportCase.Id);

                    AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.DestinationCountrySet,
                        "Destination country set",
                        destination,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.case",
                        exportCase.Id);

                    AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCaseCreated,
                        ExportAuditActions.ExportCaseCreated,
                        "export.case",
                        exportCase.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = exportCase.Id.ToString(),
                            caseNumber,
                            sourceOrderId = command.SourceOrderId.ToString(),
                            destinationCountryCode = destination,
                        });

                    AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportCaseCreated,
                        "export-case",
                        exportCase.Id.ToString(),
                        correlationId,
                        $"{exportCase.Id}:1:{ExportOutboxMessageTypes.ExportCaseCreated}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["exportCaseId"] = exportCase.Id.ToString(),
                            ["caseNumber"] = caseNumber,
                            ["sourceOrderId"] = command.SourceOrderId.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await RecalculateAndPersistReadinessAsync(
                        context,
                        exportCase,
                        documents: [],
                        correlationId,
                        actorSubjectId,
                        now,
                        emitInitial: true,
                        ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ToWriteSuccess(exportCase, alreadyExisted: false);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            try
            {
                return await tenantDatabaseSession.ExecuteReadAsync(
                    async ct =>
                    {
                        var existing = await FindBySourceOrderAsync(
                            context.TenantId,
                            context.FacilityId,
                            command.SourceOrderId,
                            ct);

                        return existing is null
                            ? ExportCaseWriteResult.Fail(
                                ExportFailureCodes.ConcurrencyConflict,
                                "Export case could not be created due to a concurrent write.")
                            : ToWriteSuccess(existing, alreadyExisted: true);
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ExportCaseWriteResult.Fail(
                    ExportFailureCodes.SourceUnavailable,
                    "Export case service is unavailable.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export case service is unavailable.");
        }
    }

    public async Task<ExportCaseWriteResult> UpdateExportCaseAsync(
        ExportOperationContext context,
        UpdateExportCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseForUpdateAsync(
                        context.TenantId,
                        context.FacilityId,
                        command.ExportCaseId,
                        ct);

                    if (exportCase is null)
                    {
                        return ExportCaseWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    if (exportCase.Version != command.ExpectedVersion)
                    {
                        return ExportCaseWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export case has been modified. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();
                    var previousDestination = exportCase.DestinationCountryCode;
                    var changed = false;

                    if (command.DestinationCountryCode is not null)
                    {
                        var destination = NormalizeCountryCode(command.DestinationCountryCode);
                        if (destination is null)
                        {
                            return ExportCaseWriteResult.Fail(
                                ExportFailureCodes.InvalidDestinationCountry,
                                "Destination country code must be a 2-letter ISO-style uppercase code.");
                        }

                        if (!string.Equals(exportCase.DestinationCountryCode, destination, StringComparison.Ordinal))
                        {
                            exportCase.DestinationCountryCode = destination;
                            changed = true;
                        }
                    }

                    if (command.OriginCountryCode is not null)
                    {
                        if (command.OriginCountryCode.Length == 0)
                        {
                            if (exportCase.OriginCountryCode is not null)
                            {
                                exportCase.OriginCountryCode = null;
                                changed = true;
                            }
                        }
                        else
                        {
                            var origin = NormalizeCountryCode(command.OriginCountryCode);
                            if (origin is null)
                            {
                                return ExportCaseWriteResult.Fail(
                                    ExportFailureCodes.InvalidOriginCountry,
                                    "Origin country code must be a 2-letter ISO-style uppercase code.");
                            }

                            if (!string.Equals(exportCase.OriginCountryCode, origin, StringComparison.Ordinal))
                            {
                                exportCase.OriginCountryCode = origin;
                                changed = true;
                            }
                        }
                    }

                    if (command.IncotermCode is not null)
                    {
                        var incoterm = string.IsNullOrWhiteSpace(command.IncotermCode)
                            ? null
                            : command.IncotermCode.Trim().ToUpperInvariant();
                        if (!string.Equals(exportCase.IncotermCode, incoterm, StringComparison.Ordinal))
                        {
                            exportCase.IncotermCode = incoterm;
                            changed = true;
                        }
                    }

                    if (command.Status is { } status)
                    {
                        if (exportCase.Status != status)
                        {
                            exportCase.Status = status;
                            changed = true;
                        }
                    }

                    if (!changed)
                    {
                        return ToWriteSuccess(exportCase, alreadyExisted: false);
                    }

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    if (!string.Equals(previousDestination, exportCase.DestinationCountryCode, StringComparison.Ordinal))
                    {
                        AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.DestinationCountrySet,
                            "Destination country set",
                            exportCase.DestinationCountryCode,
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.case",
                            exportCase.Id);
                    }

                    AddAudit(
                        context,
                        ExportAuditEventTypes.ExportCaseUpdated,
                        ExportAuditActions.ExportCaseUpdated,
                        "export.case",
                        exportCase.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = exportCase.Id.ToString(),
                            caseNumber = exportCase.CaseNumber,
                            destinationCountryCode = exportCase.DestinationCountryCode,
                            originCountryCode = exportCase.OriginCountryCode,
                            incotermCode = exportCase.IncotermCode,
                            status = ExportValueConversions.ToStorage(exportCase.Status),
                            version = exportCase.Version,
                        });

                    var documents = await LoadDocumentsAsync(
                        context.TenantId,
                        context.FacilityId,
                        exportCase.Id,
                        ct);

                    await RecalculateAndPersistReadinessAsync(
                        context,
                        exportCase,
                        documents,
                        correlationId,
                        actorSubjectId,
                        now,
                        emitInitial: false,
                        ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ToWriteSuccess(exportCase, alreadyExisted: false);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportCaseWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export case service is unavailable.");
        }
    }

    public async Task<ExportDocumentWriteResult> RegisterDocumentAsync(
        ExportOperationContext context,
        RegisterExportDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (!ExportDocumentType.IsKnown(command.DocumentType))
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidDocumentType,
                "Document type is not recognized.");
        }

        var initialStatus = command.InitialStatus;
        if (initialStatus is ExportDocumentStatus.Missing or ExportDocumentStatus.Verified)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidDocumentStatus,
                "Documents must be registered as draft or received.");
        }

        var displayName = (command.DisplayName ?? string.Empty).Trim();
        if (displayName.Length == 0)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Display name is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseForUpdateAsync(
                        context.TenantId,
                        context.FacilityId,
                        command.ExportCaseId,
                        ct);

                    if (exportCase is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    if (exportCase.Status == ExportCaseStatus.Closed)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.CaseClosed,
                            "Documents cannot be registered on a closed export case.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();
                    var documentType = command.DocumentType.Trim();

                    var documents = await LoadDocumentsAsync(
                        context.TenantId,
                        context.FacilityId,
                        exportCase.Id,
                        ct);

                    var document = new ExportDocument
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = exportCase.Id,
                        DocumentType = documentType,
                        DocumentStatus = initialStatus,
                        DisplayName = displayName,
                        FileReference = NullIfWhiteSpace(command.FileReference),
                        DocumentNumber = NullIfWhiteSpace(command.DocumentNumber),
                        IssuedAtUtc = command.IssuedAtUtc,
                        ExpiresAtUtc = command.ExpiresAtUtc,
                        Source = string.IsNullOrWhiteSpace(command.Source) ? "manual" : command.Source.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportDocuments.Add(document);
                    documents.Add(document);

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.DocumentRegistered,
                        "Document registered",
                        $"{documentType}: {displayName}",
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.document",
                        document.Id);

                    AddAudit(
                        context,
                        ExportAuditEventTypes.ExportDocumentRegistered,
                        ExportAuditActions.ExportDocumentRegistered,
                        "export.document",
                        document.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = exportCase.Id.ToString(),
                            documentId = document.Id.ToString(),
                            documentType,
                            documentStatus = ExportValueConversions.ToStorage(document.DocumentStatus),
                        });

                    AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportDocumentRegistered,
                        "export-document",
                        document.Id.ToString(),
                        correlationId,
                        $"{document.Id}:1:{ExportOutboxMessageTypes.ExportDocumentRegistered}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["exportCaseId"] = exportCase.Id.ToString(),
                            ["documentId"] = document.Id.ToString(),
                            ["documentType"] = documentType,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await RecalculateAndPersistReadinessAsync(
                        context,
                        exportCase,
                        documents,
                        correlationId,
                        actorSubjectId,
                        now,
                        emitInitial: false,
                        ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportDocumentWriteResult.Success(document.Id, document.Version, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export document service is unavailable.");
        }
    }

    public async Task<ExportDocumentWriteResult> UpdateDocumentAsync(
        ExportOperationContext context,
        UpdateExportDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (command.DocumentStatus is ExportDocumentStatus.Missing or ExportDocumentStatus.Verified)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidDocumentStatus,
                "Use verify/reject endpoints for verified/rejected transitions; missing cannot be persisted.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseForUpdateAsync(
                        context.TenantId,
                        context.FacilityId,
                        command.ExportCaseId,
                        ct);

                    if (exportCase is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    var document = await dbContext.ExportDocuments
                        .SingleOrDefaultAsync(
                            d =>
                                d.TenantId == context.TenantId
                                && d.FacilityId == context.FacilityId
                                && d.ExportCaseId == command.ExportCaseId
                                && d.Id == command.DocumentId,
                            ct);

                    if (document is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.DocumentNotFound,
                            "Export document was not found.");
                    }

                    if (document.Version != command.ExpectedVersion)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export document has been modified. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();
                    var changed = false;

                    if (command.DisplayName is not null)
                    {
                        var displayName = command.DisplayName.Trim();
                        if (displayName.Length == 0)
                        {
                            return ExportDocumentWriteResult.Fail(
                                ExportFailureCodes.ValidationFailed,
                                "Display name cannot be empty.");
                        }

                        if (!string.Equals(document.DisplayName, displayName, StringComparison.Ordinal))
                        {
                            document.DisplayName = displayName;
                            changed = true;
                        }
                    }

                    if (command.FileReference is not null)
                    {
                        var fileRef = NullIfWhiteSpace(command.FileReference);
                        if (!string.Equals(document.FileReference, fileRef, StringComparison.Ordinal))
                        {
                            document.FileReference = fileRef;
                            changed = true;
                        }
                    }

                    if (command.DocumentNumber is not null)
                    {
                        var docNumber = NullIfWhiteSpace(command.DocumentNumber);
                        if (!string.Equals(document.DocumentNumber, docNumber, StringComparison.Ordinal))
                        {
                            document.DocumentNumber = docNumber;
                            changed = true;
                        }
                    }

                    if (command.IssuedAtUtc.HasValue && document.IssuedAtUtc != command.IssuedAtUtc)
                    {
                        document.IssuedAtUtc = command.IssuedAtUtc;
                        changed = true;
                    }

                    if (command.ExpiresAtUtc.HasValue && document.ExpiresAtUtc != command.ExpiresAtUtc)
                    {
                        document.ExpiresAtUtc = command.ExpiresAtUtc;
                        changed = true;
                    }

                    if (command.DocumentStatus is { } status
                        && status is ExportDocumentStatus.Draft or ExportDocumentStatus.Received
                        && document.DocumentStatus != status)
                    {
                        document.DocumentStatus = status;
                        document.RejectionReason = null;
                        document.VerifiedAtUtc = null;
                        document.VerifiedBySubjectId = null;
                        changed = true;
                    }

                    if (!changed)
                    {
                        return ExportDocumentWriteResult.Success(document.Id, document.Version, exportCase.Version);
                    }

                    document.Version += 1;
                    document.UpdatedAtUtc = now;
                    document.LastModifiedBySubjectId = actorSubjectId;

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var documents = await LoadDocumentsAsync(
                        context.TenantId,
                        context.FacilityId,
                        exportCase.Id,
                        ct);

                    await RecalculateAndPersistReadinessAsync(
                        context,
                        exportCase,
                        documents,
                        correlationId,
                        actorSubjectId,
                        now,
                        emitInitial: false,
                        ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportDocumentWriteResult.Success(document.Id, document.Version, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export document has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export document service is unavailable.");
        }
    }

    public async Task<ExportDocumentWriteResult> VerifyDocumentAsync(
        ExportOperationContext context,
        VerifyExportDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseForUpdateAsync(
                        context.TenantId,
                        context.FacilityId,
                        command.ExportCaseId,
                        ct);

                    if (exportCase is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    var document = await dbContext.ExportDocuments
                        .SingleOrDefaultAsync(
                            d =>
                                d.TenantId == context.TenantId
                                && d.FacilityId == context.FacilityId
                                && d.ExportCaseId == command.ExportCaseId
                                && d.Id == command.DocumentId,
                            ct);

                    if (document is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.DocumentNotFound,
                            "Export document was not found.");
                    }

                    if (document.Version != command.ExpectedVersion)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export document has been modified. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    document.DocumentStatus = ExportDocumentStatus.Verified;
                    document.VerifiedAtUtc = now;
                    document.VerifiedBySubjectId = actorSubjectId;
                    document.RejectionReason = null;
                    document.Version += 1;
                    document.UpdatedAtUtc = now;
                    document.LastModifiedBySubjectId = actorSubjectId;

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.DocumentVerified,
                        "Document verified",
                        document.DisplayName,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.document",
                        document.Id);

                    AddAudit(
                        context,
                        ExportAuditEventTypes.ExportDocumentVerified,
                        ExportAuditActions.ExportDocumentVerified,
                        "export.document",
                        document.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = exportCase.Id.ToString(),
                            documentId = document.Id.ToString(),
                            documentType = document.DocumentType,
                            verifiedBySubjectId = actorSubjectId,
                        });

                    AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportDocumentVerified,
                        "export-document",
                        document.Id.ToString(),
                        correlationId,
                        $"{document.Id}:{document.Version}:{ExportOutboxMessageTypes.ExportDocumentVerified}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["exportCaseId"] = exportCase.Id.ToString(),
                            ["documentId"] = document.Id.ToString(),
                            ["documentType"] = document.DocumentType,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    var documents = await LoadDocumentsAsync(
                        context.TenantId,
                        context.FacilityId,
                        exportCase.Id,
                        ct);

                    await RecalculateAndPersistReadinessAsync(
                        context,
                        exportCase,
                        documents,
                        correlationId,
                        actorSubjectId,
                        now,
                        emitInitial: false,
                        ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportDocumentWriteResult.Success(document.Id, document.Version, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export document has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export document service is unavailable.");
        }
    }

    public async Task<ExportDocumentWriteResult> RejectDocumentAsync(
        ExportOperationContext context,
        RejectExportDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        var reason = (command.Reason ?? string.Empty).Trim();
        if (reason.Length == 0)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.RejectionReasonRequired,
                "A rejection reason is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseForUpdateAsync(
                        context.TenantId,
                        context.FacilityId,
                        command.ExportCaseId,
                        ct);

                    if (exportCase is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    var document = await dbContext.ExportDocuments
                        .SingleOrDefaultAsync(
                            d =>
                                d.TenantId == context.TenantId
                                && d.FacilityId == context.FacilityId
                                && d.ExportCaseId == command.ExportCaseId
                                && d.Id == command.DocumentId,
                            ct);

                    if (document is null)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.DocumentNotFound,
                            "Export document was not found.");
                    }

                    if (document.Version != command.ExpectedVersion)
                    {
                        return ExportDocumentWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export document has been modified. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    document.DocumentStatus = ExportDocumentStatus.Rejected;
                    document.RejectionReason = reason;
                    document.VerifiedAtUtc = null;
                    document.VerifiedBySubjectId = null;
                    document.Version += 1;
                    document.UpdatedAtUtc = now;
                    document.LastModifiedBySubjectId = actorSubjectId;

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.DocumentRejected,
                        "Document rejected",
                        reason,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.document",
                        document.Id);

                    AddAudit(
                        context,
                        ExportAuditEventTypes.ExportDocumentRejected,
                        ExportAuditActions.ExportDocumentRejected,
                        "export.document",
                        document.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = exportCase.Id.ToString(),
                            documentId = document.Id.ToString(),
                            documentType = document.DocumentType,
                            reason,
                        });

                    var documents = await LoadDocumentsAsync(
                        context.TenantId,
                        context.FacilityId,
                        exportCase.Id,
                        ct);

                    await RecalculateAndPersistReadinessAsync(
                        context,
                        exportCase,
                        documents,
                        correlationId,
                        actorSubjectId,
                        now,
                        emitInitial: false,
                        ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportDocumentWriteResult.Success(document.Id, document.Version, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export document has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportDocumentWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export document service is unavailable.");
        }
    }

    private async Task RecalculateAndPersistReadinessAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        IReadOnlyList<ExportDocument> documents,
        Guid correlationId,
        string actorSubjectId,
        DateTimeOffset now,
        bool emitInitial,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var previousStatus = exportCase.ReadinessStatus;
        var previousPercent = exportCase.ReadinessPercent;
        var previousBlocking = exportCase.BlockingIssueCount;
        var previousMissing = exportCase.MissingRequirementCount;

        var calc = ExportReadinessEngine.Calculate(exportCase, documents);

        exportCase.ReadinessStatus = calc.ReadinessStatus;
        exportCase.ReadinessPercent = calc.ReadinessPercent;
        exportCase.BlockingIssueCount = calc.BlockingIssueCount;
        exportCase.MissingRequirementCount = calc.MissingRequirementCount;

        if (exportCase.Status != ExportCaseStatus.Closed)
        {
            exportCase.Status = calc.SuggestedCaseStatus;
        }

        var readinessChanged =
            emitInitial
            || previousStatus != exportCase.ReadinessStatus
            || previousPercent != exportCase.ReadinessPercent
            || previousBlocking != exportCase.BlockingIssueCount
            || previousMissing != exportCase.MissingRequirementCount;

        if (!readinessChanged)
        {
            return;
        }

        AppendActivity(
            exportCase,
            ExportActivityEventTypes.ReadinessChanged,
            "Readiness changed",
            $"{ExportValueConversions.ToStorage(exportCase.ReadinessStatus)} ({exportCase.ReadinessPercent}%)",
            now,
            ExportActivitySourceType.System,
            actorSubjectId,
            correlationId,
            "export.case",
            exportCase.Id);

        AddAudit(
            context,
            ExportAuditEventTypes.ExportReadinessChanged,
            ExportAuditActions.ExportReadinessChanged,
            "export.case",
            exportCase.Id.ToString(),
            correlationId,
            now,
            new
            {
                exportCaseId = exportCase.Id.ToString(),
                readinessStatus = ExportValueConversions.ToStorage(exportCase.ReadinessStatus),
                readinessPercent = exportCase.ReadinessPercent,
                blockingIssueCount = exportCase.BlockingIssueCount,
                missingRequirementCount = exportCase.MissingRequirementCount,
                previousReadinessStatus = ExportValueConversions.ToStorage(previousStatus),
            });

        AddOutbox(
            context.TenantId,
            ExportOutboxMessageTypes.ExportReadinessChanged,
            "export-case",
            exportCase.Id.ToString(),
            correlationId,
            $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportReadinessChanged}:{exportCase.ReadinessPercent}",
            now,
            new Dictionary<string, string?>
            {
                ["tenantId"] = context.TenantId.ToString(),
                ["facilityId"] = context.FacilityId.ToString(),
                ["exportCaseId"] = exportCase.Id.ToString(),
                ["readinessStatus"] = ExportValueConversions.ToStorage(exportCase.ReadinessStatus),
                ["readinessPercent"] = exportCase.ReadinessPercent.ToString(CultureInfo.InvariantCulture),
                ["occurredAtUtc"] = now.ToString("O"),
            });

        if (exportCase.ReadinessStatus == ExportReadinessStatus.Ready
            && (emitInitial || previousStatus != ExportReadinessStatus.Ready))
        {
            AppendActivity(
                exportCase,
                ExportActivityEventTypes.ExportReady,
                "Export ready",
                "All readiness requirements are satisfied.",
                now,
                ExportActivitySourceType.System,
                actorSubjectId,
                correlationId,
                "export.case",
                exportCase.Id);

            AddOutbox(
                context.TenantId,
                ExportOutboxMessageTypes.ExportReady,
                "export-case",
                exportCase.Id.ToString(),
                correlationId,
                $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportReady}",
                now,
                new Dictionary<string, string?>
                {
                    ["tenantId"] = context.TenantId.ToString(),
                    ["facilityId"] = context.FacilityId.ToString(),
                    ["exportCaseId"] = exportCase.Id.ToString(),
                    ["occurredAtUtc"] = now.ToString("O"),
                });
        }
        else if (exportCase.ReadinessStatus == ExportReadinessStatus.NotReady
                 && (emitInitial || previousStatus != ExportReadinessStatus.NotReady))
        {
            AppendActivity(
                exportCase,
                ExportActivityEventTypes.ExportBlocked,
                "Export blocked",
                $"{exportCase.BlockingIssueCount} blocking issue(s).",
                now,
                ExportActivitySourceType.System,
                actorSubjectId,
                correlationId,
                "export.case",
                exportCase.Id);

            AddOutbox(
                context.TenantId,
                ExportOutboxMessageTypes.ExportBlocked,
                "export-case",
                exportCase.Id.ToString(),
                correlationId,
                $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportBlocked}",
                now,
                new Dictionary<string, string?>
                {
                    ["tenantId"] = context.TenantId.ToString(),
                    ["facilityId"] = context.FacilityId.ToString(),
                    ["exportCaseId"] = exportCase.Id.ToString(),
                    ["blockingIssueCount"] = exportCase.BlockingIssueCount.ToString(CultureInfo.InvariantCulture),
                    ["occurredAtUtc"] = now.ToString("O"),
                });
        }
    }

    private async Task<ExportCase?> FindBySourceOrderAsync(
        Guid tenantId,
        Guid facilityId,
        Guid sourceOrderId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCases
            .AsNoTracking()
            .SingleOrDefaultAsync(
                c =>
                    c.TenantId == tenantId
                    && c.FacilityId == facilityId
                    && c.SourceOrderId == sourceOrderId,
                cancellationToken);

    private async Task<ExportCase?> LoadCaseForUpdateAsync(
        Guid tenantId,
        Guid facilityId,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCases
            .SingleOrDefaultAsync(
                c =>
                    c.TenantId == tenantId
                    && c.FacilityId == facilityId
                    && c.Id == exportCaseId,
                cancellationToken);

    private async Task<List<ExportDocument>> LoadDocumentsAsync(
        Guid tenantId,
        Guid facilityId,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportDocuments
            .Where(d =>
                d.TenantId == tenantId
                && d.FacilityId == facilityId
                && d.ExportCaseId == exportCaseId)
            .ToListAsync(cancellationToken);

    private void AppendActivity(
        ExportCase exportCase,
        string eventType,
        string title,
        string? summary,
        DateTimeOffset occurredAtUtc,
        ExportActivitySourceType sourceType,
        string? actorSubjectId,
        Guid correlationId,
        string? relatedEntityType,
        Guid? relatedEntityId)
    {
        dbContext.ExportCaseActivities.Add(new ExportCaseActivity
        {
            Id = Guid.NewGuid(),
            TenantId = exportCase.TenantId,
            FacilityId = exportCase.FacilityId,
            ExportCaseId = exportCase.Id,
            EventType = eventType,
            Title = title,
            Summary = summary,
            OccurredAtUtc = occurredAtUtc,
            SourceType = sourceType,
            ActorSubjectId = actorSubjectId,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CorrelationId = correlationId,
        });
    }

    private void AddAudit(
        ExportOperationContext context,
        string eventType,
        string action,
        string resourceType,
        string resourceId,
        Guid correlationId,
        DateTimeOffset now,
        object metadata)
    {
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            EventType = eventType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = correlationId,
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions),
        });
    }

    private void AddOutbox(
        Guid tenantId,
        string messageType,
        string aggregateType,
        string aggregateId,
        Guid correlationId,
        string idempotencyKey,
        DateTimeOffset now,
        Dictionary<string, string?> payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            CorrelationId = correlationId,
            CausationId = null,
            IdempotencyKey = idempotencyKey,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    private static ExportCaseWriteResult ToWriteSuccess(ExportCase exportCase, bool alreadyExisted) =>
        ExportCaseWriteResult.Success(
            exportCase.Id,
            exportCase.CaseNumber,
            ExportValueConversions.ToStorage(exportCase.Status),
            ExportValueConversions.ToStorage(exportCase.ReadinessStatus),
            exportCase.ReadinessPercent,
            exportCase.Version,
            exportCase.CreatedAtUtc,
            exportCase.UpdatedAtUtc,
            alreadyExisted);

    private static string? NormalizeCountryCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return CountryCodePattern.IsMatch(normalized) ? normalized : null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
        || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("uix_export_cases_tenant_facility_source_order", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class UnavailableExportCaseWriteRepository : IExportCaseWriteRepository
{
    private static readonly ExportCaseWriteResult UnavailableCase =
        ExportCaseWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    private static readonly ExportDocumentWriteResult UnavailableDocument =
        ExportDocumentWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export persistence is disabled.");

    public Task<ExportCaseWriteResult> EnsureExportCaseForOrderAsync(
        ExportOperationContext context,
        EnsureExportCaseForOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableCase);

    public Task<ExportCaseWriteResult> UpdateExportCaseAsync(
        ExportOperationContext context,
        UpdateExportCaseCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableCase);

    public Task<ExportDocumentWriteResult> RegisterDocumentAsync(
        ExportOperationContext context,
        RegisterExportDocumentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDocument);

    public Task<ExportDocumentWriteResult> UpdateDocumentAsync(
        ExportOperationContext context,
        UpdateExportDocumentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDocument);

    public Task<ExportDocumentWriteResult> VerifyDocumentAsync(
        ExportOperationContext context,
        VerifyExportDocumentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDocument);

    public Task<ExportDocumentWriteResult> RejectDocumentAsync(
        ExportOperationContext context,
        RejectExportDocumentCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableDocument);
}
