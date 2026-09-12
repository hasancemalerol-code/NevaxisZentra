using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Outbox;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanHandoffRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionPlanHandoffRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
        ProductionPlanHandoffContext context,
        PrepareProductionPlanHandoffCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanHandoffResult.Fail(
                ProductionPlanHandoffFailureCodes.InvalidScope,
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
                        return ProductionPlanHandoffResult.Fail(
                            ProductionPlanHandoffFailureCodes.NotFound,
                            "Production plan was not found in the requested scope.");
                    }

                    if (plan.Version != command.ExpectedVersion)
                    {
                        return ProductionPlanHandoffResult.Fail(
                            ProductionPlanHandoffFailureCodes.ConcurrencyConflict,
                            "The production plan has been modified by another user. Please reload and try again.");
                    }

                    if (plan.Status != ProductionPlanStatus.Scheduled)
                    {
                        return ProductionPlanHandoffResult.Fail(
                            ProductionPlanHandoffFailureCodes.NotScheduled,
                            $"Only scheduled plans may be handed off. Current status: '{ProductionPlanValueConversions.ToStorage(plan.Status)}'.");
                    }

                    if (plan.ApprovalStatus != ProductionPlanApprovalStatus.Approved)
                    {
                        return ProductionPlanHandoffResult.Fail(
                            ProductionPlanHandoffFailureCodes.NotEligible,
                            "The plan must be approved before handoff.");
                    }

                    if (HasBlockingRisk(plan.RisksJson))
                    {
                        return ProductionPlanHandoffResult.Fail(
                            ProductionPlanHandoffFailureCodes.BlockingRisk,
                            "The plan has unresolved blocking risks and cannot be handed off.");
                    }

                    var idempotencyKey = BuildIdempotencyKey(
                        context.TenantId,
                        context.FacilityId,
                        plan.Id,
                        plan.Version);

                    var existingHandoff = await dbContext.ProductionPlanHandoffs
                        .IgnoreQueryFilters()
                        .Where(h => h.IdempotencyKey == idempotencyKey)
                        .SingleOrDefaultAsync(ct);

                    if (existingHandoff is not null)
                    {
                        if (existingHandoff.Status == ProductionPlanHandoffStatus.Prepared)
                        {
                            return ProductionPlanHandoffResult.Success(
                                existingHandoff.Id,
                                plan.Id,
                                plan.ReferenceCode,
                                "prepared",
                                existingHandoff.Version,
                                existingHandoff.PreparedAtUtc);
                        }

                        return ProductionPlanHandoffResult.Fail(
                            ProductionPlanHandoffFailureCodes.AlreadyPrepared,
                            "A handoff already exists for this plan version.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var payloadJson = BuildPayloadJson(plan, context.FacilityId);

                    var handoff = new ProductionPlanHandoff
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionPlanId = plan.Id,
                        ProductionPlanVersion = plan.Version,
                        ReferenceCode = plan.ReferenceCode,
                        Status = ProductionPlanHandoffStatus.Prepared,
                        IdempotencyKey = idempotencyKey,
                        PreparedAtUtc = now,
                        PreparedBySubjectId = context.IdentitySubjectId.ToString(),
                        CorrelationId = correlationId,
                        Version = 1,
                        PayloadJson = payloadJson,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                    };

                    dbContext.ProductionPlanHandoffs.Add(handoff);

                    dbContext.AuditRecords.Add(new Audit.AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.ProductionPlanHandoffPrepared,
                        Action = AuditActions.ProductionPlanHandoffPrepare,
                        ResourceType = "production.plan",
                        ResourceId = plan.Id.ToString(),
                        Outcome = Audit.AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = Audit.AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            handoffId = handoff.Id.ToString(),
                            productionPlanId = plan.Id.ToString(),
                            productionPlanVersion = plan.Version,
                            referenceCode = plan.ReferenceCode,
                            status = "prepared",
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = OutboxMessageTypes.ProductionPlanHandoffPrepared,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-plan",
                        AggregateId = plan.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{plan.Id}:{plan.Version}:{OutboxMessageTypes.ProductionPlanHandoffPrepared}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionPlanId"] = plan.Id.ToString(),
                            ["handoffId"] = handoff.Id.ToString(),
                            ["referenceCode"] = plan.ReferenceCode,
                            ["productionPlanVersion"] = plan.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["preparedBySubjectId"] = context.IdentitySubjectId.ToString(),
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

                    return ProductionPlanHandoffResult.Success(
                        handoff.Id,
                        plan.Id,
                        plan.ReferenceCode,
                        "prepared",
                        handoff.Version,
                        handoff.PreparedAtUtc);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanHandoffResult.Fail(
                ProductionPlanHandoffFailureCodes.SourceUnavailable,
                "Production plan handoff service is unavailable.");
        }
    }

    private static string BuildIdempotencyKey(
        Guid tenantId,
        Guid facilityId,
        Guid planId,
        uint planVersion) =>
        $"{tenantId}:{facilityId}:{planId}:{planVersion}:handoff";

    private static bool HasBlockingRisk(string risksJson)
    {
        if (string.IsNullOrWhiteSpace(risksJson) || risksJson == "[]")
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(risksJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var risk in doc.RootElement.EnumerateArray())
            {
                if (risk.TryGetProperty("severity", out var severity)
                    && severity.ValueKind == JsonValueKind.String
                    && string.Equals(severity.GetString(), "blocking", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private static string BuildPayloadJson(ProductionPlan plan, Guid facilityId)
    {
        var approval = ParseApprovalSummary(plan.ApprovalJson);
        var schedule = ParseScheduleSummary(plan.TimeWindowJson);
        var demands = ParseDemandsSnapshot(plan.DemandsJson);
        var resources = ParseResourcesSnapshot(plan.ResourceAssignmentJson);
        var transfer = ParseTransferSummary(plan.TransferJson);

        return JsonSerializer.Serialize(new
        {
            planId = plan.Id.ToString(),
            referenceCode = plan.ReferenceCode,
            facilityId = facilityId.ToString(),
            planVersion = plan.Version,
            scheduledStartAtUtc = schedule.StartAtUtc?.ToString("O"),
            scheduledEndAtUtc = schedule.EndAtUtc?.ToString("O"),
            shiftName = schedule.ShiftName,
            approvedBy = approval.ApprovedBy,
            approvedAtUtc = approval.ApprovedAtUtc?.ToString("O"),
            demands,
            machineId = resources.MachineId,
            moldId = resources.MoldId,
            shiftTeam = resources.ShiftTeam,
            transferStatus = transfer.Status,
            transferExternalReference = transfer.ExternalReference,
        }, JsonOptions);
    }

    private static (string? ApprovedBy, DateTimeOffset? ApprovedAtUtc) ParseApprovalSummary(string approvalJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(approvalJson);
            string? approvedBy = null;
            DateTimeOffset? approvedAtUtc = null;

            if (doc.RootElement.TryGetProperty("approvedBy", out var byProp))
            {
                approvedBy = byProp.GetString();
            }

            if (doc.RootElement.TryGetProperty("approvedAt", out var atProp)
                && DateTimeOffset.TryParse(atProp.GetString(), out var parsed))
            {
                approvedAtUtc = parsed;
            }

            return (approvedBy, approvedAtUtc);
        }
        catch
        {
            return (null, null);
        }
    }

    private static (DateTimeOffset? StartAtUtc, DateTimeOffset? EndAtUtc, string? ShiftName) ParseScheduleSummary(string timeWindowJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(timeWindowJson);
            DateTimeOffset? start = null;
            DateTimeOffset? end = null;
            string? shiftName = null;

            if (doc.RootElement.TryGetProperty("plannedStartAt", out var startProp)
                && DateTimeOffset.TryParse(startProp.GetString(), out var parsedStart))
            {
                start = parsedStart;
            }

            if (doc.RootElement.TryGetProperty("plannedEndAt", out var endProp)
                && DateTimeOffset.TryParse(endProp.GetString(), out var parsedEnd))
            {
                end = parsedEnd;
            }

            if (doc.RootElement.TryGetProperty("shiftName", out var shiftProp))
            {
                shiftName = shiftProp.GetString();
            }

            return (start, end, shiftName);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static List<object> ParseDemandsSnapshot(string demandsJson)
    {
        var result = new List<object>();
        try
        {
            using var doc = JsonDocument.Parse(demandsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var demand in doc.RootElement.EnumerateArray())
            {
                string? productCode = null;
                decimal? quantity = null;
                string? unit = null;

                if (demand.TryGetProperty("productCode", out var pc))
                {
                    productCode = pc.GetString();
                }

                // Plans store the demand quantity as "requiredQuantity"; fall back to "quantity" for legacy payloads.
                // Emit as decimal so the handoff payload carries a JSON number, not a string.
                if (demand.TryGetProperty("requiredQuantity", out var reqQty)
                    && reqQty.ValueKind == JsonValueKind.Number
                    && reqQty.TryGetDecimal(out var reqQtyDecimal)
                    && reqQtyDecimal > 0)
                {
                    quantity = reqQtyDecimal;
                }
                else if (demand.TryGetProperty("quantity", out var qty)
                    && qty.ValueKind == JsonValueKind.Number
                    && qty.TryGetDecimal(out var qtyDecimal)
                    && qtyDecimal > 0)
                {
                    quantity = qtyDecimal;
                }

                if (demand.TryGetProperty("unit", out var u))
                {
                    unit = u.GetString();
                }

                result.Add(new { productCode, quantity, unit });
            }
        }
        catch
        {
            // ignored
        }

        return result;
    }

    private static (string? MachineId, string? MoldId, string? ShiftTeam) ParseResourcesSnapshot(string resourcesJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(resourcesJson);
            string? machineId = null;
            string? moldId = null;
            string? shiftTeam = null;

            if (doc.RootElement.TryGetProperty("machineId", out var m))
            {
                machineId = m.GetString();
            }

            if (doc.RootElement.TryGetProperty("moldId", out var mo))
            {
                moldId = mo.GetString();
            }

            if (doc.RootElement.TryGetProperty("shiftTeam", out var st))
            {
                shiftTeam = st.GetString();
            }

            return (machineId, moldId, shiftTeam);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static (string? Status, string? ExternalReference) ParseTransferSummary(string transferJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(transferJson);
            string? status = null;
            string? externalRef = null;

            if (doc.RootElement.TryGetProperty("status", out var s))
            {
                status = s.GetString();
            }

            if (doc.RootElement.TryGetProperty("externalReference", out var er))
            {
                externalRef = er.GetString();
            }

            return (status, externalRef);
        }
        catch
        {
            return (null, null);
        }
    }
}

public sealed class UnavailableProductionPlanHandoffRepository : IProductionPlanHandoffRepository
{
    private static readonly ProductionPlanHandoffResult UnavailableResult =
        ProductionPlanHandoffResult.Fail(
            ProductionPlanHandoffFailureCodes.SourceUnavailable,
            "Production plan handoff is unavailable because persistence is disabled.");

    public Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
        ProductionPlanHandoffContext context,
        PrepareProductionPlanHandoffCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);
}
