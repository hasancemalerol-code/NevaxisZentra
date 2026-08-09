using System.Text.Json;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Quality;

public interface IQualityInspectionRepository
{
    Task<QualityWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityInspectionSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default);

    Task<QualityInspectionDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityInspectionCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> StartAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> RecordResultAsync(
        QualityOperationContext context,
        RecordQualityInspectionResultCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CompleteAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> ReleaseAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> RejectAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class QualityInspectionRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IProductionOrderContextReader productionOrderContextReader) : IQualityInspectionRepository
{
    public async Task<QualityWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var inspections = await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.QualityInspections.AsNoTracking()
                .Where(i => i.TenantId == context.TenantId && i.FacilityId == context.FacilityId)
                .Select(i => new { i.Status, i.OverallResult, i.ReleaseStatus })
                .ToListAsync(ct),
            cancellationToken);

        return new QualityWorkspaceSummaryReadModel(
            inspections.Count(i => i.Status == QualityInspectionStatus.InProgress || i.Status == QualityInspectionStatus.Draft),
            inspections.Count(i => i.ReleaseStatus == QualityInspectionReleaseStatus.ReadyForReview),
            inspections.Count(i => i.OverallResult == QualityInspectionOverallResult.Fail),
            inspections.Count(i => i.ReleaseStatus == QualityInspectionReleaseStatus.Released));
    }

    public async Task<IReadOnlyList<QualityInspectionSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default)
    {
        return await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.QualityInspections.AsNoTracking()
                .Where(i => i.TenantId == context.TenantId && i.FacilityId == context.FacilityId)
                .OrderByDescending(i => i.UpdatedAtUtc)
                .Join(
                    dbContext.QualityControlPlans.AsNoTracking(),
                    i => i.QualityControlPlanId,
                    p => p.Id,
                    (i, p) => new { Inspection = i, Plan = p })
                .Join(
                    dbContext.ProductionOrders.AsNoTracking(),
                    x => x.Inspection.ProductionOrderId,
                    o => o.Id,
                    (x, o) => new QualityInspectionSummaryReadModel(
                        x.Inspection.Id,
                        x.Inspection.InspectionNumber,
                        QualityValueConversions.ToStorage(x.Inspection.Status),
                        QualityValueConversions.ToStorage(x.Inspection.OverallResult),
                        QualityValueConversions.ToStorage(x.Inspection.ReleaseStatus),
                        x.Inspection.ProductionOrderId,
                        o.ReferenceCode,
                        o.ProductReference,
                        x.Inspection.QualityControlPlanId,
                        x.Plan.Code,
                        x.Inspection.InspectorSubjectId,
                        x.Inspection.StartedAtUtc,
                        x.Inspection.CompletedAtUtc,
                        x.Inspection.Version,
                        x.Inspection.UpdatedAtUtc))
                .ToListAsync(ct),
            cancellationToken);
    }

    public Task<QualityInspectionDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var inspection = await dbContext.QualityInspections.AsNoTracking()
                .Include(i => i.Results)
                .SingleOrDefaultAsync(
                    i => i.TenantId == context.TenantId
                        && i.FacilityId == context.FacilityId
                        && i.Id == inspectionId,
                    ct);

            if (inspection is null)
            {
                return null;
            }

            var plan = await dbContext.QualityControlPlans.AsNoTracking()
                .Include(p => p.InspectionDefinitions)
                .SingleAsync(p => p.Id == inspection.QualityControlPlanId, ct);

            var production = await productionOrderContextReader.ReadAsync(
                context, inspection.ProductionOrderId, ct);

            if (production is null)
            {
                production = new ProductionOrderQualityContext(
                    inspection.ProductionOrderId, "unavailable", null, "unavailable", null, 0, 0, false);
            }

            var results = plan.InspectionDefinitions
                .OrderBy(d => d.Sequence)
                .Select(def =>
                {
                    var recorded = inspection.Results.SingleOrDefault(r => r.InspectionDefinitionId == def.Id);
                    return new QualityInspectionResultReadModel(
                        recorded?.Id ?? Guid.Empty,
                        def.Id,
                        def.Code,
                        def.Name,
                        QualityValueConversions.ToStorage(def.InspectionType),
                        def.Unit,
                        def.NominalValue,
                        def.LowerLimit,
                        def.UpperLimit,
                        def.Required,
                        def.Critical,
                        recorded?.MeasuredNumericValue,
                        recorded?.MeasuredTextValue,
                        recorded?.BooleanResult,
                        recorded is null
                            ? QualityValueConversions.ToStorage(QualityInspectionItemResult.Pending)
                            : QualityValueConversions.ToStorage(recorded.Result),
                        recorded?.Comment,
                        recorded?.RecordedAtUtc ?? default,
                        recorded?.RecordedBySubjectId ?? string.Empty);
                })
                .ToList();

            return new QualityInspectionDetailReadModel(
                inspection.Id,
                inspection.InspectionNumber,
                QualityValueConversions.ToStorage(inspection.Status),
                QualityValueConversions.ToStorage(inspection.OverallResult),
                QualityValueConversions.ToStorage(inspection.ReleaseStatus),
                inspection.ProductionOrderId,
                production,
                inspection.QualityControlPlanId,
                plan.Code,
                plan.Name,
                inspection.InspectorSubjectId,
                inspection.Notes,
                inspection.ReleaseNote,
                inspection.StartedAtUtc,
                inspection.CompletedAtUtc,
                inspection.ReleasedAtUtc,
                inspection.Version,
                results);
        }, cancellationToken);

    public Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityInspectionCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var production = await productionOrderContextReader.ReadAsync(context, command.ProductionOrderId, ct);
            if (production is null || !production.Available)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.ProductionOrderNotFound,
                    "Production order not found in facility scope.");
            }

            var plan = await dbContext.QualityControlPlans
                .Include(p => p.InspectionDefinitions)
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == command.QualityControlPlanId,
                    ct);

            if (plan is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality control plan not found.");
            }

            if (plan.Status != QualityControlPlanStatus.Active)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.PlanNotActive, "Quality control plan must be active.");
            }

            var now = DateTimeOffset.UtcNow;
            var inspectionId = Guid.NewGuid();
            var number = await NextInspectionNumberAsync(context, ct);
            var inspection = new QualityInspection
            {
                Id = inspectionId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ProductionOrderId = command.ProductionOrderId,
                QualityControlPlanId = plan.Id,
                InspectionNumber = number,
                Status = QualityInspectionStatus.Draft,
                OverallResult = QualityInspectionOverallResult.Pending,
                ReleaseStatus = QualityInspectionReleaseStatus.NotReady,
                Notes = command.Notes?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                Version = 1,
            };

            dbContext.QualityInspections.Add(inspection);
            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(inspectionId, inspection.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> StartAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        MutateInspectionAsync(context, command, QualityInspectionStatus.Draft, (inspection, now) =>
        {
            inspection.Status = QualityInspectionStatus.InProgress;
            inspection.StartedAtUtc = now;
            inspection.InspectorSubjectId = context.IdentitySubjectId.ToString();
            AddAudit(context, QualityAuditActions.InspectionStarted, QualityAuditEventTypes.InspectionStarted,
                inspection.Id, now, new { inspectionId = inspection.Id });
        }, cancellationToken);

    public Task<QualityWriteResult> RecordResultAsync(
        QualityOperationContext context,
        RecordQualityInspectionResultCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var inspection = await LoadInspectionForWriteAsync(context, command.InspectionId, ct);
            if (inspection is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality inspection not found.");
            }

            if (inspection.Version != command.ExpectedVersion)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.ConcurrencyConflict,
                    $"The quality inspection was modified (have {inspection.Version}, expected {command.ExpectedVersion}).");
            }

            if (inspection.Status != QualityInspectionStatus.InProgress)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.InvalidTransition,
                    "Results can only be recorded while inspection is in progress.");
            }

            var plan = await dbContext.QualityControlPlans
                .Include(p => p.InspectionDefinitions)
                .SingleAsync(p => p.Id == inspection.QualityControlPlanId, ct);

            var definition = plan.InspectionDefinitions
                .SingleOrDefault(d => d.Id == command.InspectionDefinitionId);

            if (definition is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Inspection definition not found on plan.");
            }

            var evaluated = QualityEvaluationEngine.EvaluateDefinitionResult(
                definition,
                command.MeasuredNumericValue,
                command.MeasuredTextValue,
                command.BooleanResult);

            var now = DateTimeOffset.UtcNow;
            var existing = await dbContext.QualityInspectionResults
                .SingleOrDefaultAsync(
                    r => r.QualityInspectionId == inspection.Id
                        && r.InspectionDefinitionId == definition.Id,
                    ct);

            QualityInspectionResult resultRow;
            if (existing is null)
            {
                resultRow = new QualityInspectionResult
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    QualityInspectionId = inspection.Id,
                    InspectionDefinitionId = definition.Id,
                    Version = 1,
                };
                dbContext.QualityInspectionResults.Add(resultRow);
            }
            else
            {
                resultRow = existing;
                resultRow.Version += 1;
            }

            resultRow.MeasuredNumericValue = command.MeasuredNumericValue;
            resultRow.MeasuredTextValue = command.MeasuredTextValue?.Trim();
            resultRow.BooleanResult = command.BooleanResult;
            resultRow.Result = evaluated;
            resultRow.Comment = command.Comment?.Trim();
            resultRow.RecordedAtUtc = now;
            resultRow.RecordedBySubjectId = context.IdentitySubjectId.ToString();
            resultRow.Source = "manual";

            var recordedResults = await dbContext.QualityInspectionResults
                .Where(r => r.QualityInspectionId == inspection.Id)
                .ToListAsync(ct);
            if (existing is null)
            {
                recordedResults.Add(resultRow);
            }

            inspection.OverallResult = ComputeOverall(recordedResults, plan);
            inspection.ReleaseStatus = QualityInspectionReleaseStatus.NotReady;
            inspection.UpdatedAtUtc = now;
            inspection.Version += 1;

            AddAudit(context, QualityAuditActions.ResultRecorded, QualityAuditEventTypes.ResultRecorded,
                inspection.Id, now, new
                {
                    inspectionId = inspection.Id,
                    definitionId = definition.Id,
                    result = QualityValueConversions.ToStorage(evaluated),
                });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(inspection.Id, inspection.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> CompleteAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var inspection = await LoadInspectionForWriteAsync(context, command.InspectionId, ct);
            if (inspection is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality inspection not found.");
            }

            if (inspection.Version != command.ExpectedVersion)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.ConcurrencyConflict,
                    $"The quality inspection was modified (have {inspection.Version}, expected {command.ExpectedVersion}).");
            }

            if (inspection.Status != QualityInspectionStatus.InProgress)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.InvalidTransition,
                    "Only in-progress inspections can be completed.");
            }

            var plan = await dbContext.QualityControlPlans
                .Include(p => p.InspectionDefinitions)
                .SingleAsync(p => p.Id == inspection.QualityControlPlanId, ct);

            var recordedResults = await dbContext.QualityInspectionResults
                .Where(r => r.QualityInspectionId == inspection.Id)
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;
            inspection.OverallResult = ComputeOverall(recordedResults, plan);
            inspection.Status = QualityInspectionStatus.Completed;
            inspection.CompletedAtUtc = now;
            inspection.ReleaseStatus = QualityEvaluationEngine.ComputeReleaseStatusAfterCompletion(
                inspection.OverallResult,
                inspection.ReleaseStatus);
            inspection.UpdatedAtUtc = now;
            inspection.Version += 1;

            AddAudit(context, QualityAuditActions.InspectionCompleted, QualityAuditEventTypes.InspectionCompleted,
                inspection.Id, now, new
                {
                    inspectionId = inspection.Id,
                    overallResult = QualityValueConversions.ToStorage(inspection.OverallResult),
                });

            AddOutbox(context, inspection, QualityOutboxMessageTypes.InspectionCompleted, now, new
            {
                inspectionId = inspection.Id.ToString(),
                overallResult = QualityValueConversions.ToStorage(inspection.OverallResult),
            });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(inspection.Id, inspection.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> ReleaseAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ReleaseDecisionAsync(context, command, released: true, cancellationToken);

    public Task<QualityWriteResult> RejectAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ReleaseDecisionAsync(context, command, released: false, cancellationToken);

    private Task<QualityWriteResult> MutateInspectionAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        QualityInspectionStatus requiredStatus,
        Action<QualityInspection, DateTimeOffset> mutate,
        CancellationToken cancellationToken) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var inspection = await LoadInspectionForWriteAsync(context, command.InspectionId, ct);
            if (inspection is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality inspection not found.");
            }

            if (inspection.Version != command.ExpectedVersion)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.ConcurrencyConflict,
                    $"The quality inspection was modified (have {inspection.Version}, expected {command.ExpectedVersion}).");
            }

            if (inspection.Status != requiredStatus)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.InvalidTransition,
                    $"Inspection must be in '{QualityValueConversions.ToStorage(requiredStatus)}' status.");
            }

            var now = DateTimeOffset.UtcNow;
            mutate(inspection, now);
            inspection.UpdatedAtUtc = now;
            inspection.Version += 1;
            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(inspection.Id, inspection.Version);
        }, cancellationToken);

    private Task<QualityWriteResult> ReleaseDecisionAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        bool released,
        CancellationToken cancellationToken) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var inspection = await LoadInspectionForWriteAsync(context, command.InspectionId, ct);
            if (inspection is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality inspection not found.");
            }

            if (inspection.Version != command.ExpectedVersion)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.ConcurrencyConflict,
                    $"The quality inspection was modified (have {inspection.Version}, expected {command.ExpectedVersion}).");
            }

            if (inspection.Status != QualityInspectionStatus.Completed)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.InvalidTransition,
                    "Inspection must be completed before release decision.");
            }

            if (inspection.ReleaseStatus is QualityInspectionReleaseStatus.Released or QualityInspectionReleaseStatus.Rejected)
            {
                return QualityWriteResult.Fail(
                    QualityFailureCodes.InvalidTransition,
                    "Release decision has already been recorded.");
            }

            if (released && inspection.OverallResult == QualityInspectionOverallResult.Fail)
            {
                var plan = await dbContext.QualityControlPlans
                    .Include(p => p.InspectionDefinitions)
                    .SingleAsync(p => p.Id == inspection.QualityControlPlanId, ct);

                var recordedResults = await dbContext.QualityInspectionResults
                    .Where(r => r.QualityInspectionId == inspection.Id)
                    .ToListAsync(ct);

                var hasCriticalFail = recordedResults
                    .Join(plan.InspectionDefinitions, r => r.InspectionDefinitionId, d => d.Id, (r, d) => new { r, d })
                    .Any(x => x.d.Required && x.d.Critical && x.r.Result == QualityInspectionItemResult.Fail);

                if (hasCriticalFail)
                {
                    return QualityWriteResult.Fail(
                        QualityFailureCodes.ReleaseNotAllowed,
                        "Failed critical requirements cannot be silently released.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            inspection.ReleaseStatus = released
                ? QualityInspectionReleaseStatus.Released
                : QualityInspectionReleaseStatus.Rejected;
            inspection.ReleaseNote = command.Note?.Trim();
            inspection.ReleasedBySubjectId = context.IdentitySubjectId.ToString();
            inspection.ReleasedAtUtc = now;
            inspection.UpdatedAtUtc = now;
            inspection.Version += 1;

            var action = released ? QualityAuditActions.ReleaseReleased : QualityAuditActions.ReleaseRejected;
            var eventType = released ? QualityAuditEventTypes.ReleaseReleased : QualityAuditEventTypes.ReleaseRejected;
            var outboxType = released ? QualityOutboxMessageTypes.ReleaseReleased : QualityOutboxMessageTypes.ReleaseRejected;

            AddAudit(context, action, eventType, inspection.Id, now, new { inspectionId = inspection.Id, released });
            AddOutbox(context, inspection, outboxType, now, new { inspectionId = inspection.Id.ToString(), released });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(inspection.Id, inspection.Version);
        }, cancellationToken);

    private async Task<QualityInspection?> LoadInspectionForWriteAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken ct) =>
        await dbContext.QualityInspections
            .SingleOrDefaultAsync(
                i => i.TenantId == context.TenantId
                    && i.FacilityId == context.FacilityId
                    && i.Id == inspectionId,
                ct);

    private static QualityInspectionOverallResult ComputeOverall(
        IReadOnlyList<QualityInspectionResult> recordedResults,
        QualityControlPlan plan)
    {
        var items = plan.InspectionDefinitions.Select(def =>
        {
            var recorded = recordedResults.SingleOrDefault(r => r.InspectionDefinitionId == def.Id);
            var result = recorded?.Result ?? QualityInspectionItemResult.Pending;
            return (def, result);
        });
        return QualityEvaluationEngine.ComputeOverallResult(items);
    }

    private async Task<string> NextInspectionNumberAsync(
        QualityOperationContext context,
        CancellationToken ct)
    {
        var count = await dbContext.QualityInspections
            .CountAsync(i => i.TenantId == context.TenantId && i.FacilityId == context.FacilityId, ct);
        return $"QI-{DateTimeOffset.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    private async Task<QualityWriteResult> ExecuteWriteAsync(
        QualityOperationContext context,
        Func<CancellationToken, Task<QualityWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return QualityWriteResult.Fail(QualityFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return QualityWriteResult.Fail(
                QualityFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return QualityWriteResult.Fail(
                QualityFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return QualityWriteResult.Fail(QualityFailureCodes.SourceUnavailable, "Quality service is unavailable.");
        }
    }

    private void AddAudit(
        QualityOperationContext context,
        string action,
        string eventType,
        Guid inspectionId,
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
            ResourceType = "quality.inspection",
            ResourceId = inspectionId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata),
        });
    }

    private void AddOutbox(
        QualityOperationContext context,
        QualityInspection inspection,
        string messageType,
        DateTimeOffset now,
        object payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = "quality-inspection",
            AggregateId = inspection.Id.ToString(),
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            CausationId = null,
            IdempotencyKey = $"{inspection.Id}:{inspection.Version}:{messageType}",
            PayloadJson = JsonSerializer.Serialize(payload),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class UnavailableQualityInspectionRepository : IQualityInspectionRepository
{
    public Task<QualityWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityWorkspaceSummaryReadModel(0, 0, 0, 0));

    public Task<IReadOnlyList<QualityInspectionSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityInspectionSummaryReadModel>>([]);

    public Task<QualityInspectionDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid inspectionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityInspectionDetailReadModel?>(null);

    public Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityInspectionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality inspection service is unavailable."));

    public Task<QualityWriteResult> StartAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality inspection service is unavailable."));

    public Task<QualityWriteResult> RecordResultAsync(
        QualityOperationContext context,
        RecordQualityInspectionResultCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality inspection service is unavailable."));

    public Task<QualityWriteResult> CompleteAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality inspection service is unavailable."));

    public Task<QualityWriteResult> ReleaseAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality inspection service is unavailable."));

    public Task<QualityWriteResult> RejectAsync(
        QualityOperationContext context,
        QualityInspectionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(QualityWriteResult.Fail(
            QualityFailureCodes.SourceUnavailable,
            "Quality inspection service is unavailable."));
}
