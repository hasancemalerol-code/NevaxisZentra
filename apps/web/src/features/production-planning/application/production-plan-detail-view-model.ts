import type {
  ProductionPlan,
  ProductionPlanPriority,
  ProductionPlanTargetSystem,
} from "../domain/production-plan";
import {
  getProductionPlanDisplayValue,
  getProductionPlanFallbackLabel,
} from "../presentation/production-plan-fallback-labels";

export type ProductionPlanDetailViewModel =
  Readonly<{
    id: string;
    title: string;
    notes: string;
    facilityId: string;
    plannedStartAt: string | null;
    plannedEndAt: string | null;
    machineId: string | null;
    moldId: string | null;
    materialId: string | null;
    demandCount: number;
    primaryPriority: ProductionPlanPriority;
    targetSystem: ProductionPlanTargetSystem | null;
    missingResourceCount: number;
    hasCompleteResourceAssignment: boolean;
  }>;

function normalizeOptionalValue(
  value: string | null | undefined,
): string | null {
  const normalizedValue = value?.trim();

  return normalizedValue || null;
}

export function createProductionPlanDetailViewModel(
  productionPlan: ProductionPlan,
): ProductionPlanDetailViewModel {
  const machineId = normalizeOptionalValue(
    productionPlan.resourceAssignment.machineId,
  );

  const moldId = normalizeOptionalValue(
    productionPlan.resourceAssignment.moldId,
  );

  const materialId = normalizeOptionalValue(
    productionPlan.resourceAssignment.materialId,
  );

  const resourceAssignments = [
    machineId,
    moldId,
    materialId,
  ];

  const missingResourceCount =
    resourceAssignments.filter(
      (resourceId) => !resourceId,
    ).length;

  return {
    id: productionPlan.id,
    title: getProductionPlanDisplayValue(
      productionPlan.title,
      "notes",
    ),
    notes: getProductionPlanDisplayValue(
      productionPlan.notes,
      "notes",
    ),
    facilityId: getProductionPlanDisplayValue(
      productionPlan.facilityId,
      "facility",
    ),
    plannedStartAt: normalizeOptionalValue(
      productionPlan.timeWindow.plannedStartAt,
    ),
    plannedEndAt: normalizeOptionalValue(
      productionPlan.timeWindow.plannedEndAt,
    ),
    machineId,
    moldId,
    materialId,
    demandCount:
      productionPlan.demands.length,
    primaryPriority:
      productionPlan.demands[0]?.priority ??
      "normal",
    targetSystem:
      productionPlan.transfer.targetSystem ??
      null,
    missingResourceCount,
    hasCompleteResourceAssignment:
      missingResourceCount === 0,
  };
}

export function getProductionPlanResourceDisplayValue(
  resourceType:
    | "machine"
    | "mold"
    | "material",
  value: string | null,
): string {
  return (
    value ??
    getProductionPlanFallbackLabel(
      resourceType,
    )
  );
}