export type ProductionOrderStatus =
  | "ready"
  | "in-progress"
  | "paused"
  | "completed"
  | "cancelled";

export type ProductionOrder = Readonly<{
  id: string;
  facilityId: string;
  productionPlanHandoffId: string;
  sourceProductionPlanId: string;
  sourceProductionPlanVersion: number;
  referenceCode: string;
  title: string;
  status: ProductionOrderStatus;
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  actualStartAtUtc?: string;
  actualEndAtUtc?: string;
  shiftName?: string;
  machineReference?: string;
  moldReference?: string;
  productReference?: string;
  createdBySubjectId: string;
  lastModifiedBySubjectId: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type ProductionOrderSummary = Readonly<{
  id: string;
  facilityId: string;
  sourceProductionPlanId: string;
  referenceCode: string;
  title: string;
  status: ProductionOrderStatus;
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  actualStartAtUtc?: string;
  machineReference?: string;
  moldReference?: string;
  shiftName?: string;
  version: number;
  updatedAtUtc: string;
}>;

export const ACTIVE_PRODUCTION_ORDER_STATUSES: ReadonlySet<ProductionOrderStatus> =
  new Set(["ready", "in-progress", "paused"]);

export function isProductionOrderActive(
  status: ProductionOrderStatus,
): boolean {
  return ACTIVE_PRODUCTION_ORDER_STATUSES.has(status);
}

export function getProductionOrderStatusLabel(
  status: ProductionOrderStatus,
): string {
  switch (status) {
    case "ready":
      return "Hazır";
    case "in-progress":
      return "Üretimde";
    case "paused":
      return "Duraklatıldı";
    case "completed":
      return "Tamamlandı";
    case "cancelled":
      return "İptal edildi";
  }
}
