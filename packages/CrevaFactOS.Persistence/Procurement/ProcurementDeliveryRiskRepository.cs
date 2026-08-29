using System.Text.Json;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Procurement;

public sealed class ProcurementDeliveryRiskRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IProcurementDeliveryRiskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ProcurementDeliveryRiskWorkspaceReadModel> GetWorkspaceAsync(
        ErpOperationContext context,
        ProcurementDeliveryRiskListFilters filters,
        bool canManage,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.ProcurementDeliveryRiskAssessments
                .AsNoTracking()
                .Where(a => a.TenantId == context.TenantId
                            && a.FacilityId == context.FacilityId
                            && a.IsCurrent);

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                query = query.Where(a => a.Result == filters.Status.Trim());
            }

            if (!string.IsNullOrWhiteSpace(filters.SupplierReference))
            {
                var supplier = filters.SupplierReference.Trim();
                query = query.Where(a =>
                    a.ExternalSupplierCode == supplier
                    || a.ExternalSupplierId == supplier
                    || (a.SupplierName != null && a.SupplierName.Contains(supplier)));
            }

            if (!string.IsNullOrWhiteSpace(filters.PurchaseOrderReference))
            {
                var reference = filters.PurchaseOrderReference.Trim();
                query = query.Where(a => a.PurchaseOrderNumber.Contains(reference));
            }

            if (!string.IsNullOrWhiteSpace(filters.SourceFreshness))
            {
                query = query.Where(a => a.SourceFreshness == filters.SourceFreshness.Trim());
            }

            var allRows = await query
                .OrderByDescending(a => a.EvaluatedAtUtc)
                .ThenBy(a => a.PurchaseOrderNumber)
                .ToListAsync(ct);

            var page = Math.Max(1, filters.Page);
            var pageSize = Math.Clamp(filters.PageSize, 1, 100);
            var paged = allRows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapRow)
                .ToList();

            return new ProcurementDeliveryRiskWorkspaceReadModel(
                ProcurementDeliveryRiskConstants.RuleSetVersion,
                ProcurementDeliveryRiskConstants.WorkspaceDisclaimer,
                allRows.Count(a => a.Result == ProcurementDeliveryRiskStatuses.Overdue),
                allRows.Count(a => a.Result == ProcurementDeliveryRiskStatuses.DeliveryRisk),
                allRows.Count(a => a.Result == ProcurementDeliveryRiskStatuses.OnTrack),
                allRows.Count(a => a.Result == ProcurementDeliveryRiskStatuses.InsufficientData),
                allRows.Count(a => a.Result == ProcurementDeliveryRiskStatuses.SourceConflict),
                allRows.MaxBy(a => a.EvaluatedAtUtc)?.EvaluatedAtUtc,
                true,
                canManage,
                paged);
        }, cancellationToken);

    public Task<ProcurementDeliveryRiskDetailReadModel?> GetAssessmentByIdAsync(
        ErpOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var assessment = await dbContext.ProcurementDeliveryRiskAssessments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.Id == assessmentId
                         && a.TenantId == context.TenantId
                         && a.FacilityId == context.FacilityId,
                    ct);

            return assessment is null ? null : MapDetail(assessment);
        }, cancellationToken);

    public Task<IReadOnlyList<ProcurementDeliveryRiskAssessmentSummaryReadModel>> ListAssessmentHistoryAsync(
        ErpOperationContext context,
        Guid purchaseOrderLineId,
        int limit,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var normalizedLimit = Math.Clamp(limit, 1, 100);
            var rows = await dbContext.ProcurementDeliveryRiskAssessments
                .AsNoTracking()
                .Where(a => a.TenantId == context.TenantId
                            && a.FacilityId == context.FacilityId
                            && a.PurchaseOrderLineId == purchaseOrderLineId)
                .OrderByDescending(a => a.EvaluatedAtUtc)
                .Take(normalizedLimit)
                .ToListAsync(ct);

            return (IReadOnlyList<ProcurementDeliveryRiskAssessmentSummaryReadModel>)rows
                .Select(a => new ProcurementDeliveryRiskAssessmentSummaryReadModel(
                    a.Id,
                    a.Result,
                    a.EvaluatedAtUtc,
                    a.RuleSetVersion))
                .ToList();
        }, cancellationToken);

    public Task<ProcurementDeliveryRiskWriteResult> EvaluateAsync(
        ErpOperationContext context,
        EvaluateProcurementDeliveryRiskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var line = await dbContext.CanonicalErpPurchaseOrderLines
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        l => l.Id == command.PurchaseOrderLineId
                             && l.TenantId == context.TenantId
                             && l.FacilityId == context.FacilityId,
                        ct);

                if (line is null)
                {
                    return ProcurementDeliveryRiskWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.NotFound,
                        "Purchase order line not found.");
                }

                var purchaseOrder = await dbContext.CanonicalErpPurchaseOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Id == line.PurchaseOrderId
                             && p.TenantId == context.TenantId
                             && p.FacilityId == context.FacilityId,
                        ct);

                if (purchaseOrder is null)
                {
                    return ProcurementDeliveryRiskWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.NotFound,
                        "Purchase order not found.");
                }

                return await EvaluateAndPersistAsync(
                    context,
                    purchaseOrder,
                    line,
                    command.IdempotencyKey,
                    ct);
            },
            cancellationToken);

    public Task<ProcurementDeliveryRiskWriteResult> EvaluateAllOpenLinesAsync(
        ErpOperationContext context,
        string? idempotencyKeyPrefix,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var openLines = await (
                    from line in dbContext.CanonicalErpPurchaseOrderLines.AsNoTracking()
                    join order in dbContext.CanonicalErpPurchaseOrders.AsNoTracking()
                        on line.PurchaseOrderId equals order.Id
                    where line.TenantId == context.TenantId
                          && line.FacilityId == context.FacilityId
                          && (order.Status == ErpPurchaseOrderStatuses.Open
                              || order.Status == ErpPurchaseOrderStatuses.PartiallyReceived)
                    select new { line, order }).ToListAsync(ct);

                if (openLines.Count == 0)
                {
                    return ProcurementDeliveryRiskWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.ValidationFailed,
                        "No open purchase order lines found to evaluate.");
                }

                ProcurementDeliveryRiskWriteResult? lastResult = null;
                var index = 0;
                foreach (var item in openLines)
                {
                    var key = string.IsNullOrWhiteSpace(idempotencyKeyPrefix)
                        ? null
                        : $"{idempotencyKeyPrefix.Trim()}:{item.line.Id:N}:{index}";
                    lastResult = await EvaluateAndPersistAsync(
                        context,
                        item.order,
                        item.line,
                        key,
                        ct);
                    if (!lastResult.Succeeded)
                    {
                        return lastResult;
                    }

                    index++;
                }

                return lastResult!;
            },
            cancellationToken);

    private async Task<ProcurementDeliveryRiskWriteResult> EvaluateAndPersistAsync(
        ErpOperationContext context,
        CanonicalErpPurchaseOrder purchaseOrder,
        CanonicalErpPurchaseOrderLine line,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var assessmentResult = ProcurementDeliveryRiskCalculator.AssessLine(purchaseOrder, line, now);
        var inputJson = ProcurementDeliveryRiskIdempotencyFingerprint.SerializeAssessmentInput(assessmentResult);
        var fingerprint = ProcurementDeliveryRiskIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            line.Id,
            ProcurementDeliveryRiskConstants.RuleSetVersion,
            inputJson);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var trimmedKey = idempotencyKey.Trim();
            var existingByKey = await dbContext.ProcurementDeliveryRiskAssessments
                .FirstOrDefaultAsync(
                    a => a.TenantId == context.TenantId
                         && a.PurchaseOrderLineId == line.Id
                         && a.IdempotencyKey == trimmedKey,
                    cancellationToken);

            if (existingByKey is not null)
            {
                if (existingByKey.InputFingerprint != fingerprint)
                {
                    return ProcurementDeliveryRiskWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.AssessmentConcurrencyConflict,
                        "The idempotency key was already used with a different evaluation input.");
                }

                return ProcurementDeliveryRiskWriteResult.Success(
                    existingByKey.Id,
                    existingByKey.Version,
                    idempotentReplay: true);
            }
        }

        var existingByFingerprint = await dbContext.ProcurementDeliveryRiskAssessments
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.PurchaseOrderLineId == line.Id
                     && a.InputFingerprint == fingerprint,
                cancellationToken);

        if (existingByFingerprint is not null)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var trimmedKey = idempotencyKey.Trim();
                if (!string.IsNullOrWhiteSpace(existingByFingerprint.IdempotencyKey)
                    && !string.Equals(existingByFingerprint.IdempotencyKey, trimmedKey, StringComparison.Ordinal))
                {
                    return ProcurementDeliveryRiskWriteResult.Fail(
                        ProcurementDeliveryRiskFailureCodes.AssessmentConcurrencyConflict,
                        "The idempotency key was already used with a different evaluation input.");
                }
            }

            return ProcurementDeliveryRiskWriteResult.Success(
                existingByFingerprint.Id,
                existingByFingerprint.Version,
                idempotentReplay: true);
        }

        var currentRows = await dbContext.ProcurementDeliveryRiskAssessments
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.PurchaseOrderLineId == line.Id
                        && a.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var current in currentRows)
        {
            current.IsCurrent = false;
        }

        var assessment = BuildAssessmentEntity(
            context,
            purchaseOrder,
            line,
            assessmentResult,
            fingerprint,
            idempotencyKey,
            now);

        dbContext.ProcurementDeliveryRiskAssessments.Add(assessment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var replay = await dbContext.ProcurementDeliveryRiskAssessments
                .FirstOrDefaultAsync(
                    a => a.TenantId == context.TenantId
                         && a.PurchaseOrderLineId == line.Id
                         && a.InputFingerprint == fingerprint,
                    cancellationToken);

            if (replay is not null)
            {
                return ProcurementDeliveryRiskWriteResult.Success(
                    replay.Id,
                    replay.Version,
                    idempotentReplay: true);
            }

            return ProcurementDeliveryRiskWriteResult.Fail(
                ProcurementDeliveryRiskFailureCodes.AssessmentConcurrencyConflict,
                "Concurrent evaluation conflict.");
        }

        return ProcurementDeliveryRiskWriteResult.Success(assessment.Id, assessment.Version);
    }

    private static ProcurementDeliveryRiskAssessment BuildAssessmentEntity(
        ErpOperationContext context,
        CanonicalErpPurchaseOrder purchaseOrder,
        CanonicalErpPurchaseOrderLine line,
        ProcurementDeliveryRiskAssessmentResult result,
        string fingerprint,
        string? idempotencyKey,
        DateTimeOffset evaluatedAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderLineId = line.Id,
            PurchaseOrderNumber = purchaseOrder.PurchaseOrderNumber,
            ExternalSupplierId = purchaseOrder.ExternalSupplierId,
            ExternalSupplierCode = purchaseOrder.ExternalSupplierCode,
            SupplierName = purchaseOrder.SupplierName,
            OrderStatus = purchaseOrder.Status,
            LineNumber = line.LineNumber,
            ExternalProductId = line.ExternalProductId,
            ExternalProductCode = line.ExternalProductCode,
            LineDescription = line.Description,
            CommitmentDateUtc = result.CommitmentDateUtc,
            OrderedQuantity = result.OrderedQuantity,
            ReceivedQuantity = result.ReceivedQuantity,
            OpenQuantity = result.OpenQuantity,
            Unit = result.Unit,
            Result = result.Result,
            RuleSetVersion = ProcurementDeliveryRiskConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            StructuredDrivers = JsonSerializer.Serialize(result.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(result.Evidence, JsonOptions),
            SourceUpdatedAtUtc = result.SourceUpdatedAtUtc,
            SourceFreshness = result.SourceFreshness,
            EvaluatedAtUtc = evaluatedAtUtc,
            IsCurrent = true,
            Version = 1,
            CreatedAtUtc = evaluatedAtUtc,
            CreatedBySubjectId = context.IdentitySubjectId.ToString(),
        };

    private static ProcurementDeliveryRiskRowReadModel MapRow(ProcurementDeliveryRiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.PurchaseOrderId,
            assessment.PurchaseOrderLineId,
            assessment.PurchaseOrderNumber,
            assessment.ExternalSupplierCode,
            assessment.SupplierName,
            assessment.CommitmentDateUtc,
            assessment.ReceivedQuantity,
            assessment.OpenQuantity,
            assessment.Unit,
            assessment.Result,
            assessment.SourceFreshness,
            assessment.EvaluatedAtUtc,
            assessment.Version);

    private static ProcurementDeliveryRiskDetailReadModel MapDetail(ProcurementDeliveryRiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.FacilityId,
            assessment.PurchaseOrderId,
            assessment.PurchaseOrderLineId,
            assessment.PurchaseOrderNumber,
            assessment.ExternalSupplierId,
            assessment.ExternalSupplierCode,
            assessment.SupplierName,
            assessment.OrderStatus,
            assessment.LineNumber,
            assessment.ExternalProductId,
            assessment.ExternalProductCode,
            assessment.LineDescription,
            assessment.CommitmentDateUtc,
            assessment.OrderedQuantity,
            assessment.ReceivedQuantity,
            assessment.OpenQuantity,
            assessment.Unit,
            assessment.Result,
            assessment.RuleSetVersion,
            assessment.InputFingerprint,
            assessment.SourceUpdatedAtUtc,
            assessment.SourceFreshness,
            DeserializeDrivers(assessment.StructuredDrivers),
            DeserializeEvidence(assessment.StructuredEvidence),
            assessment.EvaluatedAtUtc,
            assessment.Version);

    private static List<ProcurementDeliveryRiskDriverReadModel> DeserializeDrivers(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<ProcurementDeliveryRiskDriverReadModel>>(json, JsonOptions) ?? [];
    }

    private static List<ProcurementDeliveryRiskEvidenceReadModel> DeserializeEvidence(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<ProcurementDeliveryRiskEvidenceReadModel>>(json, JsonOptions) ?? [];
    }
}
