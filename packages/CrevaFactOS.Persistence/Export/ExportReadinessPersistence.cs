using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

/// <summary>
/// Shared helper that recalculates and persists readiness for an export case.
/// Used by ExportCaseWriteRepository and the EXPORT-2L write repositories
/// to avoid duplicating activity/audit/outbox logic.
/// </summary>
internal sealed class ExportReadinessPersistence(CrevaFactOSDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task RecalculateAndPersistAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        IReadOnlyList<ExportDocument> documents,
        Guid correlationId,
        string actorSubjectId,
        DateTimeOffset now,
        bool emitInitial,
        ExportOperationalReadinessContext? operationalContext,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var previousStatus = exportCase.ReadinessStatus;
        var previousPercent = exportCase.ReadinessPercent;
        var previousBlocking = exportCase.BlockingIssueCount;
        var previousMissing = exportCase.MissingRequirementCount;

        var calc = ExportReadinessEngine.Calculate(exportCase, documents, operationalContext);

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

    public async Task<ExportOperationalReadinessContext> BuildOperationalContextAsync(
        ExportOperationContext context,
        ExportCase exportCase,
        IReadOnlyList<ExportDocument> documents,
        CancellationToken cancellationToken,
        bool? hasOpenBlockingBrokerCorrectionOverride = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exportCase);
        ArgumentNullException.ThrowIfNull(documents);

        var packingItems = await dbContext.ExportPackingItems
            .AsNoTracking()
            .Where(i =>
                i.TenantId == context.TenantId
                && i.FacilityId == context.FacilityId
                && i.ExportCaseId == exportCase.Id)
            .ToListAsync(cancellationToken);

        // Include items added in the current unit-of-work that are not yet visible via AsNoTracking.
        foreach (var entry in dbContext.ChangeTracker.Entries<ExportPackingItem>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Entity.TenantId == context.TenantId
                && entry.Entity.FacilityId == context.FacilityId
                && entry.Entity.ExportCaseId == exportCase.Id
                && packingItems.All(i => i.Id != entry.Entity.Id))
            {
                packingItems.Add(entry.Entity);
            }
        }

        // Ordered totals are not loaded here; reconciliation remains UnableToValidate
        // unless line-level mapping alone is enough for a future enhancement.
        var reconciliation = ExportPackingReconciliationEngine.Reconcile(packingItems, orderedQuantityTotal: null);

        var hasOpenBlockingBrokerCorrection = hasOpenBlockingBrokerCorrectionOverride
            ?? await dbContext.ExportBrokerCorrections
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.ExportCaseId == exportCase.Id
                        && c.Status == ExportBrokerCorrectionStatus.Open
                        && c.Severity == ExportBrokerCorrectionSeverity.Blocking,
                    cancellationToken);

        var primaryShipment = await dbContext.ExportShipments
            .AsNoTracking()
            .Where(s =>
                s.TenantId == context.TenantId
                && s.FacilityId == context.FacilityId
                && s.ExportCaseId == exportCase.Id
                && s.Status != ExportShipmentStatus.Cancelled)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var hasVerifiedInvoice = documents.Any(d =>
            string.Equals(d.DocumentType, ExportDocumentType.CommercialInvoice, StringComparison.Ordinal)
            && d.DocumentStatus == ExportDocumentStatus.Verified);

        var hasVerifiedPackingList = documents.Any(d =>
            string.Equals(d.DocumentType, ExportDocumentType.PackingList, StringComparison.Ordinal)
            && d.DocumentStatus == ExportDocumentStatus.Verified);

        return new ExportOperationalReadinessContext(
            exportCase.PackingStatus,
            reconciliation.Status,
            hasOpenBlockingBrokerCorrection,
            primaryShipment is not null,
            primaryShipment?.TransportMode,
            primaryShipment?.PlannedDepartureAtUtc,
            hasVerifiedInvoice,
            hasVerifiedPackingList);
    }

    public void AppendActivity(
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

    public void AddAudit(
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

    public void AddOutbox(
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
}
