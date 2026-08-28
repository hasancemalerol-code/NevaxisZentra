using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialProfitabilityAlertRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ICommercialProfitabilityAlertRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<CommercialProfitabilityAlertWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var alerts = await dbContext.CommercialProfitabilityAlerts.AsNoTracking()
                    .Where(alert => alert.TenantId == context.TenantId && alert.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                return new CommercialProfitabilityAlertWorkspaceReadModel(
                    CommercialProfitabilityAlertConstants.RuleSetVersion,
                    CommercialIntelligenceConstants.ProfitabilityComparisonDevelopmentPilotProvenance,
                    alerts.Count(alert => alert.LifecycleStatus == CommercialProfitabilityAlertLifecycleStatuses.Active),
                    alerts.Count(alert => CommercialProfitabilityAlertSeverities.HighOrCritical.Contains(alert.Severity)
                                          && CommercialProfitabilityAlertLifecycleStatuses.Open.Contains(alert.LifecycleStatus)),
                    alerts.Count(alert => alert.LifecycleStatus == CommercialProfitabilityAlertLifecycleStatuses.Acknowledged),
                    alerts.Count(alert => alert.LifecycleStatus == CommercialProfitabilityAlertLifecycleStatuses.TaskCreated),
                    alerts.Count(alert => alert.AlertCategory == CommercialProfitabilityAlertCategories.DataReview
                                          && CommercialProfitabilityAlertLifecycleStatuses.Open.Contains(alert.LifecycleStatus)),
                    true,
                    canManage,
                    canCreateTask);
            },
            cancellationToken);

    public Task<IReadOnlyList<CommercialProfitabilityAlertSummaryReadModel>> ListAlertsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityAlertListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.CommercialProfitabilityAlerts.AsNoTracking()
                    .Where(alert => alert.TenantId == context.TenantId && alert.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Category))
                {
                    query = query.Where(alert => alert.AlertCategory == filters.Category.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.Severity))
                {
                    query = query.Where(alert => alert.Severity == filters.Severity.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.LifecycleStatus))
                {
                    query = query.Where(alert => alert.LifecycleStatus == filters.LifecycleStatus.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.EvidenceStatus))
                {
                    query = query.Where(alert => alert.EvidenceStatus == filters.EvidenceStatus.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.ComparisonType))
                {
                    query = query.Where(alert => alert.SourceComparisonType == filters.ComparisonType.Trim());
                }

                var alerts = await query
                    .OrderByDescending(alert => alert.MaterializedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return (IReadOnlyList<CommercialProfitabilityAlertSummaryReadModel>)alerts.Select(MapSummary).ToList();
            },
            cancellationToken);

    public Task<CommercialProfitabilityAlertDetailReadModel?> GetAlertByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid alertId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var alert = await dbContext.CommercialProfitabilityAlerts.AsNoTracking()
                    .Include(a => a.AuditEvents)
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.Id == alertId,
                        ct);

                if (alert is null)
                {
                    return null;
                }

                var comparison = await CommercialProfitabilityComparisonOperations.GetComparisonByIdAsync(
                    dbContext,
                    context,
                    alert.SourceComparisonId,
                    ct);

                if (comparison is null)
                {
                    return null;
                }

                return MapDetail(alert, comparison);
            },
            cancellationToken);

    public Task<CommercialProfitabilityAlertWriteResult> MaterializeAlertsAsync(
        CommercialIntelligenceOperationContext context,
        MaterializeCommercialProfitabilityAlertsCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MaterializeAlertsInternalAsync(context, command, ct),
            cancellationToken);

    public Task<CommercialProfitabilityAlertWriteResult> TransitionAlertAsync(
        CommercialIntelligenceOperationContext context,
        TransitionCommercialProfitabilityAlertCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateTransitionCommand(command);
                if (validationError is not null)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        validationError);
                }

                var alert = await dbContext.CommercialProfitabilityAlerts
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.Id == command.AlertId,
                        ct);

                if (alert is null)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Kârlılık uyarısı bulunamadı.");
                }

                if (alert.Version != command.ExpectedVersion)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Uyarı sürümü güncel değil.");
                }

                var targetStatus = command.TargetLifecycleStatus.Trim();
                if (!CommercialProfitabilityAlertLifecycle.CanTransition(alert.LifecycleStatus, targetStatus))
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Bu yaşam döngüsü geçişi desteklenmiyor.");
                }

                var noteError = CommercialProfitabilityAlertLifecycle.ValidateTransitionNote(targetStatus, command.Note);
                if (noteError is not null)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        noteError);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var previousStatus = alert.LifecycleStatus;
                alert.LifecycleStatus = targetStatus;
                alert.UpdatedAtUtc = now;
                alert.UpdatedBy = actor;
                alert.Version += 1;

                await AppendAuditEventAsync(
                    alert,
                    MapTransitionEventType(targetStatus),
                    new
                    {
                        previousStatus,
                        targetStatus,
                        note = command.Note?.Trim(),
                    },
                    actor,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);

                var detail = await GetAlertByIdAsync(context, alert.Id, ct);
                return CommercialProfitabilityAlertWriteResult.Success(alert.Id, alert.Version, alert: detail);
            },
            cancellationToken);

    public Task<CommercialProfitabilityAlertWriteResult> LinkTaskAsync(
        CommercialIntelligenceOperationContext context,
        LinkCommercialProfitabilityAlertTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var alert = await dbContext.CommercialProfitabilityAlerts
                    .SingleOrDefaultAsync(
                        a => a.TenantId == context.TenantId
                             && a.FacilityId == context.FacilityId
                             && a.Id == command.AlertId,
                        ct);

                if (alert is null)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Kârlılık uyarısı bulunamadı.");
                }

                if (alert.Version != command.ExpectedVersion)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ConcurrencyConflict,
                        "Uyarı sürümü güncel değil.");
                }

                if (alert.LinkedAiTaskId.HasValue)
                {
                    if (alert.LinkedAiTaskId.Value == command.AiTaskId)
                    {
                        var existingDetail = await GetAlertByIdAsync(context, alert.Id, ct);
                        return CommercialProfitabilityAlertWriteResult.Success(
                            alert.Id,
                            alert.Version,
                            idempotentReplay: true,
                            alert: existingDetail);
                    }

                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.ValidationFailed,
                        "Uyarı zaten farklı bir göreve bağlı.");
                }

                var taskExists = await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(
                        task => task.TenantId == context.TenantId
                                && task.FacilityId == context.FacilityId
                                && task.Id == command.AiTaskId,
                        ct);

                if (!taskExists)
                {
                    return CommercialProfitabilityAlertWriteResult.Fail(
                        CommercialIntelligenceFailureCodes.NotFound,
                        "Bağlanacak AI görevi bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                alert.LinkedAiTaskId = command.AiTaskId;
                alert.UpdatedAtUtc = now;
                alert.UpdatedBy = actor;

                if (CommercialProfitabilityAlertLifecycle.CanTransition(
                        alert.LifecycleStatus,
                        CommercialProfitabilityAlertLifecycleStatuses.TaskCreated))
                {
                    alert.LifecycleStatus = CommercialProfitabilityAlertLifecycleStatuses.TaskCreated;
                }

                alert.Version += 1;

                await AppendAuditEventAsync(
                    alert,
                    CommercialProfitabilityAlertAuditEventTypes.TaskLinked,
                    new { aiTaskId = command.AiTaskId },
                    actor,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);

                var detail = await GetAlertByIdAsync(context, alert.Id, ct);
                return CommercialProfitabilityAlertWriteResult.Success(alert.Id, alert.Version, alert: detail);
            },
            cancellationToken);

    private async Task<CommercialProfitabilityAlertWriteResult> MaterializeAlertsInternalAsync(
        CommercialIntelligenceOperationContext context,
        MaterializeCommercialProfitabilityAlertsCommand command,
        CancellationToken cancellationToken)
    {
        var comparisons = await dbContext.CommercialProfitabilityComparisonRuns.AsNoTracking()
            .Include(run => run.Profile)
            .Include(run => run.Results)
            .Include(run => run.Drivers)
            .Where(run => run.TenantId == context.TenantId && run.FacilityId == context.FacilityId)
            .OrderBy(run => run.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var createdCount = 0;
        var refreshedCount = 0;
        var skippedCount = 0;
        var alertIds = new List<Guid>();
        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();

        foreach (var run in comparisons)
        {
            if (run.Profile is null)
            {
                skippedCount += 1;
                continue;
            }

            if (!CommercialProfitabilityAlertCategoryResolver.TryResolveCategory(run, run.Results.ToList(), out var category))
            {
                skippedCount += 1;
                continue;
            }

            var idempotencyKey = CommercialProfitabilityAlertPresentationBuilder.BuildMaterializationIdempotencyKey(run.Id, category);
            var snapshotJson = CommercialProfitabilityAlertPresentationBuilder.BuildSourceSnapshotJson(run, run.Drivers.ToList());
            var fingerprint = CommercialProfitabilityAlertIdempotencyFingerprint.ComputeMaterializationFingerprint(
                context,
                run.Id,
                category,
                snapshotJson);

            var existing = await dbContext.CommercialProfitabilityAlerts
                .SingleOrDefaultAsync(
                    alert => alert.TenantId == context.TenantId && alert.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (existing is not null)
            {
                if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    existing.EvidenceSummary = CommercialProfitabilityAlertPresentationBuilder.BuildEvidenceSummary(run, run.Drivers.ToList());
                    existing.SourceSnapshotJson = snapshotJson;
                    existing.Severity = CommercialProfitabilityAlertSeverityMapper.MapSeverity(run, run.Profile!, run.Drivers.ToList());
                    existing.EvidenceStatus = run.EvidenceStatus;
                    existing.ComparisonOutcome = run.ComparisonOutcome;
                    existing.LeadingDriverCode = run.LeadingDriverCode;
                    existing.IdempotencyPayloadFingerprint = fingerprint;
                    existing.UpdatedAtUtc = now;
                    existing.UpdatedBy = actor;
                    existing.Version += 1;

                    await AppendAuditEventAsync(
                        existing,
                        CommercialProfitabilityAlertAuditEventTypes.Refreshed,
                        new { comparisonRunId = run.Id, category },
                        actor,
                        now,
                        cancellationToken);

                    refreshedCount += 1;
                    alertIds.Add(existing.Id);
                }
                else
                {
                    skippedCount += 1;
                    alertIds.Add(existing.Id);
                }

                continue;
            }

            var activeDuplicate = await dbContext.CommercialProfitabilityAlerts
                .AnyAsync(
                    alert => alert.TenantId == context.TenantId
                             && alert.FacilityId == context.FacilityId
                             && alert.SourceComparisonId == run.Id
                             && alert.AlertCategory == category
                             && alert.LifecycleStatus == CommercialProfitabilityAlertLifecycleStatuses.Active,
                    cancellationToken);

            if (activeDuplicate)
            {
                skippedCount += 1;
                continue;
            }

            var alert = new CommercialProfitabilityAlert
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AlertReference = $"CPA-{run.ComparisonReference}",
                AlertCategory = category,
                Severity = CommercialProfitabilityAlertSeverityMapper.MapSeverity(run, run.Profile!, run.Drivers.ToList()),
                LifecycleStatus = CommercialProfitabilityAlertLifecycleStatuses.Active,
                SourceComparisonId = run.Id,
                SourceComparisonType = run.ComparisonType,
                Title = CommercialProfitabilityAlertPresentationBuilder.BuildTitle(category, run),
                EvidenceSummary = CommercialProfitabilityAlertPresentationBuilder.BuildEvidenceSummary(run, run.Drivers.ToList()),
                EvidenceStatus = run.EvidenceStatus,
                ComparisonOutcome = run.ComparisonOutcome,
                LeadingDriverCode = run.LeadingDriverCode,
                SourceSnapshotJson = snapshotJson,
                IdempotencyKey = idempotencyKey,
                IdempotencyPayloadFingerprint = fingerprint,
                RuleSetVersion = CommercialProfitabilityAlertConstants.RuleSetVersion,
                SourceProvenance = run.SourceProvenance,
                Version = 1,
                MaterializedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBy = actor,
                UpdatedBy = actor,
            };

            dbContext.CommercialProfitabilityAlerts.Add(alert);
            await AppendAuditEventAsync(
                alert,
                CommercialProfitabilityAlertAuditEventTypes.Materialized,
                new { comparisonRunId = run.Id, category, severity = alert.Severity },
                actor,
                now,
                cancellationToken);

            createdCount += 1;
            alertIds.Add(alert.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return CommercialProfitabilityAlertWriteResult.Success(
            alertIds.FirstOrDefault(),
            1,
            materialization: new CommercialProfitabilityAlertMaterializationResult(
                createdCount,
                refreshedCount,
                skippedCount,
                alertIds));
    }

    private static string? ValidateTransitionCommand(TransitionCommercialProfitabilityAlertCommand command)
    {
        if (command.AlertId == Guid.Empty)
        {
            return "Uyarı kimliği gereklidir.";
        }

        if (string.IsNullOrWhiteSpace(command.TargetLifecycleStatus))
        {
            return "Hedef yaşam döngüsü durumu gereklidir.";
        }

        if (!CommercialProfitabilityAlertLifecycleStatuses.All.Contains(command.TargetLifecycleStatus.Trim()))
        {
            return "Geçersiz yaşam döngüsü durumu.";
        }

        return null;
    }

    private static string MapTransitionEventType(string targetStatus) =>
        targetStatus switch
        {
            CommercialProfitabilityAlertLifecycleStatuses.Acknowledged =>
                CommercialProfitabilityAlertAuditEventTypes.Acknowledged,
            CommercialProfitabilityAlertLifecycleStatuses.Dismissed =>
                CommercialProfitabilityAlertAuditEventTypes.Dismissed,
            CommercialProfitabilityAlertLifecycleStatuses.Resolved =>
                CommercialProfitabilityAlertAuditEventTypes.Resolved,
            _ => "transition",
        };

    private async Task AppendAuditEventAsync(
        CommercialProfitabilityAlert alert,
        string eventType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var auditEvent = new CommercialProfitabilityAlertAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = alert.TenantId,
            FacilityId = alert.FacilityId,
            AlertId = alert.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            ActorSubjectId = actor,
            OccurredAtUtc = occurredAtUtc,
        };

        dbContext.CommercialProfitabilityAlertAuditEvents.Add(auditEvent);
        await Task.CompletedTask;
    }

    private static CommercialProfitabilityAlertSummaryReadModel MapSummary(CommercialProfitabilityAlert alert) =>
        new(
            alert.Id,
            alert.AlertReference,
            alert.AlertCategory,
            alert.Severity,
            alert.LifecycleStatus,
            alert.SourceComparisonId,
            alert.SourceComparisonType,
            alert.Title,
            alert.EvidenceSummary,
            alert.EvidenceStatus,
            alert.ComparisonOutcome,
            alert.LeadingDriverCode,
            alert.LinkedAiTaskId,
            alert.MaterializedAtUtc,
            alert.UpdatedAtUtc,
            alert.Version);

    private static CommercialProfitabilityAlertDetailReadModel MapDetail(
        CommercialProfitabilityAlert alert,
        CommercialProfitabilityComparisonDetailReadModel comparison) =>
        new(
            alert.Id,
            alert.AlertReference,
            alert.AlertCategory,
            alert.Severity,
            alert.LifecycleStatus,
            alert.SourceComparisonId,
            alert.SourceComparisonType,
            alert.Title,
            alert.EvidenceSummary,
            alert.EvidenceStatus,
            alert.ComparisonOutcome,
            alert.LeadingDriverCode,
            alert.SourceSnapshotJson,
            alert.LinkedAiTaskId,
            alert.RuleSetVersion,
            alert.SourceProvenance,
            alert.MaterializedAtUtc,
            alert.UpdatedAtUtc,
            alert.Version,
            comparison,
            alert.AuditEvents
                .OrderByDescending(entry => entry.OccurredAtUtc)
                .Select(entry => new CommercialProfitabilityAlertAuditEventReadModel(
                    entry.Id,
                    entry.EventType,
                    entry.PayloadJson,
                    entry.ActorSubjectId,
                    entry.OccurredAtUtc))
                .ToList());
}

