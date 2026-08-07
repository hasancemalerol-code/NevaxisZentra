using System.Text.Json;
using System.Text.Json.Serialization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Production;

/// <summary>Unavailable stub used when persistence is disabled.</summary>
public sealed class UnavailableProductionExceptionRepository : IProductionExceptionRepository
{
    private static readonly ProductionExceptionWriteResult Unavailable =
        ProductionExceptionWriteResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production exception service is unavailable because persistence is disabled.");

    public Task<ProductionExceptionWriteResult> RaiseExceptionAsync(ProductionOrderOperationContext context, RaiseExceptionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionExceptionWriteResult> AcknowledgeExceptionAsync(ProductionOrderOperationContext context, AcknowledgeExceptionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionExceptionWriteResult> ResolveExceptionAsync(ProductionOrderOperationContext context, ResolveExceptionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsListResult<ExceptionReadModel>> ListExceptionsAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) => Task.FromResult(ProductionOperationsListResult.Fail<ExceptionReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
}

/// <summary>
/// Concrete implementation for production execution exception management.
/// Each write is atomic: exception record + ProductionOrder version + audit + outbox
/// are committed in the same transaction via TenantDatabaseSession.ExecuteWriteAsync.
/// </summary>
public sealed class ProductionExceptionRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionExceptionRepository
{
    private const int DefaultReadLimit = 50;
    private const int MaxReadLimit = 100;
    private const int MaxMessageLength = 500;
    private const int MaxResolutionNoteLength = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ────────────────────────────── Raise ─────────────────────────────────────

    public async Task<ProductionExceptionWriteResult> RaiseExceptionAsync(
        ProductionOrderOperationContext context,
        RaiseExceptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (!ProductionExceptionTypes.All.Contains(command.ExceptionType))
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidExceptionType,
                $"Unknown exception type: '{command.ExceptionType}'.");
        }

        if (!ProductionExceptionSeverities.All.Contains(command.Severity))
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidExceptionSeverity,
                $"Unknown severity: '{command.Severity}'.");
        }

