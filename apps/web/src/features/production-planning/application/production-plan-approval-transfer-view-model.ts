import type { ProductionPlan } from "../domain/production-plan";

export type ProductionPlanReadinessBlockerType =
  | "blocking-risk"
  | "missing-demand"
  | "missing-machine"
  | "missing-mold"
  | "missing-material"
  | "missing-start-time"
  | "missing-end-time"
  | "missing-target-system";

export type ProductionPlanReadinessBlocker =
  Readonly<{
    type: ProductionPlanReadinessBlockerType;
    title: string;
    description: string;
  }>;

export type ProductionPlanApprovalTransferViewModel =
  Readonly<{
    approvalStatus:
      ProductionPlan["approval"]["status"];
    transferStatus:
      ProductionPlan["transfer"]["status"];
    targetSystem:
      ProductionPlan["transfer"]["targetSystem"] |
      null;
    approvalBlockers:
      readonly ProductionPlanReadinessBlocker[];
    transferBlockers:
      readonly ProductionPlanReadinessBlocker[];
    hasApprovalBlockers: boolean;
    hasTransferBlockers: boolean;
    isReadyForApprovalReview: boolean;
    isReadyForTransferReview: boolean;
  }>;

function createBlocker(
  type: ProductionPlanReadinessBlockerType,
  title: string,
  description: string,
): ProductionPlanReadinessBlocker {
  return {
    type,
    title,
    description,
  };
}

function getSharedReadinessBlockers(
  productionPlan: ProductionPlan,
): ProductionPlanReadinessBlocker[] {
  const blockers:
    ProductionPlanReadinessBlocker[] = [];

  const blockingRiskCount =
    productionPlan.risks.filter(
      (risk) => risk.blocking,
    ).length;

  if (blockingRiskCount > 0) {
    blockers.push(
      createBlocker(
        "blocking-risk",
        "Engelleyici plan riski bulunuyor",
        `${blockingRiskCount} engelleyici risk çözülmeden işlem ilerletilemez.`,
      ),
    );
  }

  if (productionPlan.demands.length === 0) {
    blockers.push(
      createBlocker(
        "missing-demand",
        "Üretim talebi bulunmuyor",
        "Plan en az bir doğrulanmış üretim talebine bağlı olmalıdır.",
      ),
    );
  }

  if (
    !productionPlan.resourceAssignment.machineId
  ) {
    blockers.push(
      createBlocker(
        "missing-machine",
        "Makine ataması eksik",
        "Plan onayından önce uygun enjeksiyon makinesi atanmalıdır.",
      ),
    );
  }

  if (!productionPlan.resourceAssignment.moldId) {
    blockers.push(
      createBlocker(
        "missing-mold",
        "Kalıp ataması eksik",
        "Plan onayından önce üretimde kullanılacak kalıp atanmalıdır.",
      ),
    );
  }

  if (
    !productionPlan.resourceAssignment.materialId
  ) {
    blockers.push(
      createBlocker(
        "missing-material",
        "Malzeme ataması eksik",
        "Plan onayından önce gerekli hammadde veya malzeme atanmalıdır.",
      ),
    );
  }

  if (!productionPlan.timeWindow.plannedStartAt) {
    blockers.push(
      createBlocker(
        "missing-start-time",
        "Başlangıç zamanı eksik",
        "Üretim planının başlangıç zamanı belirlenmelidir.",
      ),
    );
  }

  if (!productionPlan.timeWindow.plannedEndAt) {
    blockers.push(
      createBlocker(
        "missing-end-time",
        "Bitiş zamanı eksik",
        "Üretim planının planlanan bitiş zamanı belirlenmelidir.",
      ),
    );
  }

  return blockers;
}

export function createProductionPlanApprovalTransferViewModel(
  productionPlan: ProductionPlan,
): ProductionPlanApprovalTransferViewModel {
  const approvalBlockers =
    getSharedReadinessBlockers(productionPlan);

  const transferBlockers = [
    ...approvalBlockers,
  ];

  const targetSystem =
    productionPlan.transfer.targetSystem ??
    null;

  if (!targetSystem) {
    transferBlockers.push(
      createBlocker(
        "missing-target-system",
        "Aktarım hedefi belirlenmedi",
        "Yetkili aktarım öncesinde hedef ERP veya MES sistemi seçilmelidir.",
      ),
    );
  }

  return {
    approvalStatus:
      productionPlan.approval.status,
    transferStatus:
      productionPlan.transfer.status,
    targetSystem,
    approvalBlockers,
    transferBlockers,
    hasApprovalBlockers:
      approvalBlockers.length > 0,
    hasTransferBlockers:
      transferBlockers.length > 0,
    isReadyForApprovalReview:
      approvalBlockers.length === 0,
    isReadyForTransferReview:
      transferBlockers.length === 0,
  };
}