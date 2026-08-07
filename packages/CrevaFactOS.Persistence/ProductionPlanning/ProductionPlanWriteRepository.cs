using System.Text.Json;
using System.Text.Json.Serialization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed class ProductionPlanWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionPlanWriteRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<ProductionPlanWriteResult> CreateDraftAsync(
        ProductionPlanWriteContext context,
        CreateProductionPlanDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        var validationErrors = ProductionPlanDraftValidator.ValidateCreate(command);
        if (validationErrors.Count > 0)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors);
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var referenceCode = command.ReferenceCode.Trim();

                    var isDuplicate = await dbContext.ProductionPlans
                        .AnyAsync(
                            plan =>
                                plan.TenantId == context.TenantId
                                && plan.FacilityId == context.FacilityId
                                && plan.ReferenceCode == referenceCode,
                            ct);

                    if (isDuplicate)
                    {
                        return ProductionPlanWriteResult.Fail(
                            ProductionPlanWriteFailureCodes.ReferenceCodeConflict,
                            "A production plan with this reference code already exists in this facility.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var origin = ProductionPlanValueConversions.OriginFromStorage(command.Origin);
                    var confidence = command.Confidence is not null
                        ? ProductionPlanValueConversions.ConfidenceFromStorage(command.Confidence)
                        : ProductionPlanConfidence.Medium;

                    var plan = new ProductionPlan
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ReferenceCode = referenceCode,
                        Title = command.Title.Trim(),
                        Notes = command.Notes?.Trim(),
                        Status = ProductionPlanStatus.Draft,
                        Origin = origin,
                        AiGenerated = command.AiGenerated,
                        Confidence = confidence,
                        PlannedStartAtUtc = command.PlannedStartAtUtc,
                        PlannedEndAtUtc = command.PlannedEndAtUtc,
                        ApprovalStatus = ProductionPlanApprovalStatus.NotRequired,
                        TransferStatus = ProductionPlanTransferStatus.NotStarted,
                        TransferTargetSystem = null,
                        CreatedBy = context.IdentitySubjectId.ToString(),
                        AiExplanation = command.AiExplanation?.Trim(),
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                        Version = 1,
                        DemandsJson = SerializeDemands(command.Demands),
                        RisksJson = SerializeRisks(command.Risks),
                        ResourceAssignmentJson = SerializeResources(command.Resources),
                        TimeWindowJson = SerializeTimeWindow(command.TimeWindow, command.PlannedStartAtUtc, command.PlannedEndAtUtc),
                        ApprovalJson = SerializeApproval(command.Approval),
                        TransferJson = SerializeTransfer(command.Transfer),
                        SourceReferencesJson = SerializeSourceReferences(command.SourceReferences),
                        HistoryJson = "[]",
                    };

                    dbContext.ProductionPlans.Add(plan);

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.ProductionPlanDraftCreated,
                        Action = AuditActions.ProductionPlanDraftCreate,
                        ResourceType = "production-plan",
                        ResourceId = plan.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        CorrelationId = Guid.NewGuid(),
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            referenceCode = plan.ReferenceCode,
                            status = ProductionPlanValueConversions.ToStorage(plan.Status),
                            version = plan.Version,
                        }),
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionPlanWriteResult.Success(
                        plan.Id,
                        plan.ReferenceCode,
                        ProductionPlanValueConversions.ToStorage(plan.Status),
                        plan.Version,
                        plan.CreatedAtUtc,
                        plan.UpdatedAtUtc);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.ReferenceCodeConflict,
                "A production plan with this reference code already exists in this facility.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.SourceUnavailable,
                "Production plan could not be created.");
        }
    }

    public async Task<ProductionPlanWriteResult> UpdateDraftAsync(
        ProductionPlanWriteContext context,
        UpdateProductionPlanDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        var validationErrors = ProductionPlanDraftValidator.ValidateUpdate(command);
        if (validationErrors.Count > 0)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.ValidationFailed,
                "Validation failed.",
                validationErrors);
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
                        return ProductionPlanWriteResult.Fail(
                            ProductionPlanWriteFailureCodes.NotFound,
                            "Production plan was not found in the requested scope.");
                    }

                    if (!ProductionPlanDraftValidator.IsEditableStatus(plan.Status))
                    {
                        return ProductionPlanWriteResult.Fail(
                            ProductionPlanWriteFailureCodes.PlanNotEditable,
                            $"Production plans with status '{ProductionPlanValueConversions.ToStorage(plan.Status)}' cannot be edited.");
                    }

                    if (plan.Version != command.ExpectedVersion)
                    {
                        return ProductionPlanWriteResult.Fail(
                            ProductionPlanWriteFailureCodes.ConcurrencyConflict,
                            "The production plan has been modified by another user. Please reload and try again.");
                    }

                    var newReferenceCode = command.ReferenceCode.Trim();

                    if (!string.Equals(plan.ReferenceCode, newReferenceCode, StringComparison.Ordinal))
                    {
                        var isDuplicate = await dbContext.ProductionPlans
                            .AnyAsync(
                                p =>
                                    p.TenantId == context.TenantId
                                    && p.FacilityId == context.FacilityId
                                    && p.ReferenceCode == newReferenceCode
                                    && p.Id != command.ProductionPlanId,
                                ct);

                        if (isDuplicate)
                        {
                            return ProductionPlanWriteResult.Fail(
                                ProductionPlanWriteFailureCodes.ReferenceCodeConflict,
                                "A production plan with this reference code already exists in this facility.");
                        }
                    }

                    var now = DateTimeOffset.UtcNow;
                    var origin = ProductionPlanValueConversions.OriginFromStorage(command.Origin);
                    var confidence = command.Confidence is not null
                        ? ProductionPlanValueConversions.ConfidenceFromStorage(command.Confidence)
                        : plan.Confidence;

                    plan.ReferenceCode = newReferenceCode;
                    plan.Title = command.Title.Trim();
                    plan.Notes = command.Notes?.Trim();
                    plan.Origin = origin;
                    plan.AiGenerated = command.AiGenerated;
                    plan.Confidence = confidence;
                    plan.PlannedStartAtUtc = command.PlannedStartAtUtc;
                    plan.PlannedEndAtUtc = command.PlannedEndAtUtc;
                    plan.AiExplanation = command.AiExplanation?.Trim();
                    plan.UpdatedAtUtc = now;
                    plan.Version = plan.Version + 1;
                    plan.DemandsJson = SerializeDemands(command.Demands);
                    plan.RisksJson = SerializeRisks(command.Risks);
                    plan.ResourceAssignmentJson = SerializeResources(command.Resources);
                    plan.TimeWindowJson = SerializeTimeWindow(command.TimeWindow, command.PlannedStartAtUtc, command.PlannedEndAtUtc);

                    if (command.Approval is not null)
                    {
                        plan.ApprovalJson = SerializeApproval(command.Approval);
                    }

                    if (command.Transfer is not null)
                    {
                        plan.TransferJson = SerializeTransfer(command.Transfer);
                    }

                    plan.SourceReferencesJson = SerializeSourceReferences(command.SourceReferences);

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = AuditEventTypes.ProductionPlanDraftUpdated,
                        Action = AuditActions.ProductionPlanDraftUpdate,
                        ResourceType = "production-plan",
                        ResourceId = plan.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        CorrelationId = Guid.NewGuid(),
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            referenceCode = plan.ReferenceCode,
                            status = ProductionPlanValueConversions.ToStorage(plan.Status),
                            version = plan.Version,
                        }),
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionPlanWriteResult.Success(
                        plan.Id,
                        plan.ReferenceCode,
                        ProductionPlanValueConversions.ToStorage(plan.Status),
                        plan.Version,
                        plan.CreatedAtUtc,
                        plan.UpdatedAtUtc);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.ConcurrencyConflict,
                "The production plan has been modified by another user. Please reload and try again.");
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.ReferenceCodeConflict,
                "A production plan with this reference code already exists in this facility.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionPlanWriteResult.Fail(
                ProductionPlanWriteFailureCodes.SourceUnavailable,
                "Production plan could not be updated.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: "23505" };

    private static string SerializeDemands(IReadOnlyList<ProductionPlanDemandWriteModel> demands)
    {
        var items = (demands ?? []).Select(d => new
        {
            id = d.Id,
            productId = d.ProductId,
            productName = d.ProductName,
            requiredQuantity = d.RequiredQuantity,
            unit = d.Unit,
            priority = d.Priority,
            orderReference = d.OrderReference,
            customerId = d.CustomerId,
            customerName = d.CustomerName,
            productCode = d.ProductCode,
            requestedDeliveryDate = d.RequestedDeliveryDateUtc?.ToString("O"),
        });
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static string SerializeRisks(IReadOnlyList<ProductionPlanRiskWriteModel> risks)
    {
        var items = (risks ?? []).Select(r => new
        {
            id = r.Id,
            type = r.Type,
            severity = r.Severity,
            title = r.Title,
            description = r.Description,
            blocking = r.Blocking,
            source = r.Source,
        });
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static string SerializeResources(ProductionPlanResourceAssignmentWriteModel? resources)
    {
        if (resources is null)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(new
        {
            machineId = resources.MachineId,
            machineName = resources.MachineName,
            moldId = resources.MoldId,
            moldName = resources.MoldName,
            materialId = resources.MaterialId,
            materialName = resources.MaterialName,
            operatorGroupId = resources.OperatorGroupId,
            operatorGroupName = resources.OperatorGroupName,
        }, JsonOptions);
    }

    private static string SerializeTimeWindow(
        ProductionPlanTimeWindowWriteModel? timeWindow,
        DateTimeOffset plannedStartAtUtc,
        DateTimeOffset plannedEndAtUtc)
    {
        return JsonSerializer.Serialize(new
        {
            plannedStartAt = (timeWindow?.PlannedStartAtUtc ?? plannedStartAtUtc).ToString("O"),
            plannedEndAt = (timeWindow?.PlannedEndAtUtc ?? plannedEndAtUtc).ToString("O"),
            estimatedDurationMinutes = timeWindow?.EstimatedDurationMinutes,
            shiftId = timeWindow?.ShiftId,
            shiftName = timeWindow?.ShiftName,
        }, JsonOptions);
    }

    private static string SerializeApproval(ProductionPlanApprovalWriteModel? approval)
    {
        if (approval is null)
        {
            return JsonSerializer.Serialize(new { status = "not-required" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            status = approval.Status,
            requiredRole = approval.RequiredRole,
        }, JsonOptions);
    }

    private static string SerializeTransfer(ProductionPlanTransferWriteModel? transfer)
    {
        if (transfer is null)
        {
            return JsonSerializer.Serialize(new { status = "not-started" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            status = transfer.Status,
            targetSystem = transfer.TargetSystem,
        }, JsonOptions);
    }

    private static string SerializeSourceReferences(IReadOnlyList<ProductionPlanSourceReferenceWriteModel> sourceReferences)
    {
        var items = (sourceReferences ?? []).Select(s => new
        {
            sourceType = s.SourceType,
            sourceId = s.SourceId,
            sourceUpdatedAt = s.SourceUpdatedAtUtc?.ToString("O"),
        });
        return JsonSerializer.Serialize(items, JsonOptions);
    }
}

public sealed class UnavailableProductionPlanWriteRepository : IProductionPlanWriteRepository
{
    public Task<ProductionPlanWriteResult> CreateDraftAsync(
        ProductionPlanWriteContext context,
        CreateProductionPlanDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ProductionPlanWriteResult.Fail(
            ProductionPlanWriteFailureCodes.SourceUnavailable,
            "Production plan writes are unavailable because persistence is disabled."));

    public Task<ProductionPlanWriteResult> UpdateDraftAsync(
        ProductionPlanWriteContext context,
        UpdateProductionPlanDraftCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ProductionPlanWriteResult.Fail(
            ProductionPlanWriteFailureCodes.SourceUnavailable,
            "Production plan writes are unavailable because persistence is disabled."));
}