        if (string.IsNullOrWhiteSpace(command.Message))
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.CommentRequired,
                "Exception message is required.");
        }

        if (command.Message.Length > MaxMessageLength)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.MessageTooLong,
                $"Exception message must not exceed {MaxMessageLength} characters.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .SingleOrDefaultAsync(
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == command.ProductionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found in the requested scope.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    // Completed orders may only receive reconciliation-type exceptions
                    if (order.Status == ProductionOrderStatus.Completed
                        && command.ExceptionType is not (
                            ProductionExceptionTypes.HandoffInconsistency
                            or ProductionExceptionTypes.QuantityAnomaly
                            or ProductionExceptionTypes.Other))
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            "Completed orders only accept reconciliation exceptions (handoff-inconsistency, quantity-anomaly, other).");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var exception = new ProductionExecutionExceptionRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionOrderId = order.Id,
                        ExceptionType = command.ExceptionType,
                        Severity = command.Severity,
                        Status = ProductionExceptionStatuses.Open,
                        Message = command.Message.Trim(),
                        RaisedAtUtc = now,
                        RaisedBySubjectId = actorSubjectId,
                        CorrelationId = correlationId,
                        Version = 1,
                    };

                    dbContext.ProductionExecutionExceptions.Add(exception);

                    order.LastModifiedBySubjectId = actorSubjectId;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = ProductionAuditEventTypes.ProductionExceptionRaised,
                        Action = ProductionAuditActions.ProductionExceptionRaise,
                        ResourceType = "production.order",
                        ResourceId = order.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            exceptionId = exception.Id.ToString(),
                            exceptionType = command.ExceptionType,
                            severity = command.Severity,
                        }, JsonOptions),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = ProductionOutboxMessageTypes.ProductionExceptionRaised,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-order",
                        AggregateId = order.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{exception.Id}:1:{ProductionOutboxMessageTypes.ProductionExceptionRaised}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["exceptionId"] = exception.Id.ToString(),
                            ["exceptionType"] = command.ExceptionType,
                            ["severity"] = command.Severity,
                            ["occurredAtUtc"] = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionExceptionWriteResult.Success(exception.Id, order.Version, exception.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production exception service is unavailable.");
        }
    }

    // ────────────────────────────── Acknowledge ───────────────────────────────

    public async Task<ProductionExceptionWriteResult> AcknowledgeExceptionAsync(
        ProductionOrderOperationContext context,
        AcknowledgeExceptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .SingleOrDefaultAsync(
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == command.ProductionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found in the requested scope.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var exception = await dbContext.ProductionExecutionExceptions
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId
                                && e.ProductionOrderId == command.ProductionOrderId
                                && e.Id == command.ExceptionId,
                            ct);

                    if (exception is null)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ExceptionNotFound,
                            "Production exception was not found.");
                    }

                    if (exception.Status == ProductionExceptionStatuses.Resolved)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ExceptionAlreadyResolved,
                            "This exception has already been resolved.");
                    }

                    if (exception.Status != ProductionExceptionStatuses.Open)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            $"Cannot acknowledge exception with status '{exception.Status}'.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    exception.Status = ProductionExceptionStatuses.Acknowledged;
                    exception.Version += 1;

                    order.LastModifiedBySubjectId = actorSubjectId;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = ProductionAuditEventTypes.ProductionExceptionAcknowledged,
                        Action = ProductionAuditActions.ProductionExceptionAcknowledge,
                        ResourceType = "production.order",
                        ResourceId = order.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            exceptionId = exception.Id.ToString(),
                        }, JsonOptions),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = ProductionOutboxMessageTypes.ProductionExceptionAcknowledged,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-order",
                        AggregateId = order.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{exception.Id}:{exception.Version}:{ProductionOutboxMessageTypes.ProductionExceptionAcknowledged}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["exceptionId"] = exception.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionExceptionWriteResult.Success(exception.Id, order.Version, exception.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production exception service is unavailable.");
        }
    }

    // ────────────────────────────── Resolve ───────────────────────────────────

    public async Task<ProductionExceptionWriteResult> ResolveExceptionAsync(
        ProductionOrderOperationContext context,
        ResolveExceptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(command.ResolutionNote))
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.ResolutionNoteRequired,
                "A resolution note is required when resolving an exception.");
        }

        if (command.ResolutionNote.Length > MaxResolutionNoteLength)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.MessageTooLong,
                $"Resolution note must not exceed {MaxResolutionNoteLength} characters.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .SingleOrDefaultAsync(
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == command.ProductionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found in the requested scope.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var exception = await dbContext.ProductionExecutionExceptions
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId
                                && e.ProductionOrderId == command.ProductionOrderId
                                && e.Id == command.ExceptionId,
                            ct);

                    if (exception is null)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ExceptionNotFound,
                            "Production exception was not found.");
                    }

                    if (exception.Status == ProductionExceptionStatuses.Resolved)
                    {
                        return ProductionExceptionWriteResult.Fail(
                            ProductionOrderFailureCodes.ExceptionAlreadyResolved,
                            "This exception has already been resolved.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    exception.Status = ProductionExceptionStatuses.Resolved;
                    exception.ResolvedAtUtc = now;
                    exception.ResolvedBySubjectId = actorSubjectId;
                    exception.ResolutionNote = command.ResolutionNote.Trim();
                    exception.Version += 1;

                    order.LastModifiedBySubjectId = actorSubjectId;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = ProductionAuditEventTypes.ProductionExceptionResolved,
                        Action = ProductionAuditActions.ProductionExceptionResolve,
                        ResourceType = "production.order",
                        ResourceId = order.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            exceptionId = exception.Id.ToString(),
                        }, JsonOptions),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = ProductionOutboxMessageTypes.ProductionExceptionResolved,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-order",
                        AggregateId = order.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{exception.Id}:{exception.Version}:{ProductionOutboxMessageTypes.ProductionExceptionResolved}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["exceptionId"] = exception.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionExceptionWriteResult.Success(exception.Id, order.Version, exception.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionExceptionWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production exception service is unavailable.");
        }
    }

    // ────────────────────────────── List ──────────────────────────────────────

    public async Task<ProductionOperationsListResult<ExceptionReadModel>> ListExceptionsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsListResult.Fail<ExceptionReadModel>(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        var safeLimit = Math.Clamp(limit <= 0 ? DefaultReadLimit : limit, 1, MaxReadLimit);

        try
        {
            var items = await dbContext.ProductionExecutionExceptions
                .Where(e =>
                    e.TenantId == context.TenantId
                    && e.FacilityId == context.FacilityId
                    && e.ProductionOrderId == productionOrderId)
                .OrderByDescending(e => e.RaisedAtUtc)
                .Take(safeLimit)
                .Select(e => new ExceptionReadModel(
                    e.Id,
                    e.ExceptionType,
                    e.Severity,
                    e.Status,
                    e.Message,
                    e.RaisedAtUtc,
                    e.RaisedBySubjectId,
                    e.ResolvedAtUtc,
                    e.ResolvedBySubjectId,
                    e.ResolutionNote,
                    e.CorrelationId,
                    e.Version))
                .ToListAsync(cancellationToken);

            return ProductionOperationsListResult.Success<ExceptionReadModel>(items);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsListResult.Fail<ExceptionReadModel>(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production exception service is unavailable.");
        }
    }
}
