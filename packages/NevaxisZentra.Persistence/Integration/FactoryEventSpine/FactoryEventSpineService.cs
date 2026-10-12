using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.FactoryOrchestration;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Integration.FactoryEventSpine;

public interface IFactoryEventSpineService
{
    Task<FactoryEventIngestionResult> IngestAsync(
        IntegrationOperationContext context,
        FactoryEventIngestionRequest request,
        CancellationToken cancellationToken);

    Task<FactoryEventSpineProcessingResult> ProcessPendingIngestionAsync(
        IntegrationOperationContext context,
        int batchSize,
        CancellationToken cancellationToken);

    Task<FactoryEventSpineProcessingResult> DispatchPendingEventsAsync(
        IntegrationOperationContext context,
        int batchSize,
        string subjectId,
        CancellationToken cancellationToken);

    Task<FactoryEventIngestionResult> ReprocessQuarantinedInboundAsync(
        IntegrationOperationContext context,
        Guid inboundEventId,
        CancellationToken cancellationToken);

    Task<FactoryEventIngestionResult> ReplayCanonicalEventAsync(
        IntegrationOperationContext context,
        Guid canonicalEventId,
        string subjectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FactoryLiveEventMonitorItem>> ListRecentEventsAsync(
        IntegrationOperationContext context,
        int limit,
        CancellationToken cancellationToken);

    Task<FactoryConnectorOperationsSnapshot?> GetConnectorOperationsAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken);
}

public sealed class FactoryEventSpineService(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IFactoryEventRouter factoryEventRouter) : IFactoryEventSpineService
{
    private const int DefaultBatchSize = 50;

    public Task<FactoryEventIngestionResult> IngestAsync(
        IntegrationOperationContext context,
        FactoryEventIngestionRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => IngestCoreAsync(context, request, ct),
            cancellationToken);

    public Task<FactoryEventSpineProcessingResult> ProcessPendingIngestionAsync(
        IntegrationOperationContext context,
        int batchSize,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ProcessPendingIngestionCoreAsync(context, batchSize, ct),
            cancellationToken);

    public Task<FactoryEventSpineProcessingResult> DispatchPendingEventsAsync(
        IntegrationOperationContext context,
        int batchSize,
        string subjectId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => DispatchPendingEventsCoreAsync(context, batchSize, subjectId, ct),
            cancellationToken);

    public Task<FactoryEventIngestionResult> ReprocessQuarantinedInboundAsync(
        IntegrationOperationContext context,
        Guid inboundEventId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ReprocessQuarantinedInboundCoreAsync(context, inboundEventId, ct),
            cancellationToken);

    public Task<FactoryEventIngestionResult> ReplayCanonicalEventAsync(
        IntegrationOperationContext context,
        Guid canonicalEventId,
        string subjectId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ReplayCanonicalEventCoreAsync(context, canonicalEventId, subjectId, ct),
            cancellationToken);

    public Task<IReadOnlyList<FactoryLiveEventMonitorItem>> ListRecentEventsAsync(
        IntegrationOperationContext context,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListRecentEventsCoreAsync(context, limit, ct),
            cancellationToken);

