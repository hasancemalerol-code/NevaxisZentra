using System.Text.Json;
using NevaxisFactOS.Persistence.Erp;
using NevaxisFactOS.Persistence.Procurement;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotProcurementDeliveryRiskSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task EnsureProcurementDeliveryRiskPilotRecordsAsync(
        NevaxisFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await DevelopmentPilotErpPurchaseOrderSeed.EnsureErpPurchaseOrderPilotRecordsAsync(
            dbContext,
            tenantId,
            facilityId,
            seedTimestampUtc,
            cancellationToken);

        var scenarios = new[]
        {
            (
                DevelopmentPilotIds.ProcurementDeliveryRiskOverdueAssessmentId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderOverdueId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderOverdueLineId),
            (
                DevelopmentPilotIds.ProcurementDeliveryRiskDeliveryRiskAssessmentId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderDeliveryRiskId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderDeliveryRiskLineId),
            (
                DevelopmentPilotIds.ProcurementDeliveryRiskInsufficientAssessmentId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderInsufficientId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderInsufficientLineId),
            (
                DevelopmentPilotIds.ProcurementDeliveryRiskOnTrackAssessmentId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderOnTrackId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderOnTrackLineId),
        };

        foreach (var (assessmentId, purchaseOrderId, lineId) in scenarios)
        {
            var purchaseOrder = await dbContext.CanonicalErpPurchaseOrders.AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == purchaseOrderId
                         && p.TenantId == tenantId
                         && p.FacilityId == facilityId,
                    cancellationToken);

            var line = await dbContext.CanonicalErpPurchaseOrderLines.AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.Id == lineId
                         && l.TenantId == tenantId
                         && l.FacilityId == facilityId,
                    cancellationToken);

            if (purchaseOrder is null || line is null)
            {
                continue;
            }

            var result = ProcurementDeliveryRiskCalculator.AssessLine(
                purchaseOrder,
                line,
                seedTimestampUtc);

            await EnsureAssessmentAsync(
                dbContext,
                assessmentId,
                tenantId,
                facilityId,
                purchaseOrder,
                line,
                result,
                actorSubjectId,
                seedTimestampUtc,
                cancellationToken);
        }
    }

    private static async Task EnsureAssessmentAsync(
        NevaxisFactOSDbContext dbContext,
        Guid assessmentId,
        Guid tenantId,
        Guid facilityId,
        CanonicalErpPurchaseOrder purchaseOrder,
        CanonicalErpPurchaseOrderLine line,
        ProcurementDeliveryRiskAssessmentResult assessmentResult,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ProcurementDeliveryRiskAssessments.AsNoTracking()
                .AnyAsync(a => a.Id == assessmentId, cancellationToken))
        {
            return;
        }

        var inputJson = ProcurementDeliveryRiskIdempotencyFingerprint.SerializeAssessmentInput(assessmentResult);
        var fingerprint = ProcurementDeliveryRiskIdempotencyFingerprint.ComputeAssessmentInputFingerprint(
            line.Id,
            ProcurementDeliveryRiskConstants.RuleSetVersion,
            inputJson);

        if (await dbContext.ProcurementDeliveryRiskAssessments.AsNoTracking()
                .AnyAsync(
                    a => a.TenantId == tenantId
                         && a.PurchaseOrderLineId == line.Id
                         && a.InputFingerprint == fingerprint,
                    cancellationToken))
        {
            return;
        }

        var currentRows = await dbContext.ProcurementDeliveryRiskAssessments
            .Where(a => a.TenantId == tenantId
                        && a.FacilityId == facilityId
                        && a.PurchaseOrderLineId == line.Id
                        && a.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var current in currentRows)
        {
            current.IsCurrent = false;
        }

        dbContext.ProcurementDeliveryRiskAssessments.Add(new ProcurementDeliveryRiskAssessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            FacilityId = facilityId,
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
            CommitmentDateUtc = assessmentResult.CommitmentDateUtc,
            OrderedQuantity = assessmentResult.OrderedQuantity,
            ReceivedQuantity = assessmentResult.ReceivedQuantity,
            OpenQuantity = assessmentResult.OpenQuantity,
            Unit = assessmentResult.Unit,
            Result = assessmentResult.Result,
            RuleSetVersion = ProcurementDeliveryRiskConstants.RuleSetVersion,
            InputFingerprint = fingerprint,
            StructuredDrivers = JsonSerializer.Serialize(assessmentResult.Drivers, JsonOptions),
            StructuredEvidence = JsonSerializer.Serialize(assessmentResult.Evidence, JsonOptions),
            SourceUpdatedAtUtc = assessmentResult.SourceUpdatedAtUtc,
            SourceFreshness = assessmentResult.SourceFreshness,
            EvaluatedAtUtc = seedTimestampUtc,
            IsCurrent = true,
            Version = 1,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBySubjectId = actorSubjectId,
            FollowUpStatus = ProcurementDeliveryRiskFollowUpStatuses.RequiresReview,
        });
    }
}
