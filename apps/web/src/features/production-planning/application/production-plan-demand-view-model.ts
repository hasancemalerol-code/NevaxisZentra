import type {
  ProductionPlan,
  ProductionPlanDemand,
  ProductionPlanPriority,
} from "../domain/production-plan";
import {
  getProductionPlanDisplayValue,
  getProductionPlanFallbackLabel,
} from "../presentation/production-plan-fallback-labels";

export type ProductionPlanDemandViewItem = Readonly<{
  id: string;
  orderReference: string;
  productId: string;
  productName: string;
  quantity: number;
  unit: string;
  requestedDeliveryDate: string | null;
  priority: ProductionPlanPriority;
}>;

export type ProductionPlanDemandViewModel = Readonly<{
  items: readonly ProductionPlanDemandViewItem[];
  totalCount: number;
  totalQuantity: number;
  hasDemands: boolean;
}>;

function normalizeOptionalText(
  value: string | null | undefined,
): string | null {
  const normalizedValue = value?.trim();

  return normalizedValue || null;
}

function createDemandViewItem(
  demand: ProductionPlanDemand,
): ProductionPlanDemandViewItem {
  const quantity =
    Number.isFinite(demand.requiredQuantity) &&
    demand.requiredQuantity > 0
      ? demand.requiredQuantity
      : 0;

  return {
    id: demand.id,
    orderReference: getProductionPlanDisplayValue(
      demand.orderReference,
      "orderReference",
    ),
    productId: getProductionPlanDisplayValue(
      demand.productId,
      "productId",
    ),
    productName: getProductionPlanDisplayValue(
      demand.productName,
      "productName",
    ),
    quantity,
    unit:
      demand.unit?.trim() ||
      "adet",
    requestedDeliveryDate:
      normalizeOptionalText(
        demand.requestedDeliveryDate,
      ),
    priority: demand.priority,
  };
}

export function createProductionPlanDemandViewModel(
  productionPlan: ProductionPlan,
): ProductionPlanDemandViewModel {
  const items = productionPlan.demands.map(
    createDemandViewItem,
  );

  const totalQuantity = items.reduce(
    (total, item) =>
      total + item.quantity,
    0,
  );

  return {
    items,
    totalCount: items.length,
    totalQuantity,
    hasDemands: items.length > 0,
  };
}

export function getProductionPlanDemandDeliveryLabel(
  requestedDeliveryDate: string | null,
): string {
  return (
    requestedDeliveryDate ??
    getProductionPlanFallbackLabel(
      "deliveryDate",
    )
  );
}