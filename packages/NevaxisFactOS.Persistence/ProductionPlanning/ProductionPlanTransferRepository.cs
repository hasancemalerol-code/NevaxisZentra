using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Outbox;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanTransferRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionPlanTransferRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<string> AllowedTargetSystems =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "erp", "mes" };

    public async Task<ProductionPlanTransferRequestResult> CreateTransferRequestAsync(
        ProductionPlanTransferContext context,
        CreateTransferRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanTransferRequestResult.Fail(
                ProductionPlanTransferFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        var targetSystem = command.TargetSystem?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AllowedTargetSystems.Contains(targetSystem))
        {
            return ProductionPlanTransferRequestResult.Fail(
                ProductionPlanTransferFailureCodes.ValidationFailed,
                $"Target system '{command.TargetSystem}' is not supported. Supported values: erp, mes.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var plan = await dbContext.ProductionPlans
                        .SingleOrDefaultAsync(
                            p =>
                                p.TenantId == context.TenantId
                                && p.FacilityId == context.FacilityId
                                && p.Id == command.ProductionPlanId,
                            ct);

                    if (plan is null)
                    {
                        return ProductionPlanTransferRequestResult.Fail(
                            ProductionPlanTransferFailureCodes.NotFound,
                            "Production plan was not found in the requested scope.");
                    }

                    if (plan.Status != ProductionPlanStatus.Scheduled)
                    {
                        return ProductionPlanTransferRequestResult.Fail(
                            ProductionPlanTransferFailureCodes.InvalidTransition,
                            $"Cannot create a transfer request for a plan with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}'. Only Scheduled plans may be transferred.");
                    }

                    if (plan.Version != command.ExpectedPlanVersion)
                    {
                        return ProductionPlanTransferRequestResult.Fail(
                            ProductionPlanTransferFailureCodes.ConcurrencyConflict,
                            "The production plan has been modified. Please reload and try again.");
                    }

                    var idempotencyKey = BuildIdempotencyKey(
                        context.TenantId,
                        context.FacilityId,
                        command.ProductionPlanId,
                        targetSystem,
                        command.ExpectedPlanVersion);

                    var existing = await dbContext.ProductionPlanTransferRequests
                        .IgnoreQueryFilters()
                        .SingleOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, ct);

                    if (existing is not null)
                    {
                        if (existing.Status == ProductionPlanTransferRequestStatus.Succeeded)
                        {
                            return ProductionPlanTransferRequestResult.Fail(
                                ProductionPlanTransferFailureCodes.TransferAlreadySucceeded,
                                "A transfer request for this plan version and target system has already succeeded.");
                        }

                        return ProductionPlanTransferRequestResult.Success(
                            existing.Id,
                            existing.ProductionPlanId,
                            existing.TargetSystem,
                            ToStatusString(existing.Status),
                            existing.RequestedAtUtc,
                            isIdempotentDuplicate: true);
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var transferRequestId = Guid.NewGuid();

                    var transferRequest = new ProductionPlanTransferRequest
                    {
                        Id = transferRequestId,
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionPlanId = command.ProductionPlanId,
                        TargetSystem = targetSystem,
                        OperationType = "production-plan-transfer",
                        Status = ProductionPlanTransferRequestStatus.Requested,
                        IdempotencyKey = idempotencyKey,
                        AttemptNumber = 1,
                        RequestedBySubjectId = context.IdentitySubjectId.ToString(),
                        RequestedAtUtc = now,
                        CorrelationId = correlationId,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    };

                    dbContext.ProductionPlanTransferRequests.Add(transferRequest);

                    plan.TransferStatus = ProductionPlanTransferStatus.Pending;
                    plan.TransferTargetSystem = targetSystem == "erp"
                        ? ProductionPlanTargetSystem.Erp
                        : ProductionPlanTargetSystem.Mes;
                    plan.TransferJson = JsonSerializer.Serialize(new
                    {
                        status = "pending",
                        targetSystem,
                        requestedAt = now.ToString("O"),
                        requestedBy = context.IdentitySubjectId.ToString(),
                    }, JsonOptions);
                    plan.UpdatedAtUtc = now;

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.ProductionPlanTransferRequested,
                        Action = AuditActions.ProductionPlanTransferRequest,
                        ResourceType = "production.plan",
                        ResourceId = plan.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            targetSystem,
                            transferRequestId = transferRequestId.ToString(),
                            planVersion = command.ExpectedPlanVersion,
                            attemptNumber = 1,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = OutboxMessageTypes.ProductionPlanTransferRequested,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-plan",
                        AggregateId = plan.Id.ToString(),
                        CorrelationId = correlationId,
                        IdempotencyKey = $"{plan.Id}:{transferRequestId}:{OutboxMessageTypes.ProductionPlanTransferRequested}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionPlanId"] = plan.Id.ToString(),
                            ["transferRequestId"] = transferRequestId.ToString(),
                            ["targetSystem"] = targetSystem,
                            ["actorSubjectId"] = context.IdentitySubjectId.ToString(),
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

                    return ProductionPlanTransferRequestResult.Success(
                        transferRequestId,
                        plan.Id,
                        targetSystem,
                        "requested",
                        now);
                },
                cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return ProductionPlanTransferRequestResult.Fail(
                ProductionPlanTransferFailureCodes.TransferAlreadyRequested,
                "A concurrent transfer request for this plan was just created. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanTransferRequestResult.Fail(
                ProductionPlanTransferFailureCodes.SourceUnavailable,
                "Transfer request service is unavailable.");
        }
    }

    public async Task<ProductionPlanTransferExecuteResult> ExecuteTransferAsync(
        ProductionPlanTransferContext context,
        ExecuteTransferCommand command,
        IEnumerable<IProductionPlanTransferAdapter> adapters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(adapters);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanTransferExecuteResult.Fail(
                ProductionPlanTransferFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        ProductionPlanTransferRequest? transferRequest;
        ProductionPlan? plan;
        ProductionPlanTransferPayload payload;
        string idempotencyKey;
        Guid correlationId;
        DateTimeOffset startedAt;

        // Transaction A: validate + mark Processing
        try
        {
            var transactionAResult = await tenantDatabaseSession.ExecuteWriteAsync<(ProductionPlanTransferExecuteResult?, ProductionPlanTransferRequest?, ProductionPlan?, ProductionPlanTransferPayload?, string, Guid, DateTimeOffset)>(
                async ct =>
                {
                    plan = await dbContext.ProductionPlans
                        .SingleOrDefaultAsync(
                            p =>
                                p.TenantId == context.TenantId
                                && p.FacilityId == context.FacilityId
                                && p.Id == command.ProductionPlanId,
                            ct);

                    if (plan is null)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.NotFound,
                            "Production plan was not found."), null, null as ProductionPlan, null as ProductionPlanTransferPayload, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    if (plan.Status != ProductionPlanStatus.Scheduled)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.InvalidTransition,
                            "Plan must be in Scheduled status to execute a transfer."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    if (plan.Version != command.ExpectedPlanVersion)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.ConcurrencyConflict,
                            "The production plan has been modified. Please reload and try again."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    transferRequest = await dbContext.ProductionPlanTransferRequests
                        .SingleOrDefaultAsync(
                            r =>
                                r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && r.ProductionPlanId == command.ProductionPlanId
                                && r.Id == command.TransferRequestId,
                            ct);

                    if (transferRequest is null)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.NotFound,
                            "Transfer request was not found in the requested scope."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    if (transferRequest.Status == ProductionPlanTransferRequestStatus.Succeeded)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.TransferAlreadySucceeded,
                            "This transfer request has already succeeded."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    if (transferRequest.Status == ProductionPlanTransferRequestStatus.Failed
                        && transferRequest.RetryableFailure != true)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.TransferNotRetryable,
                            "This transfer request failed with a non-retryable error. Manual intervention required."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    if (transferRequest.Status == ProductionPlanTransferRequestStatus.Uncertain)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.TransferNotRetryable,
                            "This transfer request is in an uncertain state. Manual reconciliation required before retrying."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    if (transferRequest.Status == ProductionPlanTransferRequestStatus.Processing)
                    {
                        return (ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.InvalidTransition,
                            "Transfer is already being processed. Please wait or check status."), null, plan, null, string.Empty, Guid.Empty, DateTimeOffset.MinValue);
                    }

                    var now = DateTimeOffset.UtcNow;
                    var corrId = context.CorrelationId ?? Guid.NewGuid();

                    if (transferRequest.Status == ProductionPlanTransferRequestStatus.Failed
                        && transferRequest.RetryableFailure == true)
                    {
                        transferRequest.AttemptNumber += 1;
                    }

                    transferRequest.Status = ProductionPlanTransferRequestStatus.Processing;
                    transferRequest.StartedAtUtc = now;
                    transferRequest.UpdatedAtUtc = now;
                    transferRequest.CorrelationId = corrId;

                    plan.TransferStatus = ProductionPlanTransferStatus.InProgress;
                    plan.UpdatedAtUtc = now;

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.ProductionPlanTransferStarted,
                        Action = AuditActions.ProductionPlanTransferStart,
                        ResourceType = "production.plan",
                        ResourceId = plan.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = corrId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            targetSystem = transferRequest.TargetSystem,
                            transferRequestId = transferRequest.Id.ToString(),
                            attemptNumber = transferRequest.AttemptNumber,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = OutboxMessageTypes.ProductionPlanTransferStarted,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-plan",
                        AggregateId = plan.Id.ToString(),
                        CorrelationId = corrId,
                        IdempotencyKey = $"{plan.Id}:{transferRequest.Id}:{transferRequest.AttemptNumber}:{OutboxMessageTypes.ProductionPlanTransferStarted}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionPlanId"] = plan.Id.ToString(),
                            ["transferRequestId"] = transferRequest.Id.ToString(),
                            ["targetSystem"] = transferRequest.TargetSystem,
                            ["attemptNumber"] = transferRequest.AttemptNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
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

                    var transferPayload = BuildPayload(plan, context.FacilityId);
                    return (null, transferRequest, plan, transferPayload, transferRequest.IdempotencyKey, corrId, now);
                },
                cancellationToken);

            if (transactionAResult.Item1 is not null)
            {
                return transactionAResult.Item1;
            }

            transferRequest = transactionAResult.Item2!;
            plan = transactionAResult.Item3!;
            payload = transactionAResult.Item4!;
            idempotencyKey = transactionAResult.Item5;
            correlationId = transactionAResult.Item6;
            startedAt = transactionAResult.Item7;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanTransferExecuteResult.Fail(
                ProductionPlanTransferFailureCodes.SourceUnavailable,
                "Transfer execution service is unavailable (pre-execute phase).");
        }

        // External adapter call — outside any DB transaction
        var adaptersList = adapters as IReadOnlyList<IProductionPlanTransferAdapter>
            ?? adapters.ToList();
        var adapter = adaptersList.FirstOrDefault(a =>
            string.Equals(a.TargetSystem, transferRequest.TargetSystem, StringComparison.OrdinalIgnoreCase));

        if (adapter is null)
        {
            return ProductionPlanTransferExecuteResult.Fail(
                ProductionPlanTransferFailureCodes.AdapterNotFound,
                $"No transfer adapter is registered for target system '{transferRequest.TargetSystem}'.");
        }

        ProductionPlanTransferAdapterResult adapterResult;
        try
        {
            adapterResult = await adapter.TransferAsync(payload, idempotencyKey, correlationId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            adapterResult = new ProductionPlanTransferAdapterResult.Uncertain(
                "adapter-exception",
                ReconciliationRequired: true,
                "Adapter threw an unexpected exception. Manual reconciliation required.");
        }

        // Transaction B: persist result
        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var request = await dbContext.ProductionPlanTransferRequests
                        .SingleOrDefaultAsync(r => r.Id == transferRequest.Id, ct);

                    if (request is null)
                    {
                        return ProductionPlanTransferExecuteResult.Fail(
                            ProductionPlanTransferFailureCodes.SourceUnavailable,
                            "Transfer request could not be located after execution. Manual reconciliation may be required.");
                    }

                    var planReloaded = await dbContext.ProductionPlans
                        .SingleOrDefaultAsync(
                            p =>
                                p.TenantId == context.TenantId
                                && p.FacilityId == context.FacilityId
                                && p.Id == command.ProductionPlanId,
                            ct);

                    var now = DateTimeOffset.UtcNow;
                    request.CompletedAtUtc = now;
                    request.UpdatedAtUtc = now;

                    ProductionPlanTransferExecuteResult outcome;

                    switch (adapterResult)
                    {
                        case ProductionPlanTransferAdapterResult.Success success:
                            request.Status = ProductionPlanTransferRequestStatus.Succeeded;
                            request.ExternalReference = success.ExternalReference;
                            request.ResponseFingerprint = success.ResponseFingerprint;
                            if (planReloaded is not null)
                            {
                                planReloaded.TransferStatus = ProductionPlanTransferStatus.Completed;
                                planReloaded.TransferJson = JsonSerializer.Serialize(new
                                {
                                    status = "completed",
                                    targetSystem = request.TargetSystem,
                                    externalReference = success.ExternalReference,
                                    completedAt = now.ToString("O"),
                                }, JsonOptions);
                                planReloaded.UpdatedAtUtc = now;
                            }

                            PersistResultAuditAndOutbox(
                                dbContext, context, request, plan, now, correlationId,
                                AuditEventTypes.ProductionPlanTransferSucceeded,
                                AuditActions.ProductionPlanTransferSucceed,
                                OutboxMessageTypes.ProductionPlanTransferSucceeded,
                                success.ExternalReference, null, null);

                            outcome = ProductionPlanTransferExecuteResult.Success(
                                request.Id, success.ExternalReference);
                            break;

                        case ProductionPlanTransferAdapterResult.Failure failure:
                            request.Status = ProductionPlanTransferRequestStatus.Failed;
                            request.FailureCode = failure.Code;
                            request.SafeFailureMessage = failure.SafeMessage;
                            request.RetryableFailure = failure.Retryable;
                            if (planReloaded is not null)
                            {
                                planReloaded.TransferStatus = ProductionPlanTransferStatus.Failed;
                                planReloaded.TransferJson = JsonSerializer.Serialize(new
                                {
                                    status = "failed",
                                    targetSystem = request.TargetSystem,
                                    errorCode = failure.Code,
                                    errorMessage = failure.SafeMessage,
                                    retryable = failure.Retryable,
                                    failedAt = now.ToString("O"),
                                }, JsonOptions);
                                planReloaded.UpdatedAtUtc = now;
                            }

                            PersistResultAuditAndOutbox(
                                dbContext, context, request, plan, now, correlationId,
                                AuditEventTypes.ProductionPlanTransferFailed,
                                AuditActions.ProductionPlanTransferFail,
                                OutboxMessageTypes.ProductionPlanTransferFailed,
                                null, failure.Code, failure.SafeMessage);

                            outcome = ProductionPlanTransferExecuteResult.Failed(
                                request.Id, failure.Retryable, failure.SafeMessage);
                            break;

                        case ProductionPlanTransferAdapterResult.Uncertain uncertain:
                        default:
                            request.Status = ProductionPlanTransferRequestStatus.Uncertain;
                            request.FailureCode = adapterResult is ProductionPlanTransferAdapterResult.Uncertain u
                                ? u.Code : "uncertain";
                            request.SafeFailureMessage = adapterResult is ProductionPlanTransferAdapterResult.Uncertain u2
                                ? u2.SafeMessage : "Transfer result is uncertain.";
                            request.RetryableFailure = false;
                            if (planReloaded is not null)
                            {
                                planReloaded.TransferStatus = ProductionPlanTransferStatus.Uncertain;
                                planReloaded.TransferJson = JsonSerializer.Serialize(new
                                {
                                    status = "uncertain",
                                    targetSystem = request.TargetSystem,
                                    reconciliationRequired = true,
                                    failedAt = now.ToString("O"),
                                }, JsonOptions);
                                planReloaded.UpdatedAtUtc = now;
                            }

                            PersistResultAuditAndOutbox(
                                dbContext, context, request, plan, now, correlationId,
                                AuditEventTypes.ProductionPlanTransferUncertain,
                                AuditActions.ProductionPlanTransferMarkUncertain,
                                OutboxMessageTypes.ProductionPlanTransferUncertain,
                                null, request.FailureCode, request.SafeFailureMessage);

                            outcome = ProductionPlanTransferExecuteResult.UncertainResult(request.Id);
                            break;
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return outcome;
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanTransferExecuteResult.Fail(
                ProductionPlanTransferFailureCodes.SourceUnavailable,
                "Transfer execution service is unavailable (post-execute phase). Manual reconciliation may be required.");
        }
    }

    private static void PersistResultAuditAndOutbox(
        NevaxisFactOSDbContext db,
        ProductionPlanTransferContext context,
        ProductionPlanTransferRequest request,
        ProductionPlan plan,
        DateTimeOffset now,
        Guid correlationId,
        string auditEventType,
        string auditAction,
        string outboxMessageType,
        string? externalReference,
        string? failureCode,
        string? safeMessage)
    {
        db.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            EventType = auditEventType,
            Action = auditAction,
            ResourceType = "production.plan",
            ResourceId = plan.Id.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = correlationId,
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(new
            {
                targetSystem = request.TargetSystem,
                transferRequestId = request.Id.ToString(),
                attemptNumber = request.AttemptNumber,
                externalReference,
                failureCode,
            }),
        });

        var payloadDict = new Dictionary<string, string?>
        {
            ["tenantId"] = context.TenantId.ToString(),
            ["facilityId"] = context.FacilityId.ToString(),
            ["productionPlanId"] = plan.Id.ToString(),
            ["transferRequestId"] = request.Id.ToString(),
            ["targetSystem"] = request.TargetSystem,
            ["attemptNumber"] = request.AttemptNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["occurredAtUtc"] = now.ToString("O"),
        };

        if (externalReference is not null)
        {
            payloadDict["externalReference"] = externalReference;
        }

        if (failureCode is not null)
        {
            payloadDict["failureCode"] = failureCode;
        }

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = outboxMessageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = "production-plan",
            AggregateId = plan.Id.ToString(),
            CorrelationId = correlationId,
            IdempotencyKey = $"{plan.Id}:{request.Id}:{request.AttemptNumber}:{outboxMessageType}",
            PayloadJson = JsonSerializer.Serialize(payloadDict),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    private static ProductionPlanTransferPayload BuildPayload(ProductionPlan plan, Guid facilityId)
    {
        var demands = ParseDemands(plan.DemandsJson);
        var resources = ParseResources(plan.ResourceAssignmentJson);
        var approvalSummary = ParseApprovalSummary(plan.ApprovalJson);
        var schedulingWindow = ParseSchedulingWindow(plan.TimeWindowJson);

        return new ProductionPlanTransferPayload(
            PlanId: plan.Id,
            ReferenceCode: plan.ReferenceCode,
            FacilityReferenceId: facilityId.ToString(),
            PlannedStartAtUtc: plan.PlannedStartAtUtc,
            PlannedEndAtUtc: plan.PlannedEndAtUtc,
            ShiftName: schedulingWindow.ShiftName,
            Demands: demands,
            ResourceAssignment: resources,
            ApprovalSummary: approvalSummary,
            Version: plan.Version);
    }

    private static (string? ShiftName, bool Parsed) ParseSchedulingWindow(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("shiftName", out var shift))
            {
                return (shift.GetString(), true);
            }
        }
        catch { /* ignored */ }

        return (null, false);
    }

    private static List<ProductionPlanTransferDemand> ParseDemands(string json)
    {
        var result = new List<ProductionPlanTransferDemand>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var code = item.TryGetProperty("productCode", out var c) ? c.GetString() ?? "" : "";
                var qty = item.TryGetProperty("quantity", out var q) && q.TryGetDecimal(out var d) ? d : 0m;
                var unit = item.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(code))
                {
                    result.Add(new ProductionPlanTransferDemand(code, qty, unit));
                }
            }
        }
        catch { /* ignored */ }

        return result;
    }

    private static ProductionPlanTransferResourceAssignment ParseResources(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var machineId = doc.RootElement.TryGetProperty("machineId", out var m) ? m.GetString() : null;
                var moldId = doc.RootElement.TryGetProperty("moldId", out var mo) ? mo.GetString() : null;
                var team = doc.RootElement.TryGetProperty("shiftTeam", out var t) ? t.GetString() : null;
                return new ProductionPlanTransferResourceAssignment(machineId, moldId, team);
            }
        }
        catch { /* ignored */ }

        return new ProductionPlanTransferResourceAssignment(null, null, null);
    }

    private static ProductionPlanTransferApprovalSummary ParseApprovalSummary(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var approvedBy = doc.RootElement.TryGetProperty("approvedBy", out var by) ? by.GetString() ?? "unknown" : "unknown";
                DateTimeOffset approvedAt = DateTimeOffset.UtcNow;
                if (doc.RootElement.TryGetProperty("approvedAt", out var at)
                    && DateTimeOffset.TryParse(at.GetString(), out var parsed))
                {
                    approvedAt = parsed;
                }

                return new ProductionPlanTransferApprovalSummary(approvedBy, approvedAt);
            }
        }
        catch { /* ignored */ }

        return new ProductionPlanTransferApprovalSummary("unknown", DateTimeOffset.UtcNow);
    }

    private static string BuildIdempotencyKey(
        Guid tenantId,
        Guid facilityId,
        Guid planId,
        string targetSystem,
        uint planVersion) =>
        $"{tenantId}:{facilityId}:{planId}:{targetSystem}:{planVersion}";

    private static string ToStatusString(ProductionPlanTransferRequestStatus status) => status switch
    {
        ProductionPlanTransferRequestStatus.Requested => "requested",
        ProductionPlanTransferRequestStatus.Processing => "processing",
        ProductionPlanTransferRequestStatus.Succeeded => "succeeded",
        ProductionPlanTransferRequestStatus.Failed => "failed",
        ProductionPlanTransferRequestStatus.Uncertain => "uncertain",
        _ => "unknown",
    };

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message?.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class UnavailableProductionPlanTransferRepository : IProductionPlanTransferRepository
{
    private static readonly ProductionPlanTransferRequestResult UnavailableRequestResult =
        ProductionPlanTransferRequestResult.Fail(
            ProductionPlanTransferFailureCodes.SourceUnavailable,
            "Transfer service is unavailable because persistence is disabled.");

    private static readonly ProductionPlanTransferExecuteResult UnavailableExecuteResult =
        ProductionPlanTransferExecuteResult.Fail(
            ProductionPlanTransferFailureCodes.SourceUnavailable,
            "Transfer service is unavailable because persistence is disabled.");

    public Task<ProductionPlanTransferRequestResult> CreateTransferRequestAsync(
        ProductionPlanTransferContext context,
        CreateTransferRequestCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableRequestResult);

    public Task<ProductionPlanTransferExecuteResult> ExecuteTransferAsync(
        ProductionPlanTransferContext context,
        ExecuteTransferCommand command,
        IEnumerable<IProductionPlanTransferAdapter> adapters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableExecuteResult);
}
