using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportBrokerWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportBrokerWriteRepository
{
    public async Task<ExportBrokerWorkspaceWriteResult> CreateWorkspaceAsync(
        ExportOperationContext context,
        CreateExportBrokerWorkspaceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var brokerName = (command.BrokerCompanyName ?? string.Empty).Trim();
        if (brokerName.Length == 0)
        {
            return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Broker company name is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    var activeWorkspaceExists = await dbContext.ExportBrokerWorkspaces
                        .AnyAsync(
                            w => w.TenantId == context.TenantId
                                && w.FacilityId == context.FacilityId
                                && w.ExportCaseId == command.ExportCaseId
                                && w.Status != ExportBrokerWorkspaceStatus.Cancelled
                                && w.Status != ExportBrokerWorkspaceStatus.Closed,
                            ct);

                    if (activeWorkspaceExists)
                    {
                        return ExportBrokerWorkspaceWriteResult.Fail(
                            Export2LFailureCodes.ActiveWorkspaceExists,
                            "An active broker workspace already exists for this export case.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var workspace = new ExportBrokerWorkspace
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = command.ExportCaseId,
                        ShipmentId = command.ShipmentId,
                        BrokerCompanyName = brokerName,
                        BrokerContactName = string.IsNullOrWhiteSpace(command.BrokerContactName) ? null : command.BrokerContactName.Trim(),
                        BrokerEmail = string.IsNullOrWhiteSpace(command.BrokerEmail) ? null : command.BrokerEmail.Trim(),
                        BrokerPhone = string.IsNullOrWhiteSpace(command.BrokerPhone) ? null : command.BrokerPhone.Trim(),
                        Status = ExportBrokerWorkspaceStatus.Draft,
                        Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportBrokerWorkspaces.Add(workspace);

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.BrokerWorkspaceCreated,
                        "Broker workspace created",
                        brokerName,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.broker.workspace",
                        workspace.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportBrokerWorkspaceCreated,
                        ExportAuditActions.ExportBrokerWorkspaceCreated,
                        "export.broker.workspace",
                        workspace.Id.ToString(),
                        correlationId,
                        now,
                        new { exportCaseId = command.ExportCaseId.ToString(), workspaceId = workspace.Id.ToString(), brokerName });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportBrokerWorkspaceWriteResult.Success(
                        workspace.Id, workspace.Version, ExportValueConversions.ToStorage(workspace.Status));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }

    public async Task<ExportBrokerPackageWriteResult> PreparePackageAsync(
        ExportOperationContext context,
        PrepareExportBrokerPackageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var workspace = await LoadWorkspaceAsync(context, command.ExportCaseId, command.BrokerWorkspaceId, ct);
                    if (workspace is null)
                    {
                        return ExportBrokerPackageWriteResult.Fail(Export2LFailureCodes.BrokerWorkspaceNotFound, "Broker workspace was not found.");
                    }

                    if (workspace.Version != command.ExpectedWorkspaceVersion)
                    {
                        return ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                            "The broker workspace has been modified. Please reload and try again.");
                    }

                    if (workspace.Status is ExportBrokerWorkspaceStatus.Closed or ExportBrokerWorkspaceStatus.Cancelled)
                    {
                        return ExportBrokerPackageWriteResult.Fail(Export2LFailureCodes.InvalidBrokerWorkspaceStatus,
                            "Cannot prepare a package for a closed or cancelled broker workspace.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    // Mark any existing draft package versions as stale
                    var existingDraftVersions = await dbContext.ExportBrokerPackageVersions
                        .Where(v => v.TenantId == context.TenantId
                            && v.BrokerWorkspaceId == workspace.Id
                            && v.Status == ExportBrokerPackageVersionStatus.Draft)
                        .ToListAsync(ct);

                    foreach (var stale in existingDraftVersions)
                    {
                        stale.Status = ExportBrokerPackageVersionStatus.Stale;
                    }

                    // Calculate next version number
                    var maxVersionNumber = await dbContext.ExportBrokerPackageVersions
                        .Where(v => v.TenantId == context.TenantId && v.BrokerWorkspaceId == workspace.Id)
                        .Select(v => (int?)v.VersionNumber)
                        .MaxAsync(ct) ?? 0;

                    var nextVersionNumber = maxVersionNumber + 1;

                    var packageVersion = new ExportBrokerPackageVersion
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = command.ExportCaseId,
                        BrokerWorkspaceId = workspace.Id,
                        VersionNumber = nextVersionNumber,
                        Status = ExportBrokerPackageVersionStatus.Draft,
                        Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                    };

                    dbContext.ExportBrokerPackageVersions.Add(packageVersion);

                    // Snapshot requested documents — never include rejected documents.
                    if (command.DocumentIds.Count > 0)
                    {
                        var documents = await dbContext.ExportDocuments
                            .AsNoTracking()
                            .Where(d => d.TenantId == context.TenantId
                                && d.ExportCaseId == command.ExportCaseId
                                && command.DocumentIds.Contains(d.Id))
                            .ToListAsync(ct);

                        if (documents.Any(d => d.DocumentStatus == ExportDocumentStatus.Rejected))
                        {
                            return ExportBrokerPackageWriteResult.Fail(
                                Export2LFailureCodes.RejectedDocumentNotAllowed,
                                "Rejected documents cannot be included in a broker package.");
                        }

                        foreach (var doc in documents)
                        {
                            dbContext.ExportBrokerPackageDocuments.Add(new ExportBrokerPackageDocument
                            {
                                Id = Guid.NewGuid(),
                                PackageVersionId = packageVersion.Id,
                                DocumentId = doc.Id,
                                DocumentType = doc.DocumentType,
                                DocumentStatusSnapshot = ExportValueConversions.ToStorage(doc.DocumentStatus),
                                DisplayNameSnapshot = doc.DisplayName ?? doc.DocumentType,
                                Included = true,
                            });
                        }
                    }

                    workspace.Status = ExportBrokerWorkspaceStatus.PackageReady;
                    workspace.PreparedAtUtc = now;
                    workspace.PreparedBySubjectId = actorSubjectId;
                    workspace.Version += 1;
                    workspace.UpdatedAtUtc = now;
                    workspace.LastModifiedBySubjectId = actorSubjectId;

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.BrokerPackagePrepared,
                            "Broker package prepared",
                            $"v{nextVersionNumber}, {command.DocumentIds.Count} document(s)",
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.broker.workspace",
                            workspace.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportBrokerPackagePrepared,
                            ExportAuditActions.ExportBrokerPackagePrepared,
                            "export.broker.workspace",
                            workspace.Id.ToString(),
                            correlationId,
                            now,
                            new
                            {
                                exportCaseId = command.ExportCaseId.ToString(),
                                workspaceId = workspace.Id.ToString(),
                                packageVersionId = packageVersion.Id.ToString(),
                                versionNumber = nextVersionNumber,
                                documentCount = command.DocumentIds.Count,
                            });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportBrokerPackageWriteResult.Success(packageVersion.Id, nextVersionNumber, workspace.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The broker workspace has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }

    public async Task<ExportBrokerPackageWriteResult> SendPackageAsync(
        ExportOperationContext context,
        SendExportBrokerPackageCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var workspace = await LoadWorkspaceAsync(context, command.ExportCaseId, command.BrokerWorkspaceId, ct);
                    if (workspace is null)
                    {
                        return ExportBrokerPackageWriteResult.Fail(Export2LFailureCodes.BrokerWorkspaceNotFound, "Broker workspace was not found.");
                    }

                    if (workspace.Version != command.ExpectedWorkspaceVersion)
                    {
                        return ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                            "The broker workspace has been modified. Please reload and try again.");
                    }

                    if (workspace.Status != ExportBrokerWorkspaceStatus.PackageReady)
                    {
                        return ExportBrokerPackageWriteResult.Fail(Export2LFailureCodes.InvalidBrokerWorkspaceStatus,
                            "Workspace must be in PackageReady status to send a package.");
                    }

                    var draftVersion = await dbContext.ExportBrokerPackageVersions
                        .SingleOrDefaultAsync(
                            v => v.TenantId == context.TenantId
                                && v.BrokerWorkspaceId == workspace.Id
                                && v.Status == ExportBrokerPackageVersionStatus.Draft,
                            ct);

                    if (draftVersion is null)
                    {
                        return ExportBrokerPackageWriteResult.Fail(Export2LFailureCodes.NoDraftPackageVersion,
                            "No draft package version found. Please prepare a package first.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    draftVersion.Status = ExportBrokerPackageVersionStatus.Sent;
                    draftVersion.SentAtUtc = now;
                    draftVersion.SentBySubjectId = actorSubjectId;

                    workspace.Status = ExportBrokerWorkspaceStatus.Sent;
                    workspace.SentAtUtc = now;
                    workspace.SentBySubjectId = actorSubjectId;
                    workspace.Version += 1;
                    workspace.UpdatedAtUtc = now;
                    workspace.LastModifiedBySubjectId = actorSubjectId;

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.BrokerPackageSent,
                            "Broker package sent",
                            $"v{draftVersion.VersionNumber} to {workspace.BrokerCompanyName}",
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.broker.workspace",
                            workspace.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportBrokerPackageSent,
                            ExportAuditActions.ExportBrokerPackageSent,
                            "export.broker.workspace",
                            workspace.Id.ToString(),
                            correlationId,
                            now,
                            new
                            {
                                exportCaseId = command.ExportCaseId.ToString(),
                                workspaceId = workspace.Id.ToString(),
                                packageVersionId = draftVersion.Id.ToString(),
                                versionNumber = draftVersion.VersionNumber,
                            });

                        helper.AddOutbox(
                            context.TenantId,
                            ExportOutboxMessageTypes.ExportBrokerPackageSent,
                            "export-broker-workspace",
                            workspace.Id.ToString(),
                            correlationId,
                            $"{workspace.Id}:{workspace.Version}:{ExportOutboxMessageTypes.ExportBrokerPackageSent}",
                            now,
                            new Dictionary<string, string?>
                            {
                                ["tenantId"] = context.TenantId.ToString(),
                                ["facilityId"] = context.FacilityId.ToString(),
                                ["exportCaseId"] = command.ExportCaseId.ToString(),
                                ["workspaceId"] = workspace.Id.ToString(),
                                ["occurredAtUtc"] = now.ToString("O"),
                            });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportBrokerPackageWriteResult.Success(draftVersion.Id, draftVersion.VersionNumber, workspace.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The broker workspace has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }

    public async Task<ExportBrokerWorkspaceWriteResult> AcknowledgeAsync(
        ExportOperationContext context,
        AcknowledgeExportBrokerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var workspace = await LoadWorkspaceAsync(context, command.ExportCaseId, command.BrokerWorkspaceId, ct);
                    if (workspace is null)
                    {
                        return ExportBrokerWorkspaceWriteResult.Fail(Export2LFailureCodes.BrokerWorkspaceNotFound, "Broker workspace was not found.");
                    }

                    if (workspace.Version != command.ExpectedWorkspaceVersion)
                    {
                        return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                            "The broker workspace has been modified. Please reload and try again.");
                    }

                    if (workspace.Status != ExportBrokerWorkspaceStatus.Sent)
                    {
                        return ExportBrokerWorkspaceWriteResult.Fail(Export2LFailureCodes.InvalidBrokerWorkspaceStatus,
                            "Workspace must be in Sent status to acknowledge.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    workspace.Status = ExportBrokerWorkspaceStatus.Acknowledged;
                    workspace.AcknowledgedAtUtc = now;
                    workspace.BrokerReference = string.IsNullOrWhiteSpace(command.BrokerReference) ? null : command.BrokerReference.Trim();
                    workspace.Version += 1;
                    workspace.UpdatedAtUtc = now;
                    workspace.LastModifiedBySubjectId = actorSubjectId;

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.BrokerWorkspaceAcknowledged,
                            "Broker acknowledged",
                            workspace.BrokerCompanyName,
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.broker.workspace",
                            workspace.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportBrokerWorkspaceAcknowledged,
                            ExportAuditActions.ExportBrokerWorkspaceAcknowledged,
                            "export.broker.workspace",
                            workspace.Id.ToString(),
                            correlationId,
                            now,
                            new
                            {
                                exportCaseId = command.ExportCaseId.ToString(),
                                workspaceId = workspace.Id.ToString(),
                                brokerReference = workspace.BrokerReference,
                            });

                        helper.AddOutbox(
                            context.TenantId,
                            ExportOutboxMessageTypes.ExportBrokerWorkspaceAcknowledged,
                            "export-broker-workspace",
                            workspace.Id.ToString(),
                            correlationId,
                            $"{workspace.Id}:{workspace.Version}:{ExportOutboxMessageTypes.ExportBrokerWorkspaceAcknowledged}",
                            now,
                            new Dictionary<string, string?>
                            {
                                ["tenantId"] = context.TenantId.ToString(),
                                ["facilityId"] = context.FacilityId.ToString(),
                                ["exportCaseId"] = command.ExportCaseId.ToString(),
                                ["workspaceId"] = workspace.Id.ToString(),
                                ["occurredAtUtc"] = now.ToString("O"),
                            });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportBrokerWorkspaceWriteResult.Success(
                        workspace.Id, workspace.Version, ExportValueConversions.ToStorage(workspace.Status));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The broker workspace has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }

    public async Task<ExportBrokerCorrectionWriteResult> CreateCorrectionAsync(
        ExportOperationContext context,
        CreateExportBrokerCorrectionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var category = (command.Category ?? string.Empty).Trim();
        var title = (command.Title ?? string.Empty).Trim();
        var description = (command.Description ?? string.Empty).Trim();

        if (category.Length == 0) return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Category is required.");
        if (title.Length == 0) return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Title is required.");
        if (description.Length == 0) return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Description is required.");

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var workspace = await LoadWorkspaceAsync(context, command.ExportCaseId, command.BrokerWorkspaceId, ct);
                    if (workspace is null)
                    {
                        return ExportBrokerCorrectionWriteResult.Fail(Export2LFailureCodes.BrokerWorkspaceNotFound, "Broker workspace was not found.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var correction = new ExportBrokerCorrection
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = command.ExportCaseId,
                        BrokerWorkspaceId = workspace.Id,
                        Category = category,
                        Title = title,
                        Description = description,
                        Severity = command.Severity,
                        Status = ExportBrokerCorrectionStatus.Open,
                        RelatedDocumentId = command.RelatedDocumentId,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                    };

                    dbContext.ExportBrokerCorrections.Add(correction);

                    // Move workspace to CorrectionRequested if it was in Acknowledged
                    if (workspace.Status == ExportBrokerWorkspaceStatus.Acknowledged)
                    {
                        workspace.Status = ExportBrokerWorkspaceStatus.CorrectionRequested;
                        workspace.Version += 1;
                        workspace.UpdatedAtUtc = now;
                        workspace.LastModifiedBySubjectId = actorSubjectId;
                    }

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.BrokerCorrectionCreated,
                            "Broker correction requested",
                            $"{ExportValueConversions.ToStorage(command.Severity)}: {title}",
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.broker.correction",
                            correction.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportBrokerCorrectionCreated,
                            ExportAuditActions.ExportBrokerCorrectionCreated,
                            "export.broker.correction",
                            correction.Id.ToString(),
                            correlationId,
                            now,
                            new
                            {
                                exportCaseId = command.ExportCaseId.ToString(),
                                workspaceId = workspace.Id.ToString(),
                                correctionId = correction.Id.ToString(),
                                severity = ExportValueConversions.ToStorage(command.Severity),
                                category,
                                title,
                            });

                        var documents = await dbContext.ExportDocuments
                            .Where(d =>
                                d.TenantId == context.TenantId
                                && d.FacilityId == context.FacilityId
                                && d.ExportCaseId == command.ExportCaseId)
                            .ToListAsync(ct);
                        var operationalContext = await helper.BuildOperationalContextAsync(
                            context,
                            exportCase,
                            documents,
                            ct,
                            hasOpenBlockingBrokerCorrectionOverride:
                                command.Severity == ExportBrokerCorrectionSeverity.Blocking
                                    ? true
                                    : null);
                        await helper.RecalculateAndPersistAsync(
                            context, exportCase, documents,
                            correlationId, actorSubjectId, now,
                            emitInitial: false, operationalContext, ct);
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportBrokerCorrectionWriteResult.Success(correction.Id);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The broker workspace has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }

    public async Task<ExportBrokerCorrectionWriteResult> ResolveCorrectionAsync(
        ExportOperationContext context,
        ResolveExportBrokerCorrectionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (command.Resolution is not (ExportBrokerCorrectionStatus.Resolved or ExportBrokerCorrectionStatus.Cancelled))
        {
            return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ValidationFailed,
                "Resolution status must be Resolved or Cancelled.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var workspace = await LoadWorkspaceAsync(context, command.ExportCaseId, command.BrokerWorkspaceId, ct);
                    if (workspace is null)
                    {
                        return ExportBrokerCorrectionWriteResult.Fail(
                            Export2LFailureCodes.BrokerWorkspaceNotFound,
                            "Broker workspace was not found.");
                    }

                    if (workspace.Version != command.ExpectedWorkspaceVersion)
                    {
                        return ExportBrokerCorrectionWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The broker workspace has been modified. Please reload and try again.");
                    }

                    var correction = await dbContext.ExportBrokerCorrections
                        .SingleOrDefaultAsync(
                            c => c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.ExportCaseId == command.ExportCaseId
                                && c.BrokerWorkspaceId == command.BrokerWorkspaceId
                                && c.Id == command.CorrectionId,
                            ct);

                    if (correction is null)
                    {
                        return ExportBrokerCorrectionWriteResult.Fail(Export2LFailureCodes.BrokerCorrectionNotFound, "Broker correction was not found.");
                    }

                    if (correction.Status != ExportBrokerCorrectionStatus.Open)
                    {
                        return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ValidationFailed,
                            "Only open corrections can be resolved or cancelled.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    correction.Status = command.Resolution;
                    correction.ResolvedAtUtc = now;
                    correction.ResolvedBySubjectId = actorSubjectId;
                    correction.ResolutionNote = string.IsNullOrWhiteSpace(command.ResolutionNote) ? null : command.ResolutionNote.Trim();

                    workspace.Version += 1;
                    workspace.UpdatedAtUtc = now;
                    workspace.LastModifiedBySubjectId = actorSubjectId;

                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is not null)
                    {
                        exportCase.Version += 1;
                        exportCase.UpdatedAtUtc = now;
                        exportCase.LastModifiedBySubjectId = actorSubjectId;

                        var helper = new ExportReadinessPersistence(dbContext);
                        helper.AppendActivity(
                            exportCase,
                            ExportActivityEventTypes.BrokerCorrectionResolved,
                            "Broker correction resolved",
                            $"{ExportValueConversions.ToStorage(command.Resolution)}: {correction.Title}",
                            now,
                            ExportActivitySourceType.User,
                            actorSubjectId,
                            correlationId,
                            "export.broker.correction",
                            correction.Id);

                        helper.AddAudit(
                            context,
                            ExportAuditEventTypes.ExportBrokerCorrectionResolved,
                            ExportAuditActions.ExportBrokerCorrectionResolved,
                            "export.broker.correction",
                            correction.Id.ToString(),
                            correlationId,
                            now,
                            new
                            {
                                exportCaseId = command.ExportCaseId.ToString(),
                                correctionId = correction.Id.ToString(),
                                resolution = ExportValueConversions.ToStorage(command.Resolution),
                            });

                        var documents = await dbContext.ExportDocuments
                            .Where(d =>
                                d.TenantId == context.TenantId
                                && d.FacilityId == context.FacilityId
                                && d.ExportCaseId == command.ExportCaseId)
                            .ToListAsync(ct);

                        // Exclude the correction being resolved from the DB open-blocking check.
                        var stillBlocked = await dbContext.ExportBrokerCorrections
                            .AsNoTracking()
                            .AnyAsync(
                                c =>
                                    c.TenantId == context.TenantId
                                    && c.FacilityId == context.FacilityId
                                    && c.ExportCaseId == command.ExportCaseId
                                    && c.Id != correction.Id
                                    && c.Status == ExportBrokerCorrectionStatus.Open
                                    && c.Severity == ExportBrokerCorrectionSeverity.Blocking,
                                ct);

                        var operationalContext = await helper.BuildOperationalContextAsync(
                            context,
                            exportCase,
                            documents,
                            ct,
                            hasOpenBlockingBrokerCorrectionOverride: stillBlocked);
                        await helper.RecalculateAndPersistAsync(
                            context, exportCase, documents,
                            correlationId, actorSubjectId, now,
                            emitInitial: false, operationalContext, ct);
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportBrokerCorrectionWriteResult.Success(correction.Id);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker service is unavailable.");
        }
    }

    private async Task<ExportCase?> LoadCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCases
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.Id == exportCaseId,
                cancellationToken);

    private async Task<ExportBrokerWorkspace?> LoadWorkspaceAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        Guid workspaceId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportBrokerWorkspaces
            .SingleOrDefaultAsync(
                w => w.TenantId == context.TenantId
                    && w.FacilityId == context.FacilityId
                    && w.ExportCaseId == exportCaseId
                    && w.Id == workspaceId,
                cancellationToken);
}

public sealed class UnavailableExportBrokerWriteRepository : IExportBrokerWriteRepository
{
    private static ExportBrokerWorkspaceWriteResult WorkspaceUnavailable() =>
        ExportBrokerWorkspaceWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker persistence is disabled.");

    private static ExportBrokerPackageWriteResult PackageUnavailable() =>
        ExportBrokerPackageWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker persistence is disabled.");

    private static ExportBrokerCorrectionWriteResult CorrectionUnavailable() =>
        ExportBrokerCorrectionWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export broker persistence is disabled.");

    public Task<ExportBrokerWorkspaceWriteResult> CreateWorkspaceAsync(ExportOperationContext context, CreateExportBrokerWorkspaceCommand command, CancellationToken cancellationToken = default) => Task.FromResult(WorkspaceUnavailable());
    public Task<ExportBrokerPackageWriteResult> PreparePackageAsync(ExportOperationContext context, PrepareExportBrokerPackageCommand command, CancellationToken cancellationToken = default) => Task.FromResult(PackageUnavailable());
    public Task<ExportBrokerPackageWriteResult> SendPackageAsync(ExportOperationContext context, SendExportBrokerPackageCommand command, CancellationToken cancellationToken = default) => Task.FromResult(PackageUnavailable());
    public Task<ExportBrokerWorkspaceWriteResult> AcknowledgeAsync(ExportOperationContext context, AcknowledgeExportBrokerCommand command, CancellationToken cancellationToken = default) => Task.FromResult(WorkspaceUnavailable());
    public Task<ExportBrokerCorrectionWriteResult> CreateCorrectionAsync(ExportOperationContext context, CreateExportBrokerCorrectionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(CorrectionUnavailable());
    public Task<ExportBrokerCorrectionWriteResult> ResolveCorrectionAsync(ExportOperationContext context, ResolveExportBrokerCorrectionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(CorrectionUnavailable());
}
