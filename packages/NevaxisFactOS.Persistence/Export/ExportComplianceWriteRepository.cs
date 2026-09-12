using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportComplianceWriteRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportComplianceWriteRepository
{
    public async Task<ExportComplianceWriteResult> EvaluateComplianceAsync(
        ExportOperationContext context,
        EvaluateExportComplianceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct => await RunEvaluationAsync(
                    context,
                    command.ExportCaseId,
                    command.ExpectedCaseVersion,
                    command.ShipmentId,
                    isRevalidation: false,
                    ct),
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "An evaluation number conflict occurred. Please reload and try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export compliance service is unavailable.");
        }
    }

    public async Task<ExportComplianceWriteResult> RevalidateComplianceAsync(
        ExportOperationContext context,
        RevalidateExportComplianceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct => await RunEvaluationAsync(
                    context,
                    command.ExportCaseId,
                    command.ExpectedCaseVersion,
                    shipmentId: null,
                    isRevalidation: true,
                    ct),
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "An evaluation number conflict occurred. Please reload and try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export compliance service is unavailable.");
        }
    }

    public async Task<ExportComplianceRequirementWriteResult> SatisfyRequirementAsync(
        ExportOperationContext context,
        SatisfyComplianceRequirementCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var resolutionType = (command.ResolutionType ?? string.Empty).Trim();
        var resolutionNote = (command.ResolutionNote ?? string.Empty).Trim();
        if (resolutionType.Length == 0 || resolutionNote.Length == 0)
        {
            return ExportComplianceRequirementWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Resolution type and note are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportComplianceRequirementWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    var requirement = await dbContext.ExportComplianceRequirements
                        .SingleOrDefaultAsync(
                            r => r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && r.ExportCaseId == command.ExportCaseId
                                && r.Id == command.RequirementId,
                            ct);

                    if (requirement is null)
                    {
                        return ExportComplianceRequirementWriteResult.Fail(
                            Export3LFailureCodes.RequirementNotFound,
                            "Compliance requirement was not found.");
                    }

                    if (requirement.Version != command.ExpectedRequirementVersion)
                    {
                        return ExportComplianceRequirementWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The compliance requirement has been modified. Please reload and try again.");
                    }

                    if (exportCase.CurrentComplianceEvaluationId != requirement.EvaluationId)
                    {
                        return ExportComplianceRequirementWriteResult.Fail(
                            Export3LFailureCodes.EvaluationImmutable,
                            "Only requirements on the current evaluation can be satisfied.");
                    }

                    var evaluation = await dbContext.ExportComplianceEvaluations
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId
                                && e.FacilityId == context.FacilityId
                                && e.Id == requirement.EvaluationId,
                            ct);

                    if (evaluation is null || evaluation.IsStale)
                    {
                        return ExportComplianceRequirementWriteResult.Fail(
                            Export3LFailureCodes.EvaluationImmutable,
                            "The compliance evaluation is stale and cannot be modified.");
                    }

                    if (requirement.Status is ExportComplianceRequirementStatus.Satisfied
                        or ExportComplianceRequirementStatus.NotApplicable)
                    {
                        return ExportComplianceRequirementWriteResult.Success(
                            requirement.Version,
                            exportCase.Version);
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    requirement.Status = ExportComplianceRequirementStatus.Satisfied;
                    requirement.ResolutionType = resolutionType;
                    requirement.ResolutionNote = resolutionNote;
                    requirement.SatisfiedAtUtc = now;
                    requirement.SatisfiedBySubjectId = actorSubjectId;
                    requirement.Version += 1;

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.ComplianceCleared,
                        "Compliance requirement satisfied",
                        requirement.Title,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.compliance.requirement",
                        requirement.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportComplianceRequirementSatisfied,
                        ExportAuditActions.ExportComplianceRequirementSatisfied,
                        "export.compliance.requirement",
                        requirement.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = command.ExportCaseId.ToString(),
                            requirementId = requirement.Id.ToString(),
                            requirementCode = requirement.RequirementCode,
                            resolutionType,
                        });

                    var documents = await LoadDocumentsAsync(context, command.ExportCaseId, ct);
                    var operationalContext = await helper.BuildOperationalContextAsync(
                        context, exportCase, documents, ct);
                    await helper.RecalculateAndPersistAsync(
                        context, exportCase, documents,
                        correlationId, actorSubjectId, now,
                        emitInitial: false, operationalContext, ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportComplianceRequirementWriteResult.Success(
                        requirement.Version,
                        exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportComplianceRequirementWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The compliance requirement has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportComplianceRequirementWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export compliance service is unavailable.");
        }
    }

    public async Task MarkDestinationChangeRevalidationAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exportCase);

        var now = DateTimeOffset.UtcNow;
        var correlationId = context.CorrelationId ?? Guid.NewGuid();
        var actorSubjectId = context.IdentitySubjectId.ToString();

        exportCase.ComplianceRevalidationRequired = true;
        exportCase.ComplianceStatus = ExportComplianceEvaluationResult.UnableToEvaluate;
        exportCase.UpdatedAtUtc = now;
        exportCase.LastModifiedBySubjectId = actorSubjectId;

        if (exportCase.CurrentComplianceEvaluationId.HasValue)
        {
            var evaluation = await dbContext.ExportComplianceEvaluations
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.Id == exportCase.CurrentComplianceEvaluationId.Value,
                    cancellationToken);

            if (evaluation is not null)
            {
                evaluation.IsStale = true;
                evaluation.RevalidationRequired = true;
            }
        }

        var helper = new ExportReadinessPersistence(dbContext);
        helper.AppendActivity(
            exportCase,
            ExportActivityEventTypes.ComplianceRevalidationRequired,
            "Compliance revalidation required",
            exportCase.DestinationCountryCode,
            now,
            ExportActivitySourceType.System,
            actorSubjectId,
            correlationId,
            "export.case",
            exportCase.Id);

        helper.AddAudit(
            context,
            ExportAuditEventTypes.ExportComplianceRevalidated,
            ExportAuditActions.ExportComplianceRevalidated,
            "export.case",
            exportCase.Id.ToString(),
            correlationId,
            now,
            new
            {
                exportCaseId = exportCase.Id.ToString(),
                destinationCountryCode = exportCase.DestinationCountryCode,
                reason = "destination-change",
            });

        helper.AddOutbox(
            context.TenantId,
            ExportOutboxMessageTypes.ExportComplianceRevalidationRequired,
            "export-case",
            exportCase.Id.ToString(),
            correlationId,
            $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportComplianceRevalidationRequired}",
            now,
            new Dictionary<string, string?>
            {
                ["tenantId"] = context.TenantId.ToString(),
                ["facilityId"] = context.FacilityId.ToString(),
                ["exportCaseId"] = exportCase.Id.ToString(),
                ["destinationCountryCode"] = exportCase.DestinationCountryCode,
                ["occurredAtUtc"] = now.ToString("O"),
            });
    }

    private async Task<ExportComplianceWriteResult> RunEvaluationAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        uint expectedCaseVersion,
        Guid? shipmentId,
        bool isRevalidation,
        CancellationToken cancellationToken)
    {
        var exportCase = await LoadCaseAsync(context, exportCaseId, cancellationToken);
        if (exportCase is null)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.NotFound,
                "Export case was not found.");
        }

        if (exportCase.Version != expectedCaseVersion)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }

        if (exportCase.Status == ExportCaseStatus.Closed)
        {
            return ExportComplianceWriteResult.Fail(
                ExportFailureCodes.CaseClosed,
                "Cannot evaluate compliance for a closed export case.");
        }

        var now = DateTimeOffset.UtcNow;
        var correlationId = context.CorrelationId ?? Guid.NewGuid();
        var actorSubjectId = context.IdentitySubjectId.ToString();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        ExportShipment? shipment = null;
        if (shipmentId.HasValue)
        {
            shipment = await dbContext.ExportShipments
                .SingleOrDefaultAsync(
                    s => s.TenantId == context.TenantId
                        && s.FacilityId == context.FacilityId
                        && s.ExportCaseId == exportCaseId
                        && s.Id == shipmentId.Value,
                    cancellationToken);

            if (shipment is null)
            {
                return ExportComplianceWriteResult.Fail(
                    ExportFailureCodes.NotFound,
                    "Shipment was not found.");
            }
        }
        else
        {
            shipment = await dbContext.ExportShipments
                .Where(s =>
                    s.TenantId == context.TenantId
                    && s.FacilityId == context.FacilityId
                    && s.ExportCaseId == exportCaseId
                    && s.Status != ExportShipmentStatus.Cancelled)
                .OrderByDescending(s => s.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var documents = await LoadDocumentsAsync(context, exportCaseId, cancellationToken);

        var verifiedRulePack = await dbContext.ExportCountryRulePacks
            .Where(p =>
                p.TenantId == context.TenantId
                && p.CountryCode == exportCase.DestinationCountryCode
                && p.Status == ExportCountryRulePackStatus.Verified
                && p.EffectiveFrom <= today
                && (p.EffectiveTo == null || p.EffectiveTo >= today))
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var applicableRules = verifiedRulePack is null
            ? new List<ExportCountryComplianceRule>()
            : await dbContext.ExportCountryComplianceRules
                .Where(r => r.TenantId == context.TenantId && r.RulePackId == verifiedRulePack.Id)
                .ToListAsync(cancellationToken);

        var customerRequirements = await dbContext.ExportCustomerRequirements
            .Where(r =>
                r.TenantId == context.TenantId
                && r.Status == ExportCustomerRequirementStatus.Verified)
            .ToListAsync(cancellationToken);

        var packagingType = await dbContext.ExportPackingUnits
            .Where(u =>
                u.TenantId == context.TenantId
                && u.FacilityId == context.FacilityId
                && u.ExportCaseId == exportCaseId)
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => u.UnitType)
            .FirstOrDefaultAsync(cancellationToken);

        var engineInput = new ExportComplianceEngine.EvaluationInput(
            exportCase.DestinationCountryCode,
            exportCase.OriginCountryCode,
            exportCase.OriginConfirmationStatus,
            exportCase.HsCode,
            exportCase.HsCodeStatus,
            exportCase.ProductReference,
            exportCase.ProductGroup,
            exportCase.CustomerName,
            shipment?.IncotermCode ?? exportCase.IncotermCode,
            shipment?.TransportMode,
            packagingType,
            documents,
            verifiedRulePack,
            applicableRules,
            customerRequirements);

        var output = ExportComplianceEngine.Evaluate(engineInput);

        Guid? supersedesEvaluationId = null;
        if (exportCase.CurrentComplianceEvaluationId.HasValue)
        {
            var previousEvaluation = await dbContext.ExportComplianceEvaluations
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.Id == exportCase.CurrentComplianceEvaluationId.Value,
                    cancellationToken);

            if (previousEvaluation is not null)
            {
                previousEvaluation.IsStale = true;
                supersedesEvaluationId = previousEvaluation.Id;
            }
        }

        var maxEvaluationNumber = await dbContext.ExportComplianceEvaluations
            .Where(e =>
                e.TenantId == context.TenantId
                && e.FacilityId == context.FacilityId
                && e.ExportCaseId == exportCaseId)
            .Select(e => (int?)e.EvaluationNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var evaluation = new ExportComplianceEvaluation
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ExportCaseId = exportCaseId,
            ShipmentId = shipment?.Id,
            EvaluationNumber = maxEvaluationNumber + 1,
            CountryCode = exportCase.DestinationCountryCode,
            OriginCountryCode = exportCase.OriginCountryCode,
            HsCodeSnapshot = exportCase.HsCode,
            RulePackId = verifiedRulePack?.Id,
            RulePackVersionNumber = verifiedRulePack?.VersionNumber,
            Result = output.Result,
            ReadinessPercent = output.ReadinessPercent,
            IsStale = false,
            RevalidationRequired = false,
            SupersedesEvaluationId = supersedesEvaluationId,
            EvaluatedAtUtc = now,
            EvaluatedBySubjectId = actorSubjectId,
            Version = 1,
        };

        dbContext.ExportComplianceEvaluations.Add(evaluation);

        foreach (var req in output.Requirements)
        {
            dbContext.ExportComplianceRequirements.Add(new ExportComplianceRequirement
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ExportCaseId = exportCaseId,
                EvaluationId = evaluation.Id,
                ShipmentId = shipment?.Id,
                RulePackId = req.RulePackId,
                RuleId = req.RuleId,
                RequirementCode = req.RequirementCode,
                Title = req.Title,
                Description = req.Description,
                Status = req.Status,
                Severity = req.Severity,
                Blocking = req.Blocking,
                RequiredDocumentType = req.RequiredDocumentType,
                RelatedDocumentId = req.RelatedDocumentId,
                EvaluatedAtUtc = now,
                SourceReference = req.SourceReference,
                RuleVersionSnapshot = req.RuleVersionSnapshot,
                Version = 1,
            });
        }

        exportCase.CurrentComplianceEvaluationId = evaluation.Id;
        exportCase.ComplianceStatus = output.Result;
        exportCase.ComplianceRevalidationRequired = false;
        exportCase.Version += 1;
        exportCase.UpdatedAtUtc = now;
        exportCase.LastModifiedBySubjectId = actorSubjectId;

        var helper = new ExportReadinessPersistence(dbContext);
        var activityType = output.Result switch
        {
            ExportComplianceEvaluationResult.Blocked => ExportActivityEventTypes.ComplianceBlocked,
            ExportComplianceEvaluationResult.Ready => ExportActivityEventTypes.ComplianceCleared,
            _ => ExportActivityEventTypes.ComplianceEvaluated,
        };

        helper.AppendActivity(
            exportCase,
            activityType,
            isRevalidation ? "Compliance revalidated" : "Compliance evaluated",
            $"{ExportValueConversions.ToStorage(output.Result)} ({output.ReadinessPercent}%)",
            now,
            ExportActivitySourceType.User,
            actorSubjectId,
            correlationId,
            "export.compliance.evaluation",
            evaluation.Id);

        helper.AddAudit(
            context,
            isRevalidation
                ? ExportAuditEventTypes.ExportComplianceRevalidated
                : ExportAuditEventTypes.ExportComplianceEvaluated,
            isRevalidation
                ? ExportAuditActions.ExportComplianceRevalidated
                : ExportAuditActions.ExportComplianceEvaluated,
            "export.compliance.evaluation",
            evaluation.Id.ToString(),
            correlationId,
            now,
            new
            {
                exportCaseId = exportCaseId.ToString(),
                evaluationId = evaluation.Id.ToString(),
                evaluationNumber = evaluation.EvaluationNumber,
                result = ExportValueConversions.ToStorage(output.Result),
                readinessPercent = output.ReadinessPercent,
            });

        if (output.Result == ExportComplianceEvaluationResult.Blocked)
        {
            helper.AddOutbox(
                context.TenantId,
                ExportOutboxMessageTypes.ExportComplianceBlocked,
                "export-case",
                exportCase.Id.ToString(),
                correlationId,
                $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportComplianceBlocked}",
                now,
                new Dictionary<string, string?>
                {
                    ["tenantId"] = context.TenantId.ToString(),
                    ["facilityId"] = context.FacilityId.ToString(),
                    ["exportCaseId"] = exportCase.Id.ToString(),
                    ["evaluationId"] = evaluation.Id.ToString(),
                    ["occurredAtUtc"] = now.ToString("O"),
                });
        }
        else if (output.Result == ExportComplianceEvaluationResult.Ready)
        {
            helper.AddOutbox(
                context.TenantId,
                ExportOutboxMessageTypes.ExportComplianceReady,
                "export-case",
                exportCase.Id.ToString(),
                correlationId,
                $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportComplianceReady}",
                now,
                new Dictionary<string, string?>
                {
                    ["tenantId"] = context.TenantId.ToString(),
                    ["facilityId"] = context.FacilityId.ToString(),
                    ["exportCaseId"] = exportCase.Id.ToString(),
                    ["evaluationId"] = evaluation.Id.ToString(),
                    ["occurredAtUtc"] = now.ToString("O"),
                });
        }

        var operationalContext = await helper.BuildOperationalContextAsync(
            context, exportCase, documents, cancellationToken);
        await helper.RecalculateAndPersistAsync(
            context, exportCase, documents,
            correlationId, actorSubjectId, now,
            emitInitial: false, operationalContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ExportComplianceWriteResult.Success(
            evaluation.Id,
            exportCase.Version,
            ExportValueConversions.ToStorage(output.Result));
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

    private async Task<List<ExportDocument>> LoadDocumentsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportDocuments
            .Where(d =>
                d.TenantId == context.TenantId
                && d.FacilityId == context.FacilityId
                && d.ExportCaseId == exportCaseId)
            .ToListAsync(cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

public sealed class UnavailableExportComplianceWriteRepository : IExportComplianceWriteRepository
{
    private static ExportComplianceWriteResult EvalUnavailable() =>
        ExportComplianceWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export compliance persistence is disabled.");

    private static ExportComplianceRequirementWriteResult RequirementUnavailable() =>
        ExportComplianceRequirementWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export compliance persistence is disabled.");

    public Task<ExportComplianceWriteResult> EvaluateComplianceAsync(
        ExportOperationContext context,
        EvaluateExportComplianceCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EvalUnavailable());

    public Task<ExportComplianceWriteResult> RevalidateComplianceAsync(
        ExportOperationContext context,
        RevalidateExportComplianceCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(EvalUnavailable());

    public Task<ExportComplianceRequirementWriteResult> SatisfyRequirementAsync(
        ExportOperationContext context,
        SatisfyComplianceRequirementCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RequirementUnavailable());

    public Task MarkDestinationChangeRevalidationAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