public sealed class UnavailableCommercialProfitabilityAlertRepository : ICommercialProfitabilityAlertRepository
{
    private static CommercialProfitabilityAlertWriteResult Unavailable() =>
        CommercialProfitabilityAlertWriteResult.Fail("service-unavailable", "Commercial profitability alerts are unavailable.");

    public Task<CommercialProfitabilityAlertWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability alerts are unavailable.");

    public Task<IReadOnlyList<CommercialProfitabilityAlertSummaryReadModel>> ListAlertsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityAlertListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Commercial profitability alerts are unavailable.");

    public Task<CommercialProfitabilityAlertDetailReadModel?> GetAlertByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid alertId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CommercialProfitabilityAlertDetailReadModel?>(null);

    public Task<CommercialProfitabilityAlertWriteResult> MaterializeAlertsAsync(
        CommercialIntelligenceOperationContext context,
        MaterializeCommercialProfitabilityAlertsCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialProfitabilityAlertWriteResult> TransitionAlertAsync(
        CommercialIntelligenceOperationContext context,
        TransitionCommercialProfitabilityAlertCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<CommercialProfitabilityAlertWriteResult> LinkTaskAsync(
        CommercialIntelligenceOperationContext context,
        LinkCommercialProfitabilityAlertTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
