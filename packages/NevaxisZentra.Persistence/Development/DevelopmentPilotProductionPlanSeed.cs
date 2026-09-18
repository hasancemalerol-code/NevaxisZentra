using System.Globalization;
using NevaxisZentra.Persistence.ProductionPlanning;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotProductionPlanSeed
{
    internal static IReadOnlyList<ProductionPlan> CreatePlans(
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc)
    {
        return
        [
            CreateDraftPlan(tenantId, facilityId, createdBy, seedTimestampUtc),
            CreateNeedsReviewPlan(tenantId, facilityId, createdBy, seedTimestampUtc),
            CreateScheduledPlan(tenantId, facilityId, createdBy, seedTimestampUtc),
            CreateBlockingRiskPlan(tenantId, facilityId, createdBy, seedTimestampUtc),
        ];
    }

    private static ProductionPlan CreateDraftPlan(
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc) =>
        CreatePlan(
            DevelopmentPilotIds.PlanDraftId,
            tenantId,
            facilityId,
            "PILOT-PLAN-001",
            "Pilot — Polipropilen kapak taslak planı",
            ProductionPlanStatus.Draft,
            ProductionPlanOrigin.AiRecommendation,
            ProductionPlanConfidence.Medium,
            aiGenerated: true,
            createdBy,
            seedTimestampUtc.AddDays(-2),
            seedTimestampUtc.AddDays(-1),
            """
            [{"id":"demand-pilot-001","productId":"product-pp-cap-32","productCode":"PP-CAP-32","productName":"32mm polipropilen kapak","requiredQuantity":48000,"unit":"adet","priority":"normal","orderReference":"PILOT-REQ-001","customerName":"Pilot OEM A","requestedDeliveryDate":"2026-08-20T12:00:00.000Z"}]
            """,
            """
            [{"id":"risk-pilot-001","type":"data-missing","severity":"information","title":"Operatör grubu henüz atanmadı","description":"Vardiya planı tamamlanmadan operatör grubu seçilmedi.","blocking":false,"source":"pilot-seed"}]
            """,
            """
            {"machineId":"machine-im-120","machineName":"IM-120 / 350 Ton","moldId":"mold-pp-cap-32","moldName":"PP-CAP-32 / 16 Göz","materialId":"material-pp-homopolymer","materialName":"PP Homopolimer Natural"}
            """,
            """
            {"plannedStartAt":"2026-08-18T06:00:00.000Z","plannedEndAt":"2026-08-19T18:00:00.000Z","estimatedDurationMinutes":2160,"shiftId":"shift-a-b","shiftName":"A ve B vardiyaları"}
            """,
            """{"status":"not-required","requiredRole":"planning-manager"}""",
            """{"status":"not-started","targetSystem":"mes"}""",
            DateTimeOffset.Parse("2026-08-18T06:00:00.000Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-08-19T18:00:00.000Z", CultureInfo.InvariantCulture),
            "AI taslak planı enjeksiyon makinesi, kalıp ve hammadde uyumluluğuna göre oluşturuldu.",
            "Development pilot taslak planı.");

    private static ProductionPlan CreateNeedsReviewPlan(
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc) =>
        CreatePlan(
            DevelopmentPilotIds.PlanNeedsReviewId,
            tenantId,
            facilityId,
            "PILOT-PLAN-002",
            "Pilot — ABS gövde parçası inceleme planı",
            ProductionPlanStatus.NeedsReview,
            ProductionPlanOrigin.ApprovedOrder,
            ProductionPlanConfidence.High,
            aiGenerated: false,
            createdBy,
            seedTimestampUtc.AddDays(-3),
            seedTimestampUtc,
            """
            [{"id":"demand-pilot-002","productId":"product-abs-body-18","productCode":"ABS-BODY-18","productName":"ABS gövde parçası","requiredQuantity":12000,"unit":"adet","priority":"high","orderReference":"PILOT-ORD-002","customerName":"Pilot OEM B","requestedDeliveryDate":"2026-08-14T08:00:00.000Z"}]
            """,
            """
            [{"id":"risk-pilot-002","type":"delivery-risk","severity":"warning","title":"Termin tamponu dar","description":"Planlanan bitiş ile teslim tarihi arasında 18 saat bulunuyor.","blocking":false,"source":"pilot-seed"}]
            """,
            """
            {"machineId":"machine-im-180","machineName":"IM-180 / 500 Ton","moldId":"mold-abs-body-18","moldName":"ABS-BODY-18 / 2 Göz","materialId":"material-abs-natural","materialName":"ABS Natural","operatorGroupId":"operator-group-a","operatorGroupName":"A vardiyası operatörleri"}
            """,
            """
            {"plannedStartAt":"2026-08-10T05:00:00.000Z","plannedEndAt":"2026-08-12T17:00:00.000Z","estimatedDurationMinutes":3600,"shiftId":"shift-a","shiftName":"A vardiyası"}
            """,
            """{"status":"not-required","requiredRole":"planning-manager"}""",
            """{"status":"not-started","targetSystem":"erp"}""",
            DateTimeOffset.Parse("2026-08-10T05:00:00.000Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-08-12T17:00:00.000Z", CultureInfo.InvariantCulture),
            null,
            "Onaylı sipariş talebine göre hazırlanan inceleme planı.");

    private static ProductionPlan CreateScheduledPlan(
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc) =>
        CreatePlan(
            DevelopmentPilotIds.PlanScheduledId,
            tenantId,
            facilityId,
            "PILOT-PLAN-003",
            "Pilot — ERP üretim talebi planlanmış run",
            ProductionPlanStatus.Scheduled,
            ProductionPlanOrigin.ErpProductionRequest,
            ProductionPlanConfidence.High,
            aiGenerated: false,
            createdBy,
            seedTimestampUtc.AddDays(-5),
            seedTimestampUtc.AddHours(-6),
            """
            [{"id":"demand-pilot-003","productId":"product-pom-wheel-55","productCode":"POM-WHEEL-55","productName":"POM dişli çark","requiredQuantity":9600,"unit":"adet","priority":"normal","orderReference":"PILOT-ORD-EXPORT-001","customerName":"Nordic Components GmbH","requestedDeliveryDate":"2026-08-22T16:00:00.000Z"}]
            """,
            "[]",
            """
            {"machineId":"machine-im-90","machineName":"IM-90 / 250 Ton","moldId":"mold-pom-wheel-55","moldName":"POM-WHEEL-55 / 4 Göz","materialId":"material-pom-natural","materialName":"POM Natural Delrin","operatorGroupId":"operator-group-b","operatorGroupName":"B vardiyası operatörleri"}
            """,
            """
            {"plannedStartAt":"2026-08-16T05:00:00.000Z","plannedEndAt":"2026-08-17T13:00:00.000Z","estimatedDurationMinutes":1920,"shiftId":"shift-b","shiftName":"B vardiyası"}
            """,
            """{"status":"approved","requiredRole":"planning-manager","approvedAt":"2026-08-06T10:00:00.000Z","approvedBy":"planning-manager"}""",
            """{"status":"pending","targetSystem":"mes"}""",
            DateTimeOffset.Parse("2026-08-16T05:00:00.000Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-08-17T13:00:00.000Z", CultureInfo.InvariantCulture),
            null,
            "ERP üretim talebinden planlanmış pilot run.");

    private static ProductionPlan CreateBlockingRiskPlan(
        Guid tenantId,
        Guid facilityId,
        string createdBy,
        DateTimeOffset seedTimestampUtc) =>
        CreatePlan(
            DevelopmentPilotIds.PlanBlockingRiskId,
            tenantId,
            facilityId,
            "PILOT-PLAN-004",
            "Pilot — Masterbatch eksikliği engelli plan",
            ProductionPlanStatus.NeedsReview,
            ProductionPlanOrigin.ApprovedOrder,
            ProductionPlanConfidence.Medium,
            aiGenerated: true,
            createdBy,
            seedTimestampUtc.AddDays(-1),
            seedTimestampUtc.AddHours(-2),
            """
            [{"id":"demand-pilot-004","productId":"product-pp-housing-77","productCode":"PP-HOUSING-77","productName":"PP konut parçası","requiredQuantity":24000,"unit":"adet","priority":"urgent","orderReference":"PILOT-ORD-004","customerName":"Pilot OEM D","requestedDeliveryDate":"2026-08-11T06:00:00.000Z"}]
            """,
            """
            [{"id":"risk-pilot-004a","type":"material-shortage","severity":"critical","title":"Masterbatch stoğu yetersiz","description":"Siyah masterbatch için 420 kg açık bulunuyor.","blocking":true,"source":"pilot-seed"},{"id":"risk-pilot-004b","type":"mold-conflict","severity":"critical","title":"Kalıp başka planda rezerve","description":"Aynı kalıp PILOT-PLAN-002 ile çakışıyor.","blocking":true,"source":"pilot-seed"},{"id":"risk-pilot-004c","type":"maintenance-risk","severity":"warning","title":"Makine bakım penceresi yakın","description":"IM-120 için planlanan bakım 6 saat sonra başlıyor.","blocking":false,"source":"pilot-seed"}]
            """,
            """
            {"machineId":"machine-im-120","machineName":"IM-120 / 350 Ton","moldId":"mold-pp-housing-77","moldName":"PP-HOUSING-77 / 8 Göz","materialId":"material-pp-masterbatch-black","materialName":"PP + Siyah Masterbatch"}
            """,
            """
            {"plannedStartAt":"2026-08-09T05:00:00.000Z","plannedEndAt":"2026-08-10T21:00:00.000Z","estimatedDurationMinutes":2400,"shiftId":"shift-a-b","shiftName":"A ve B vardiyaları"}
            """,
            """{"status":"pending","requiredRole":"planning-manager","requestedAt":"2026-08-06T08:00:00.000Z","requestedBy":"planning-pilot-user"}""",
            """{"status":"not-started","targetSystem":"mes"}""",
            DateTimeOffset.Parse("2026-08-09T05:00:00.000Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-08-10T21:00:00.000Z", CultureInfo.InvariantCulture),
            "Masterbatch ve kalıp çakışması nedeniyle plan onaya gönderilemez.",
            "Engelleyici risk içeren pilot plan.");

    private static ProductionPlan CreatePlan(
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string referenceCode,
        string title,
        ProductionPlanStatus status,
        ProductionPlanOrigin origin,
        ProductionPlanConfidence confidence,
        bool aiGenerated,
        string createdBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string demandsJson,
        string risksJson,
        string resourceAssignmentJson,
        string timeWindowJson,
        string approvalJson,
        string transferJson,
        DateTimeOffset plannedStartAtUtc,
        DateTimeOffset plannedEndAtUtc,
        string? aiExplanation,
        string? notes) => new()
    {
        Id = id,
        TenantId = tenantId,
        FacilityId = facilityId,
        ReferenceCode = referenceCode,
        Title = title,
        Status = status,
        Origin = origin,
        AiGenerated = aiGenerated,
        Confidence = confidence,
        PlannedStartAtUtc = plannedStartAtUtc,
        PlannedEndAtUtc = plannedEndAtUtc,
        Notes = notes,
        ApprovalStatus = ParseApprovalStatus(approvalJson),
        TransferStatus = ParseTransferStatus(transferJson),
        TransferTargetSystem = ParseTransferTarget(transferJson),
        CreatedBy = createdBy,
        AiExplanation = aiExplanation,
        CreatedAtUtc = createdAtUtc,
        UpdatedAtUtc = updatedAtUtc,
        Version = 1,
        DemandsJson = demandsJson,
        RisksJson = risksJson,
        ResourceAssignmentJson = resourceAssignmentJson,
        TimeWindowJson = timeWindowJson,
        ApprovalJson = approvalJson,
        TransferJson = transferJson,
        SourceReferencesJson =
            """[{"sourceType":"manual-request","sourceId":"pilot-source-001","sourceUpdatedAt":"2026-08-06T07:00:00.000Z"}]""",
        HistoryJson =
            $$"""[{"id":"history-pilot-{{referenceCode}}","type":"created","title":"Plan oluşturuldu","description":"Development pilot plan kaydı.","occurredAt":"{{createdAtUtc:O}}"}]""",
    };

    private static ProductionPlanApprovalStatus ParseApprovalStatus(string approvalJson)
    {
        if (approvalJson.Contains("\"pending\"", StringComparison.Ordinal))
        {
            return ProductionPlanApprovalStatus.Pending;
        }

        if (approvalJson.Contains("\"approved\"", StringComparison.Ordinal))
        {
            return ProductionPlanApprovalStatus.Approved;
        }

        return ProductionPlanApprovalStatus.NotRequired;
    }

    private static ProductionPlanTransferStatus ParseTransferStatus(string transferJson)
    {
        if (transferJson.Contains("\"pending\"", StringComparison.Ordinal))
        {
            return ProductionPlanTransferStatus.Pending;
        }

        return ProductionPlanTransferStatus.NotStarted;
    }

    private static ProductionPlanTargetSystem? ParseTransferTarget(string transferJson)
    {
        if (transferJson.Contains("\"mes\"", StringComparison.Ordinal))
        {
            return ProductionPlanTargetSystem.Mes;
        }

        if (transferJson.Contains("\"erp\"", StringComparison.Ordinal))
        {
            return ProductionPlanTargetSystem.Erp;
        }

        return null;
    }
}
