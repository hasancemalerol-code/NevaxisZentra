import type { ProductionPlan } from "../domain/production-plan";

export const PRODUCTION_PLAN_PREVIEW_DATA =
  [
    {
      id: "production-plan-001",
      tenantId: "tenant-NevaxisZentra",
      facilityId: "facility-istanbul",
      referenceCode: "PREV-001",
      version: 1,
      title:
        "Kaya Endüstriyel — Acil bağlantı parçası üretimi",
      status: "needs-review",
      origin: "approved-order",
      confidence: "medium",
      createdAt:
        "2026-08-05T07:45:00.000Z",
      updatedAt:
        "2026-08-05T09:20:00.000Z",
      createdBy: "ai-production-agent",
      sourceReferences: [
        {
          sourceType: "order-draft",
          sourceId: "order-draft-002",
          sourceUpdatedAt:
            "2026-08-05T07:40:00.000Z",
        },
      ],
      demands: [
        {
          id: "demand-001",
          orderReference: "ORD-2026-00841",
          customerId: "customer-kaya",
          customerName:
            "Kaya Endüstriyel Ürünler Ltd. Şti.",
          productId: "product-connector-42",
          productCode: "BP-042",
          productName:
            "Teknik bağlantı parçası",
          requiredQuantity: 18_000,
          unit: "adet",
          requestedDeliveryDate:
            "2026-08-08T14:00:00.000Z",
          priority: "urgent",
        },
      ],
      resourceAssignment: {
        machineId: "machine-im-07",
        machineName: "IM-07 / 220 Ton",
        moldId: "mold-bp-042",
        moldName: "BP-042 / 8 Göz",
        materialId: "material-pa66-gf30",
        materialName: "PA66 GF30 Siyah",
      },
      timeWindow: {
        plannedStartAt:
          "2026-08-06T05:00:00.000Z",
        plannedEndAt:
          "2026-08-07T17:00:00.000Z",
        estimatedDurationMinutes: 2_160,
        shiftId: "shift-a-b",
        shiftName: "A ve B vardiyaları",
      },
      risks: [
        {
          id: "risk-001",
          type: "material-shortage",
          severity: "critical",
          title:
            "Hammadde miktarı üretim ihtiyacını karşılamıyor",
          description:
            "ERP stok verisine göre gerekli PA66 GF30 miktarında 185 kg açık bulunuyor.",
          blocking: true,
          source: "erp-material-stock",
        },
        {
          id: "risk-002",
          type: "delivery-risk",
          severity: "warning",
          title:
            "Termin tamponu yetersiz",
          description:
            "Planlanan üretim bitişi ile müşteri teslim tarihi arasında yalnızca 21 saat bulunuyor.",
          blocking: false,
          source: "ai-planning-agent",
        },
      ],
      approval: {
        status: "not-required",
        requiredRole: "planning-manager",
      },
      transfer: {
        targetSystem: "mes",
        status: "not-started",
      },
      aiGenerated: true,
      aiExplanation:
        "Plan, onaylı sipariş talebi, makine-kalıp uyumluluğu ve mevcut kapasite verileri kullanılarak oluşturuldu.",
      notes:
        "Hammadde açığı çözülmeden onaya gönderilmemeli.",
    },
    {
      id: "production-plan-002",
      tenantId: "tenant-NevaxisZentra",
      facilityId: "facility-istanbul",
      referenceCode: "PREV-002",
      version: 2,
      title:
        "Atlas Otomotiv — Trim klipsi üretim planı",
      status: "ready-for-approval",
      origin: "ai-recommendation",
      confidence: "high",
      createdAt:
        "2026-08-04T11:10:00.000Z",
      updatedAt:
        "2026-08-05T08:30:00.000Z",
      createdBy: "ai-production-agent",
      sourceReferences: [
        {
          sourceType: "erp-order",
          sourceId: "ERP-SO-45872",
          sourceUpdatedAt:
            "2026-08-04T10:55:00.000Z",
        },
      ],
      demands: [
        {
          id: "demand-002",
          orderReference: "ERP-SO-45872",
          customerId: "customer-atlas",
          customerName: "Atlas Otomotiv A.Ş.",
          productId: "product-trim-clip-18",
          productCode: "TC-018",
          productName: "İç trim sabitleme klipsi",
          requiredQuantity: 42_000,
          unit: "adet",
          requestedDeliveryDate:
            "2026-08-12T12:00:00.000Z",
          priority: "high",
        },
      ],
      resourceAssignment: {
        machineId: "machine-im-12",
        machineName: "IM-12 / 160 Ton",
        moldId: "mold-tc-018",
        moldName: "TC-018 / 16 Göz",
        materialId: "material-pom-natural",
        materialName: "POM Natural",
        operatorGroupId: "operator-group-2",
        operatorGroupName:
          "Enjeksiyon Operatör Grubu 2",
      },
      timeWindow: {
        plannedStartAt:
          "2026-08-09T21:00:00.000Z",
        plannedEndAt:
          "2026-08-10T17:00:00.000Z",
        estimatedDurationMinutes: 1_200,
        shiftId: "shift-c-a",
        shiftName: "C ve A vardiyaları",
      },
      risks: [
        {
          id: "risk-003",
          type: "maintenance-risk",
          severity: "information",
          title:
            "Makine bakım zamanı yaklaşıyor",
          description:
            "IM-12 makinesinin planlı bakımı üretim tamamlandıktan 36 saat sonra başlayacak.",
          blocking: false,
          source: "maintenance-system",
        },
      ],
      approval: {
        status: "pending",
        requiredRole: "planning-manager",
        requestedAt:
          "2026-08-05T08:30:00.000Z",
        requestedBy: "ai-production-agent",
      },
      transfer: {
        targetSystem: "mes",
        status: "pending",
      },
      aiGenerated: true,
      aiExplanation:
        "Makine, kalıp, operatör grubu ve termin uygunluğu birlikte değerlendirilerek düşük çakışmalı zaman aralığı seçildi.",
    },
    {
      id: "production-plan-003",
      tenantId: "tenant-NevaxisZentra",
      facilityId: "facility-istanbul",
      referenceCode: "PREV-003",
      version: 3,
      title:
        "Nova Beyaz Eşya — Pompa gövdesi üretimi",
      status: "approved",
      origin: "erp-production-request",
      confidence: "high",
      createdAt:
        "2026-08-03T06:20:00.000Z",
      updatedAt:
        "2026-08-05T06:55:00.000Z",
      createdBy: "planning-user-01",
      sourceReferences: [
        {
          sourceType: "erp-order",
          sourceId: "ERP-PR-77104",
          sourceUpdatedAt:
            "2026-08-03T06:15:00.000Z",
        },
      ],
      demands: [
        {
          id: "demand-003",
          orderReference: "ERP-PR-77104",
          customerId: "customer-nova",
          customerName:
            "Nova Beyaz Eşya Sanayi A.Ş.",
          productId: "product-pump-body-09",
          productCode: "PB-009",
          productName: "Pompa gövdesi",
          requiredQuantity: 12_500,
          unit: "adet",
          requestedDeliveryDate:
            "2026-08-15T09:00:00.000Z",
          priority: "normal",
        },
      ],
      resourceAssignment: {
        machineId: "machine-im-03",
        machineName: "IM-03 / 300 Ton",
        moldId: "mold-pb-009",
        moldName: "PB-009 / 4 Göz",
        materialId: "material-pp-gf20",
        materialName: "PP GF20 Gri",
        operatorGroupId: "operator-group-1",
        operatorGroupName:
          "Enjeksiyon Operatör Grubu 1",
      },
      timeWindow: {
        plannedStartAt:
          "2026-08-11T05:00:00.000Z",
        plannedEndAt:
          "2026-08-12T13:00:00.000Z",
        estimatedDurationMinutes: 1_920,
        shiftId: "shift-a-b",
        shiftName: "A ve B vardiyaları",
      },
      risks: [],
      approval: {
        status: "approved",
        requiredRole: "planning-manager",
        requestedAt:
          "2026-08-04T13:00:00.000Z",
        requestedBy: "planning-user-01",
        approvedAt:
          "2026-08-05T06:55:00.000Z",
        approvedBy: "planning-manager-01",
      },
      transfer: {
        targetSystem: "mes",
        status: "not-started",
      },
      aiGenerated: false,
      notes:
        "Plan onaylandı; MES aktarımı yetkili kullanıcı işlemini bekliyor.",
    },
  ] as const satisfies readonly ProductionPlan[];

export function findProductionPlanById(
  productionPlanId: string,
): ProductionPlan | undefined {
  return PRODUCTION_PLAN_PREVIEW_DATA.find(
    (productionPlan) =>
      productionPlan.id === productionPlanId,
  );
}
