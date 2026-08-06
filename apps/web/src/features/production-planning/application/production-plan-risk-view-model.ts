import type {
  ProductionPlan,
  ProductionPlanRisk,
} from "../domain/production-plan";

export type ProductionPlanRiskViewItem =
  Readonly<{
    id: string;
    title: string;
    description: string;
    severity: ProductionPlanRisk["severity"];
    type: ProductionPlanRisk["type"];
    isBlocking: boolean;
  }>;

export type ProductionPlanRiskViewModel =
  Readonly<{
    items: readonly ProductionPlanRiskViewItem[];
    blockingItems: readonly ProductionPlanRiskViewItem[];
    advisoryItems: readonly ProductionPlanRiskViewItem[];
    totalCount: number;
    blockingCount: number;
    advisoryCount: number;
    hasRisks: boolean;
    hasBlockingRisks: boolean;
  }>;

function normalizeRiskText(
  value: string | null | undefined,
  fallback: string,
): string {
  return value?.trim() || fallback;
}

function createRiskViewItem(
  risk: ProductionPlanRisk,
): ProductionPlanRiskViewItem {
  return {
    id: risk.id,
    title: normalizeRiskText(
      risk.title,
      "Tanımlanmamış plan riski",
    ),
    description: normalizeRiskText(
      risk.description,
      "Bu risk için açıklama bulunmuyor.",
    ),
    severity: risk.severity,
    type: risk.type,
    isBlocking: risk.blocking,
  };
}

export function createProductionPlanRiskViewModel(
  productionPlan: ProductionPlan,
): ProductionPlanRiskViewModel {
  const items = productionPlan.risks.map(
    createRiskViewItem,
  );

  const blockingItems = items.filter(
    (item) => item.isBlocking,
  );

  const advisoryItems = items.filter(
    (item) => !item.isBlocking,
  );

  return {
    items,
    blockingItems,
    advisoryItems,
    totalCount: items.length,
    blockingCount: blockingItems.length,
    advisoryCount: advisoryItems.length,
    hasRisks: items.length > 0,
    hasBlockingRisks:
      blockingItems.length > 0,
  };
}