    public Task<FactoryConnectorOperationsSnapshot?> GetConnectorOperationsAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetConnectorOperationsCoreAsync(context, connectorId, ct),
            cancellationToken);

    private async Task<FactoryEventIngestionResult> IngestCoreAsync(
        IntegrationOperationContext context,
        FactoryEventIngestionRequest request,
        CancellationToken cancellationToken)
    {
        var connector = await LoadConnectorAsync(context, request.ConnectorId, cancellationToken);
        if (connector is null)
        {
            return new FactoryEventIngestionResult(false, false, null, null, IntegrationFailureCodes.NotFound, "Bağlantı bulunamadı.");
        }

        if (request.RawPayloadJson.Length > 1024 * 1024)
        {
            return new FactoryEventIngestionResult(false, false, null, null, IntegrationFailureCodes.ValidationFailed, "Payload boyutu limiti aşıldı.");
        }

        var sourceEventId = request.SourceEventId.Trim();
        var idempotencyKey = request.IdempotencyKeyOverride
            ?? FactoryIntegrationNormalizer.BuildIdempotencyKey(context.TenantId, request.ConnectorId, sourceEventId);
        var payloadHash = FactoryIntegrationNormalizer.ComputePayloadHash(request.RawPayloadJson);

        var existing = await dbContext.IntegrationInboundFactoryEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == context.TenantId && e.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            return new FactoryEventIngestionResult(true, true, existing.Id, existing.NormalizedEventId, null, null);
        }

        var now = DateTimeOffset.UtcNow;
        var inbound = new IntegrationInboundFactoryEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectorId = request.ConnectorId,
            SourceEventId = sourceEventId,
            IdempotencyKey = idempotencyKey,
            PayloadHash = payloadHash,
            RawPayloadJson = request.RawPayloadJson,
            ProcessingStatus = FactoryInboundEventStatuses.Received,
            CorrelationId = Guid.NewGuid(),
            ReceivedAtUtc = now,
            UpdatedAtUtc = now,
        };

        dbContext.IntegrationInboundFactoryEvents.Add(inbound);
        connector.LastAttemptedSyncAtUtc = now;
        connector.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FactoryEventIngestionResult(true, false, inbound.Id, null, null, null);
    }

    private async Task<FactoryEventSpineProcessingResult> ProcessPendingIngestionCoreAsync(
        IntegrationOperationContext context,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var batch = await dbContext.IntegrationInboundFactoryEvents
            .Where(e => e.TenantId == context.TenantId &&
                        e.FacilityId == context.FacilityId &&
                        (e.ProcessingStatus == FactoryInboundEventStatuses.Received ||
                         (e.ProcessingStatus == FactoryInboundEventStatuses.Failed &&
                          e.NextRetryAtUtc <= now)))
            .OrderBy(e => e.ReceivedAtUtc)
            .Take(Math.Min(batchSize, DefaultBatchSize))
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;
        var quarantined = 0;

        foreach (var inbound in batch)
        {
            inbound.AttemptCount++;
            inbound.ProcessingStatus = FactoryInboundEventStatuses.Validating;
            inbound.UpdatedAtUtc = now;

            try
            {
                var connector = await LoadConnectorAsync(context, inbound.ConnectorId, cancellationToken)
                    ?? throw new InvalidOperationException("Bağlantı bulunamadı.");

                var contract = FactoryIntegrationNormalizer.Normalize(
                    inbound.RawPayloadJson,
                    connector.SourceSystemName);

                var quarantineReason = ValidateContract(contract);
                if (quarantineReason is not null)
                {
                    inbound.ProcessingStatus = FactoryInboundEventStatuses.Quarantined;
                    inbound.ValidationState = "invalid";
                    inbound.QuarantineId = await CreateQuarantineAsync(context, inbound, connector, quarantineReason, cancellationToken);
                    quarantined++;
                    continue;
                }

                var canonical = BuildCanonicalEvent(inbound, connector, contract, now);
                dbContext.IntegrationCanonicalFactoryEvents.Add(canonical);
                dbContext.IntegrationEventDispatchOutbox.Add(new IntegrationEventDispatchOutbox
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    CanonicalEventId = canonical.Id,
                    ConsumerName = FactoryEventSpineConsumerNames.FactoryEventRouter,
                    Status = FactoryCanonicalEventStates.DispatchPending,
                    CreatedAtUtc = now,
                });

                inbound.NormalizedEventId = canonical.Id;
                inbound.ProcessingStatus = FactoryInboundEventStatuses.Normalized;
                inbound.ValidationState = "valid";
                inbound.ProcessedAtUtc = now;
                connector.LastSuccessfulSyncAtUtc = now;
                connector.LastSuccessAtUtc = now;
                connector.HealthStatus = IntegrationHealthStatuses.Healthy;
                processed++;
            }
            catch (JsonException ex)
            {
                inbound.ProcessingStatus = FactoryInboundEventStatuses.Failed;
                inbound.LastError = ex.Message;
                inbound.ErrorClassification = FactoryEventErrorClassifications.Data;
                inbound.NextRetryAtUtc = now.AddMinutes(Math.Min(30, inbound.AttemptCount * 2));
                failed++;
            }
            catch (Exception ex)
            {
                inbound.ProcessingStatus = FactoryInboundEventStatuses.Failed;
                inbound.LastError = ex.Message;
                inbound.ErrorClassification = FactoryEventErrorClassifications.Transient;
                inbound.NextRetryAtUtc = now.AddMinutes(Math.Min(30, inbound.AttemptCount * 2));
                failed++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new FactoryEventSpineProcessingResult(processed, failed, quarantined, 0);
    }

    private async Task<FactoryEventSpineProcessingResult> DispatchPendingEventsCoreAsync(
        IntegrationOperationContext context,
        int batchSize,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var events = await dbContext.IntegrationCanonicalFactoryEvents
            .Where(e => e.TenantId == context.TenantId &&
                        e.FacilityId == context.FacilityId &&
                        (e.ProcessingState == FactoryCanonicalEventStates.DispatchPending ||
                         (e.ProcessingState == FactoryCanonicalEventStates.Failed &&
                          e.NextRetryAtUtc <= now) ||
                         // A runner that stopped mid-event leaves it Processing; the consumer receipt below keeps the
                         // re-run from routing the same event twice.
                         (e.ProcessingState == FactoryCanonicalEventStates.Processing &&
                          e.UpdatedAtUtc <= now.AddMinutes(-5))))
            .OrderBy(e => e.ReceivedAtUtc)
            .Take(Math.Min(batchSize, DefaultBatchSize))
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;

        foreach (var canonical in events)
        {
            var processingKey = $"{canonical.Id:N}:{FactoryEventSpineConsumerNames.FactoryEventRouter}";
            var receiptExists = await dbContext.IntegrationEventConsumerReceipts.AnyAsync(
                r => r.TenantId == context.TenantId && r.ProcessingKey == processingKey,
                cancellationToken);
            if (receiptExists)
            {
                canonical.ProcessingState = FactoryCanonicalEventStates.Processed;
                canonical.UpdatedAtUtc = now;
                processed++;
                continue;
            }

            canonical.AttemptCount++;
            canonical.ProcessingState = FactoryCanonicalEventStates.Processing;
            canonical.UpdatedAtUtc = now;

            try
            {
                var contract = DeserializeContract(canonical);
                if (!FactoryEventOrderingGuard.ShouldApply(canonical, await GetLatestSequenceAsync(context, canonical, cancellationToken)))
                {
                    canonical.ProcessingState = FactoryCanonicalEventStates.Processed;
                    canonical.LastError = "Out-of-order event skipped.";
                    processed++;
                    continue;
                }

                var connector = await LoadConnectorAsync(context, canonical.ConnectorId, cancellationToken)
                    ?? throw new InvalidOperationException("Bağlantı bulunamadı.");

                var domainEvent = FactoryIntegrationNormalizer.ToFactoryDomainEvent(
                    contract,
                    context.FacilityId,
                    connector.ConnectorKind,
                    connector.SourceSystemType);

                var routeResult = await factoryEventRouter.RouteAsync(domainEvent, subjectId, cancellationToken);

                canonical.RoutedCaseId = routeResult.Case.Id;
                canonical.RoutedCorrelationKey = routeResult.CorrelationKey;
                canonical.ProcessingState = FactoryCanonicalEventStates.Processed;
                canonical.UpdatedAtUtc = now;

                dbContext.IntegrationEventConsumerReceipts.Add(new IntegrationEventConsumerReceipt
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    CanonicalEventId = canonical.Id,
                    ConsumerName = FactoryEventSpineConsumerNames.FactoryEventRouter,
                    ProcessingKey = processingKey,
                    Result = "processed",
                    ProcessedAtUtc = now,
                });

                var inbound = await dbContext.IntegrationInboundFactoryEvents
                    .FirstOrDefaultAsync(e => e.Id == canonical.InboundEventId, cancellationToken);
                if (inbound is not null)
                {
                    inbound.ProcessingStatus = FactoryInboundEventStatuses.Processed;
                    inbound.UpdatedAtUtc = now;
                }

                processed++;
            }
            catch (FactoryOrchestrationException ex) when (ex.FailureCode == FactoryOrchestrationFailureCodes.EventNotQualified)
            {
                canonical.ProcessingState = FactoryCanonicalEventStates.Processed;
                canonical.LastError = ex.Message;
                processed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Whatever the failed event half-wrote (a case, a receipt) must not be saved with its failure marker.
                DiscardPendingChangesExcept(canonical);
                canonical.ProcessingState = canonical.AttemptCount >= canonical.MaxAttempts
                    ? FactoryCanonicalEventStates.DeadLetter
                    : FactoryCanonicalEventStates.Failed;
                canonical.LastError = ex.GetType().Name;
                canonical.ErrorClassification = FactoryEventErrorClassifications.Transient;
                canonical.NextRetryAtUtc = now.AddMinutes(Math.Min(30, canonical.AttemptCount * 2));
                failed++;
            }

            // Each event is its own unit of work inside the batch transaction, so one event's fault cannot carry
            // another event's pending changes with it.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new FactoryEventSpineProcessingResult(processed, failed, 0, 0);
    }

    private void DiscardPendingChangesExcept(object keep)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            if (ReferenceEquals(entry.Entity, keep))
            {
                continue;
            }

            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    break;
            }
        }
    }

    private async Task<FactoryEventIngestionResult> ReprocessQuarantinedInboundCoreAsync(
        IntegrationOperationContext context,
        Guid inboundEventId,
        CancellationToken cancellationToken)
    {
        var inbound = await dbContext.IntegrationInboundFactoryEvents
            .FirstOrDefaultAsync(
                e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId && e.Id == inboundEventId,
                cancellationToken);
        if (inbound is null)
        {
            return new FactoryEventIngestionResult(false, false, null, null, IntegrationFailureCodes.NotFound, "Kayıt bulunamadı.");
        }

        inbound.ProcessingStatus = FactoryInboundEventStatuses.Received;
        inbound.LastError = null;
        inbound.NextRetryAtUtc = null;
        inbound.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await ProcessPendingIngestionCoreAsync(context, 1, cancellationToken);
        return new FactoryEventIngestionResult(true, false, inbound.Id, inbound.NormalizedEventId, null, null);
    }

    private async Task<FactoryEventIngestionResult> ReplayCanonicalEventCoreAsync(
        IntegrationOperationContext context,
        Guid canonicalEventId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var canonical = await dbContext.IntegrationCanonicalFactoryEvents
            .FirstOrDefaultAsync(
                e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId && e.Id == canonicalEventId,
                cancellationToken);
        if (canonical is null)
        {
            return new FactoryEventIngestionResult(false, false, null, null, IntegrationFailureCodes.NotFound, "Olay bulunamadı.");
        }

        canonical.ProcessingState = FactoryCanonicalEventStates.DispatchPending;
        canonical.AttemptCount = 0;
        canonical.LastError = null;
        canonical.NextRetryAtUtc = null;
        canonical.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await DispatchPendingEventsCoreAsync(context, 1, subjectId, cancellationToken);
        return new FactoryEventIngestionResult(true, false, canonical.InboundEventId, canonical.Id, null, null);
    }

    private async Task<IReadOnlyList<FactoryLiveEventMonitorItem>> ListRecentEventsCoreAsync(
        IntegrationOperationContext context,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.IntegrationCanonicalFactoryEvents
            .AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId)
            .OrderByDescending(e => e.ReceivedAtUtc)
            .Take(Math.Min(limit, 100))
            .ToListAsync(cancellationToken);

        return rows.Select(e => new FactoryLiveEventMonitorItem(
            e.Id,
            e.InboundEventId,
            e.SourceSystem,
            e.EventType,
            e.MachineId,
            e.OrderId,
            e.OccurredAtUtc,
            e.ReceivedAtUtc,
            e.ProcessingState,
            e.RoutedCorrelationKey,
            e.RoutedCaseId)).ToList();
    }

    private async Task<FactoryConnectorOperationsSnapshot?> GetConnectorOperationsCoreAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken)
    {
        var connector = await LoadConnectorAsync(context, connectorId, cancellationToken);
        if (connector is null)
        {
            return null;
        }

        // DateTimeOffset.UtcNow.Date is a DateTime of Unspecified kind: comparing it with a DateTimeOffset column
        // converts it with the machine's local offset, and Npgsql rejects any non-zero offset (HTTP 500 on every
        // machine outside UTC, i.e. every Turkish installation).
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var processedToday = await dbContext.IntegrationCanonicalFactoryEvents.CountAsync(
            e => e.TenantId == context.TenantId &&
                 e.ConnectorId == connectorId &&
                 e.ProcessingState == FactoryCanonicalEventStates.Processed &&
                 e.UpdatedAtUtc >= today,
            cancellationToken);
        var quarantinedToday = await dbContext.IntegrationInboundFactoryEvents.CountAsync(
            e => e.TenantId == context.TenantId &&
                 e.ConnectorId == connectorId &&
                 e.ProcessingStatus == FactoryInboundEventStatuses.Quarantined &&
                 e.UpdatedAtUtc >= today,
            cancellationToken);
        var failedToday = await dbContext.IntegrationCanonicalFactoryEvents.CountAsync(
            e => e.TenantId == context.TenantId &&
                 e.ConnectorId == connectorId &&
                 (e.ProcessingState == FactoryCanonicalEventStates.Failed ||
                  e.ProcessingState == FactoryCanonicalEventStates.DeadLetter) &&
                 e.UpdatedAtUtc >= today,
            cancellationToken);

        var lastEvent = await dbContext.IntegrationInboundFactoryEvents
            .AsNoTracking()
            .Where(e => e.TenantId == context.TenantId && e.ConnectorId == connectorId)
            .OrderByDescending(e => e.ReceivedAtUtc)
            .Select(e => e.ReceivedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        TimeSpan? delay = lastEvent == default
            ? null
            : DateTimeOffset.UtcNow - lastEvent;
        var (health, summary) = FactoryIntegrationHealthEngine.Evaluate(connector, failedToday, delay);

        return new FactoryConnectorOperationsSnapshot(
            connector.Id,
            connector.Name,
            connector.ConnectorKind,
            health,
            summary,
            connector.LastSuccessfulSyncAtUtc,
            lastEvent == default ? null : lastEvent,
            processedToday,
            quarantinedToday,
            failedToday,
            connector.CheckpointJson,
            connector.FailureStatus);
    }

    private static IntegrationCanonicalFactoryEvent BuildCanonicalEvent(
        IntegrationInboundFactoryEvent inbound,
        IntegrationConnector connector,
        FactoryCanonicalEventContract contract,
        DateTimeOffset now)
    {
        var isLate = now - contract.OccurredAtUtc > TimeSpan.FromHours(4);
        return new IntegrationCanonicalFactoryEvent
        {
            Id = Guid.NewGuid(),
            TenantId = inbound.TenantId,
            FacilityId = inbound.FacilityId,
            ConnectorId = inbound.ConnectorId,
            InboundEventId = inbound.Id,
            EventType = contract.EventType,
            EventVersion = contract.EventVersion,
            SourceSystem = connector.SourceSystemName,
            SourceEventId = contract.SourceEventId,
            IdempotencyKey = inbound.IdempotencyKey,
            OccurredAtUtc = contract.OccurredAtUtc,
            ReceivedAtUtc = inbound.ReceivedAtUtc,
            MachineId = contract.MachineId,
            OrderId = contract.OrderId,
            MaterialId = contract.MaterialId,
            MoldId = contract.MoldId,
            ProductionLineId = contract.ProductionLineId,
            ShiftId = contract.ShiftId,
            MeasurementsJson = JsonSerializer.Serialize(contract.Measurements),
            BusinessAttributesJson = JsonSerializer.Serialize(contract.BusinessAttributes),
            NormalizedPayloadJson = inbound.RawPayloadJson,
            CorrelationKeysJson = JsonSerializer.Serialize(
                FactoryIntegrationNormalizer.BuildCorrelationKeys(contract, inbound.FacilityId)),
            ProcessingState = FactoryCanonicalEventStates.DispatchPending,
            ValidationState = "valid",
            IsLateEvent = isLate,
            SourceSequence = contract.BusinessAttributes.TryGetValue("sequence", out var seq) &&
                             long.TryParse(seq, out var parsed)
                ? parsed
                : null,
            Severity = contract.Severity,
            Title = contract.Title,
            Description = contract.Description,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    private static string? ValidateContract(FactoryCanonicalEventContract contract)
    {
        if (string.IsNullOrWhiteSpace(contract.EventType))
        {
            return "missing-event-type";
        }

        if (contract.OccurredAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return "invalid-timestamp";
        }

        foreach (var measurement in contract.Measurements.Values)
        {
            if (measurement.QuarantineRequired)
            {
                return measurement.QuarantineReason ?? "invalid-unit";
            }
        }

        if (string.IsNullOrWhiteSpace(contract.MachineId) &&
            string.IsNullOrWhiteSpace(contract.OrderId) &&
            string.IsNullOrWhiteSpace(contract.MaterialId))
        {
            return "missing-resource-context";
        }

        return null;
    }

    private async Task<Guid> CreateQuarantineAsync(
        IntegrationOperationContext context,
        IntegrationInboundFactoryEvent inbound,
        IntegrationConnector connector,
        string reason,
        CancellationToken cancellationToken)
    {
        var quarantine = new IntegrationIngestionQuarantine
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectorId = inbound.ConnectorId,
            SourceSystem = connector.SourceSystemName,
            SourceEntityType = "factory-event",
            SourceRecordId = inbound.SourceEventId,
            ReasonCode = reason,
            SafeReason = reason,
            Status = IntegrationQuarantineStatuses.Open,
            OccurredAtUtc = inbound.ReceivedAtUtc,
        };
        dbContext.IntegrationIngestionQuarantine.Add(quarantine);
        await dbContext.SaveChangesAsync(cancellationToken);
        return quarantine.Id;
    }

    private async Task<IntegrationConnector?> LoadConnectorAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken) =>
        await dbContext.IntegrationConnectors
            .FirstOrDefaultAsync(
                c => c.TenantId == context.TenantId &&
                     c.FacilityId == context.FacilityId &&
                     c.Id == connectorId,
                cancellationToken);

    private async Task<long?> GetLatestSequenceAsync(
        IntegrationOperationContext context,
        IntegrationCanonicalFactoryEvent current,
        CancellationToken cancellationToken)
    {
        if (current.SourceSequence is null || string.IsNullOrWhiteSpace(current.MachineId))
        {
            return null;
        }

        return await dbContext.IntegrationCanonicalFactoryEvents
            .AsNoTracking()
            .Where(e => e.TenantId == context.TenantId &&
                        e.FacilityId == context.FacilityId &&
                        e.MachineId == current.MachineId &&
                        e.ProcessingState == FactoryCanonicalEventStates.Processed)
            .MaxAsync(e => (long?)e.SourceSequence, cancellationToken);
    }

    private static FactoryCanonicalEventContract DeserializeContract(IntegrationCanonicalFactoryEvent canonical)
    {
        var measurements = JsonSerializer.Deserialize<Dictionary<string, FactoryNormalizedMeasurement>>(canonical.MeasurementsJson)
            ?? new Dictionary<string, FactoryNormalizedMeasurement>();
        var businessAttributes = JsonSerializer.Deserialize<Dictionary<string, string>>(canonical.BusinessAttributesJson)
            ?? new Dictionary<string, string>();

        return new FactoryCanonicalEventContract(
            canonical.EventType,
            canonical.EventVersion,
            canonical.SourceEventId,
            canonical.OccurredAtUtc,
            canonical.MachineId,
            canonical.OrderId,
            canonical.MaterialId,
            canonical.MoldId,
            canonical.ProductionLineId,
            canonical.ShiftId,
            canonical.Severity,
            canonical.Title,
            canonical.Description,
            measurements,
            businessAttributes);
    }
}

public static class FactoryEventOrderingGuard
{
    public static bool ShouldApply(IntegrationCanonicalFactoryEvent current, long? latestSequence)
    {
        if (current.SourceSequence is null || latestSequence is null)
        {
            return true;
        }

        return current.SourceSequence >= latestSequence;
    }
}
