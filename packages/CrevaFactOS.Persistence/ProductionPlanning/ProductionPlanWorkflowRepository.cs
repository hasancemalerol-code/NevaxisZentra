using System.Text.Json;
using System.Text.Json.Serialization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanWorkflowRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionPlanWorkflowRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public Task<ProductionPlanWorkflowResult> SubmitForReviewAsync(
        ProductionPlanWorkflowContext context,
        SubmitProductionPlanForReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ProductionPlanWorkflowValidator.ValidateSubmitForReview(command);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors));
        }

        return ExecuteWorkflowAsync(
            context,
            command.ProductionPlanId,
            command.ExpectedVersion,
            plan =>
            {
                if (!ProductionPlanWorkflowValidator.IsValidSubmitTransition(plan.Status))
                {
                    return ProductionPlanWorkflowResult.Fail(
                        ProductionPlanWorkflowFailureCodes.InvalidTransition,
                        $"Cannot submit for review a plan with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}'. Only Draft or NeedsReview plans may be submitted.");
                }

                return null;
            },
            (plan, now, previousStatus) =>
            {
                var comment = command.Comment?.Trim();
                plan.Status = ProductionPlanStatus.ReadyForApproval;
                plan.ApprovalStatus = ProductionPlanApprovalStatus.Pending;
                plan.ApprovalJson = SerializeApprovalForSubmit(plan.ApprovalJson, context.IdentitySubjectId, now, comment);
                plan.HistoryJson = AppendHistory(plan.HistoryJson, new WorkflowHistoryEntry(
                    Guid.NewGuid().ToString(),
                    "approval",
                    "İncelemeye gönderildi",
                    string.IsNullOrEmpty(comment)
                        ? "Plan approval incelemesine gönderildi."
                        : $"Plan incelemeye gönderildi. Yorum: {Truncate(comment, 200)}",
                    now,
                    ProductionPlanValueConversions.ToStorage(previousStatus),
                    ProductionPlanValueConversions.ToStorage(ProductionPlanStatus.ReadyForApproval),
                    context.IdentitySubjectId.ToString(),
                    plan.Version,
                    plan.Version + 1));
            },
            AuditActions.ProductionPlanSubmitForReview,
            AuditEventTypes.ProductionPlanSubmittedForReview,
            OutboxMessageTypes.ProductionPlanSubmittedForReview,
            cancellationToken);
    }

    public Task<ProductionPlanWorkflowResult> RequestRevisionAsync(
        ProductionPlanWorkflowContext context,
        RequestProductionPlanRevisionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ProductionPlanWorkflowValidator.ValidateRequestRevision(command);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors));
        }

        return ExecuteWorkflowAsync(
            context,
            command.ProductionPlanId,
            command.ExpectedVersion,
            plan =>
            {
                if (!ProductionPlanWorkflowValidator.IsValidRevisionTransition(plan.Status))
                {
                    return ProductionPlanWorkflowResult.Fail(
                        ProductionPlanWorkflowFailureCodes.InvalidTransition,
                        $"Cannot request revision for a plan with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}'. Only ReadyForApproval plans may have revision requested.");
                }

                return null;
            },
            (plan, now, previousStatus) =>
            {
                var reason = command.Reason.Trim();
                plan.Status = ProductionPlanStatus.NeedsReview;
                plan.ApprovalStatus = ProductionPlanApprovalStatus.Pending;
                plan.ApprovalJson = SerializeApprovalForRevision(plan.ApprovalJson, context.IdentitySubjectId, now, reason);
                plan.HistoryJson = AppendHistory(plan.HistoryJson, new WorkflowHistoryEntry(
                    Guid.NewGuid().ToString(),
                    "approval",
                    "Revizyon istendi",
                    Truncate(reason, 200),
                    now,
                    ProductionPlanValueConversions.ToStorage(previousStatus),
                    ProductionPlanValueConversions.ToStorage(ProductionPlanStatus.NeedsReview),
                    context.IdentitySubjectId.ToString(),
                    plan.Version,
                    plan.Version + 1));
            },
            AuditActions.ProductionPlanRequestRevision,
            AuditEventTypes.ProductionPlanRevisionRequested,
            OutboxMessageTypes.ProductionPlanRevisionRequested,
            cancellationToken);
    }

    public Task<ProductionPlanWorkflowResult> ApproveAsync(
        ProductionPlanWorkflowContext context,
        ApproveProductionPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ProductionPlanWorkflowValidator.ValidateApprove(command);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors));
        }

        return ExecuteWorkflowAsync(
            context,
            command.ProductionPlanId,
            command.ExpectedVersion,
            plan =>
            {
                if (!ProductionPlanWorkflowValidator.IsValidApproveTransition(plan.Status))
                {
                    return ProductionPlanWorkflowResult.Fail(
                        ProductionPlanWorkflowFailureCodes.InvalidTransition,
                        $"Cannot approve a plan with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}'. Only ReadyForApproval plans may be approved.");
                }

                return null;
            },
            (plan, now, previousStatus) =>
            {
                var note = command.Note?.Trim();
                plan.Status = ProductionPlanStatus.Approved;
                plan.ApprovalStatus = ProductionPlanApprovalStatus.Approved;
                plan.ApprovalJson = SerializeApprovalForApprove(plan.ApprovalJson, context.IdentitySubjectId, now, note);
                plan.HistoryJson = AppendHistory(plan.HistoryJson, new WorkflowHistoryEntry(
                    Guid.NewGuid().ToString(),
                    "approval",
                    "Onaylandı",
                    string.IsNullOrEmpty(note)
                        ? "Plan onaylandı."
                        : $"Plan onaylandı. Not: {Truncate(note, 200)}",
                    now,
                    ProductionPlanValueConversions.ToStorage(previousStatus),
                    ProductionPlanValueConversions.ToStorage(ProductionPlanStatus.Approved),
                    context.IdentitySubjectId.ToString(),
                    plan.Version,
                    plan.Version + 1));
            },
            AuditActions.ProductionPlanApprove,
            AuditEventTypes.ProductionPlanApproved,
            OutboxMessageTypes.ProductionPlanApproved,
            cancellationToken);
    }

    public Task<ProductionPlanWorkflowResult> RejectAsync(
        ProductionPlanWorkflowContext context,
        RejectProductionPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ProductionPlanWorkflowValidator.ValidateReject(command);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors));
        }

        return ExecuteWorkflowAsync(
            context,
            command.ProductionPlanId,
            command.ExpectedVersion,
            plan =>
            {
                if (!ProductionPlanWorkflowValidator.IsValidRejectTransition(plan.Status))
                {
                    return ProductionPlanWorkflowResult.Fail(
                        ProductionPlanWorkflowFailureCodes.InvalidTransition,
                        $"Cannot reject a plan with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}'. Only ReadyForApproval plans may be rejected.");
                }

                return null;
            },
            (plan, now, previousStatus) =>
            {
                var reason = command.Reason.Trim();
                plan.Status = ProductionPlanStatus.Rejected;
                plan.ApprovalStatus = ProductionPlanApprovalStatus.Rejected;
                plan.ApprovalJson = SerializeApprovalForReject(plan.ApprovalJson, context.IdentitySubjectId, now, reason);
                plan.HistoryJson = AppendHistory(plan.HistoryJson, new WorkflowHistoryEntry(
                    Guid.NewGuid().ToString(),
                    "approval",
                    "Reddedildi",
                    Truncate(reason, 200),
                    now,
                    ProductionPlanValueConversions.ToStorage(previousStatus),
                    ProductionPlanValueConversions.ToStorage(ProductionPlanStatus.Rejected),
                    context.IdentitySubjectId.ToString(),
                    plan.Version,
                    plan.Version + 1));
            },
            AuditActions.ProductionPlanReject,
            AuditEventTypes.ProductionPlanRejected,
            OutboxMessageTypes.ProductionPlanRejected,
            cancellationToken);
    }

    public async Task<ProductionPlanWorkflowResult> ScheduleAsync(
        ProductionPlanWorkflowContext context,
        ScheduleProductionPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ProductionPlanWorkflowValidator.ValidateSchedule(command);
        if (validationErrors.Count > 0)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors);
        }

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
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
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.NotFound,
                            "Production plan was not found in the requested scope.");
                    }

                    if (!ProductionPlanWorkflowValidator.IsValidScheduleTransition(plan.Status))
                    {
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.InvalidTransition,
                            $"Cannot schedule a plan with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}'. Only Approved plans may be scheduled.");
                    }

                    if (plan.Version != command.ExpectedVersion)
                    {
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.ConcurrencyConflict,
                            "The production plan has been modified by another user. Please reload and try again.");
                    }

                    if (plan.ApprovalStatus != ProductionPlanApprovalStatus.Approved)
                    {
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.InvalidTransition,
                            "Plan does not have an approved approval status.");
                    }

                    if (HasBlockingRisk(plan.RisksJson))
                    {
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.ScheduleBlockedByRisk,
                            "Plan has one or more blocking risks. Resolve all blocking risks before scheduling.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var previousStatus = plan.Status;
                    var previousStatusStr = ProductionPlanValueConversions.ToStorage(previousStatus);
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var shiftName = command.ShiftName?.Trim();
                    var comment = command.SchedulingComment?.Trim();

                    plan.Status = ProductionPlanStatus.Scheduled;
                    plan.PlannedStartAtUtc = command.PlannedStartAtUtc;
                    plan.PlannedEndAtUtc = command.PlannedEndAtUtc;
                    plan.UpdatedAtUtc = now;
                    plan.TimeWindowJson = SerializeSchedulingWindow(
                        command.PlannedStartAtUtc,
                        command.PlannedEndAtUtc,
                        now,
                        context.IdentitySubjectId,
                        shiftName,
                        comment);
                    plan.HistoryJson = AppendHistory(plan.HistoryJson, new WorkflowHistoryEntry(
                        Guid.NewGuid().ToString(),
                        "scheduling",
                        "Planlandı",
                        string.IsNullOrEmpty(comment)
                            ? "Plan üretim takviminde planlandı."
                            : $"Plan planlandı. Yorum: {Truncate(comment, 200)}",
                        now,
                        previousStatusStr,
                        ProductionPlanValueConversions.ToStorage(ProductionPlanStatus.Scheduled),
                        context.IdentitySubjectId.ToString(),
                        plan.Version,
                        plan.Version + 1));
                    plan.Version = plan.Version + 1;

                    var newStatusStr = ProductionPlanValueConversions.ToStorage(plan.Status);

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.ProductionPlanScheduled,
                        Action = AuditActions.ProductionPlanSchedule,
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
                            previousStatus = previousStatusStr,
                            newStatus = newStatusStr,
                            versionBefore = command.ExpectedVersion,
                            versionAfter = plan.Version,
                            plannedStartAtUtc = command.PlannedStartAtUtc.ToString("O"),
                            plannedEndAtUtc = command.PlannedEndAtUtc.ToString("O"),
                            scheduledAtUtc = now.ToString("O"),
                            shiftName,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = OutboxMessageTypes.ProductionPlanScheduled,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-plan",
                        AggregateId = plan.Id.ToString(),
                        CorrelationId = correlationId,
                        IdempotencyKey = $"{plan.Id}:{plan.Version}:{OutboxMessageTypes.ProductionPlanScheduled}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionPlanId"] = plan.Id.ToString(),
                            ["referenceCode"] = plan.ReferenceCode,
                            ["previousStatus"] = previousStatusStr,
                            ["newStatus"] = newStatusStr,
                            ["actorSubjectId"] = context.IdentitySubjectId.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                            ["version"] = plan.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["plannedStartAtUtc"] = command.PlannedStartAtUtc.ToString("O"),
                            ["plannedEndAtUtc"] = command.PlannedEndAtUtc.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionPlanWorkflowResult.SuccessScheduled(
                        plan.Id,
                        plan.ReferenceCode,
                        previousStatusStr,
                        newStatusStr,
                        plan.Version,
                        plan.UpdatedAtUtc,
                        ProductionPlanValueConversions.ToStorage(plan.ApprovalStatus),
                        plan.PlannedStartAtUtc,
                        plan.PlannedEndAtUtc,
                        now);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ConcurrencyConflict,
                "The production plan has been modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.SourceUnavailable,
                "Production plan workflow service is unavailable.");
        }
    }

    private static bool HasBlockingRisk(string risksJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(risksJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var risk in doc.RootElement.EnumerateArray())
            {
                if (risk.TryGetProperty("blocking", out var blocking)
                    && blocking.ValueKind == JsonValueKind.True)
                {
                    return true;
                }

                if (risk.TryGetProperty("severity", out var severity)
                    && severity.GetString() is "blocking")
                {
                    return true;
                }
            }
        }
        catch
        {
            // Malformed JSON — treat as no blocking risk
        }

        return false;
    }

    private static string SerializeSchedulingWindow(
        DateTimeOffset plannedStart,
        DateTimeOffset plannedEnd,
        DateTimeOffset scheduledAt,
        Guid scheduledBy,
        string? shiftName,
        string? comment)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["scheduledAt"] = scheduledAt.ToString("O"),
            ["scheduledBy"] = scheduledBy.ToString(),
            ["plannedStartAtUtc"] = plannedStart.ToString("O"),
            ["plannedEndAtUtc"] = plannedEnd.ToString("O"),
        };
        if (!string.IsNullOrEmpty(shiftName))
        {
            dict["shiftName"] = shiftName;
        }

        if (!string.IsNullOrEmpty(comment))
        {
            dict["schedulingComment"] = comment;
        }

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private async Task<ProductionPlanWorkflowResult> ExecuteWorkflowAsync(
        ProductionPlanWorkflowContext context,
        Guid productionPlanId,
        uint expectedVersion,
        Func<ProductionPlan, ProductionPlanWorkflowResult?> validateTransition,
        Action<ProductionPlan, DateTimeOffset, ProductionPlanStatus> applyTransition,
        string auditAction,
        string auditEventType,
        string outboxMessageType,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
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
                                && p.Id == productionPlanId,
                            ct);

                    if (plan is null)
                    {
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.NotFound,
                            "Production plan was not found in the requested scope.");
                    }

                    var transitionFailure = validateTransition(plan);
                    if (transitionFailure is not null)
                    {
                        return transitionFailure;
                    }

                    if (plan.Version != expectedVersion)
                    {
                        return ProductionPlanWorkflowResult.Fail(
                            ProductionPlanWorkflowFailureCodes.ConcurrencyConflict,
                            "The production plan has been modified by another user. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var previousStatus = plan.Status;
                    var previousStatusStr = ProductionPlanValueConversions.ToStorage(previousStatus);
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();

                    applyTransition(plan, now, previousStatus);

                    plan.UpdatedAtUtc = now;
                    plan.Version = plan.Version + 1;

                    var newStatusStr = ProductionPlanValueConversions.ToStorage(plan.Status);

                    dbContext.AuditRecords.Add(new AuditRecord
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
                            previousStatus = previousStatusStr,
                            newStatus = newStatusStr,
                            versionBefore = expectedVersion,
                            versionAfter = plan.Version,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = outboxMessageType,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-plan",
                        AggregateId = plan.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{plan.Id}:{plan.Version}:{outboxMessageType}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionPlanId"] = plan.Id.ToString(),
                            ["referenceCode"] = plan.ReferenceCode,
                            ["previousStatus"] = previousStatusStr,
                            ["newStatus"] = newStatusStr,
                            ["actorSubjectId"] = context.IdentitySubjectId.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                            ["version"] = plan.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionPlanWorkflowResult.Success(
                        plan.Id,
                        plan.ReferenceCode,
                        previousStatusStr,
                        newStatusStr,
                        plan.Version,
                        plan.UpdatedAtUtc,
                        ProductionPlanValueConversions.ToStorage(plan.ApprovalStatus));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.ConcurrencyConflict,
                "The production plan has been modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanWorkflowResult.Fail(
                ProductionPlanWorkflowFailureCodes.SourceUnavailable,
                "Production plan workflow service is unavailable.");
        }
    }

    private static string AppendHistory(string existingHistoryJson, WorkflowHistoryEntry entry)
    {
        List<string> entries;
        try
        {
            using var doc = JsonDocument.Parse(existingHistoryJson);
            entries = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().Select(e => e.GetRawText()).ToList()
                : [];
        }
        catch
        {
            entries = [];
        }

        var newEntry = JsonSerializer.Serialize(new
        {
            id = entry.Id,
            type = entry.Type,
            title = entry.Title,
            description = entry.Description,
            occurredAt = entry.OccurredAt.ToString("O"),
            previousStatus = entry.PreviousStatus,
            newStatus = entry.NewStatus,
            actorSubjectId = entry.ActorSubjectId,
            versionBefore = entry.VersionBefore,
            versionAfter = entry.VersionAfter,
        }, JsonOptions);

        entries.Add(newEntry);
        return "[" + string.Join(",", entries) + "]";
    }

    private static string SerializeApprovalForSubmit(
        string existingApprovalJson,
        Guid actorSubjectId,
        DateTimeOffset now,
        string? comment)
    {
        var existing = ParseApprovalDict(existingApprovalJson);
        existing["status"] = "pending";
        existing["requestedAt"] = now.ToString("O");
        existing["requestedBy"] = actorSubjectId.ToString();
        if (!string.IsNullOrEmpty(comment))
        {
            existing["submissionComment"] = comment;
        }
        else
        {
            existing.Remove("submissionComment");
        }

        existing.Remove("revisionRequestedAt");
        existing.Remove("revisionRequestedBy");
        existing.Remove("revisionReason");
        return JsonSerializer.Serialize(existing, JsonOptions);
    }

    private static string SerializeApprovalForRevision(
        string existingApprovalJson,
        Guid actorSubjectId,
        DateTimeOffset now,
        string reason)
    {
        var existing = ParseApprovalDict(existingApprovalJson);
        existing["status"] = "pending";
        existing["revisionRequestedAt"] = now.ToString("O");
        existing["revisionRequestedBy"] = actorSubjectId.ToString();
        existing["revisionReason"] = reason;
        return JsonSerializer.Serialize(existing, JsonOptions);
    }

    private static string SerializeApprovalForApprove(
        string existingApprovalJson,
        Guid actorSubjectId,
        DateTimeOffset now,
        string? note)
    {
        var existing = ParseApprovalDict(existingApprovalJson);
        existing["status"] = "approved";
        existing["approvedAt"] = now.ToString("O");
        existing["approvedBy"] = actorSubjectId.ToString();
        if (!string.IsNullOrEmpty(note))
        {
            existing["approvalNote"] = note;
        }
        else
        {
            existing.Remove("approvalNote");
        }

        return JsonSerializer.Serialize(existing, JsonOptions);
    }

    private static string SerializeApprovalForReject(
        string existingApprovalJson,
        Guid actorSubjectId,
        DateTimeOffset now,
        string reason)
    {
        var existing = ParseApprovalDict(existingApprovalJson);
        existing["status"] = "rejected";
        existing["rejectedAt"] = now.ToString("O");
        existing["rejectedBy"] = actorSubjectId.ToString();
        existing["rejectionReason"] = reason;
        return JsonSerializer.Serialize(existing, JsonOptions);
    }

    private static Dictionary<string, object?> ParseApprovalDict(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? (object?)prop.Value.GetString()
                        : prop.Value.GetRawText();
                }

                return dict;
            }
        }
        catch
        {
            // ignored
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";

    private sealed record WorkflowHistoryEntry(
        string Id,
        string Type,
        string Title,
        string Description,
        DateTimeOffset OccurredAt,
        string PreviousStatus,
        string NewStatus,
        string ActorSubjectId,
        uint VersionBefore,
        uint VersionAfter);
}

public sealed class UnavailableProductionPlanWorkflowRepository : IProductionPlanWorkflowRepository
{
    private static readonly ProductionPlanWorkflowResult UnavailableResult =
        ProductionPlanWorkflowResult.Fail(
            ProductionPlanWorkflowFailureCodes.SourceUnavailable,
            "Production plan workflow is unavailable because persistence is disabled.");

    public Task<ProductionPlanWorkflowResult> SubmitForReviewAsync(
        ProductionPlanWorkflowContext context,
        SubmitProductionPlanForReviewCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionPlanWorkflowResult> RequestRevisionAsync(
        ProductionPlanWorkflowContext context,
        RequestProductionPlanRevisionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionPlanWorkflowResult> ApproveAsync(
        ProductionPlanWorkflowContext context,
        ApproveProductionPlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionPlanWorkflowResult> RejectAsync(
        ProductionPlanWorkflowContext context,
        RejectProductionPlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionPlanWorkflowResult> ScheduleAsync(
        ProductionPlanWorkflowContext context,
        ScheduleProductionPlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);
}
