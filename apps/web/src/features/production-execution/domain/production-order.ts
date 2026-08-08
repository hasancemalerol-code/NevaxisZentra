export type ProductionOrderStatus =
  | "ready"
  | "in-progress"
  | "paused"
  | "completed"
  | "cancelled";

export type ProductionQuantitySummary = Readonly<{
  plannedQuantity?: number;
  plannedQuantityUnit?: string;
  goodQuantity: number;
  scrapQuantity: number;
  grossProducedQuantity: number;
  remainingQuantity?: number;
  completionPercentage?: number;
  lastEntryAtUtc?: string;
}>;

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
  quantitySummary: ProductionQuantitySummary;
  totalDowntimeSeconds?: number;
  totalSetupSeconds?: number;
  manualCycleCount?: number;
  totalManualCycleDurationMilliseconds?: number;
  lastCycleAtUtc?: string;
  createdBySubjectId: string;
  lastModifiedBySubjectId: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  // 5L: shift/operator summary fields
  currentShiftId?: string;
  currentShiftCode?: string;
  currentShiftStartAtUtc?: string;
  currentShiftEndAtUtc?: string;
  primaryOperatorSubjectId?: string;
  activeOperatorCount?: number;
  lastAssignmentAtUtc?: string;
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
  plannedQuantity?: number;
  goodQuantity: number;
  scrapQuantity: number;
  totalDowntimeSeconds?: number;
  manualCycleCount?: number;
  version: number;
  updatedAtUtc: string;
  // 5L: shift/operator summary fields
  currentShiftCode?: string;
  primaryOperatorSubjectId?: string;
  activeOperatorCount?: number;
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
