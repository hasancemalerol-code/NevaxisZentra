import type { ProductionPlan } from "../domain/production-plan";

export type ProductionPlanDetailQueryResult =
  | Readonly<{
      status: "found";
      productionPlan: ProductionPlan;
    }>
  | Readonly<{
      status: "not-found";
      productionPlan: null;
    }>;

function normalizeProductionPlanId(
  productionPlanId: string,
): string {
  return productionPlanId.trim();
}

export function findProductionPlanById(
  productionPlans: readonly ProductionPlan[],
  productionPlanId: string,
): ProductionPlan | undefined {
  const normalizedProductionPlanId =
    normalizeProductionPlanId(
      productionPlanId,
    );

  if (!normalizedProductionPlanId) {
    return undefined;
  }

  const matchingProductionPlans =
    productionPlans.filter(
      (productionPlan) =>
        productionPlan.id ===
        normalizedProductionPlanId,
    );

  if (matchingProductionPlans.length > 1) {
    throw new Error(
      [
        "Production plan identity violation.",
        `Multiple plans use id "${normalizedProductionPlanId}".`,
      ].join(" "),
    );
  }

  return matchingProductionPlans[0];
}

export function queryProductionPlanDetail(
  productionPlans: readonly ProductionPlan[],
  productionPlanId: string,
): ProductionPlanDetailQueryResult {
  const productionPlan =
    findProductionPlanById(
      productionPlans,
      productionPlanId,
    );

  if (!productionPlan) {
    return {
      status: "not-found",
      productionPlan: null,
    };
  }

  return {
    status: "found",
    productionPlan,
  };
}

export function requireProductionPlanById(
  productionPlans: readonly ProductionPlan[],
  productionPlanId: string,
): ProductionPlan {
  const result = queryProductionPlanDetail(
    productionPlans,
    productionPlanId,
  );

  if (result.status === "not-found") {
    throw new Error(
      `Production plan "${productionPlanId.trim()}" was not found.`,
    );
  }

  return result.productionPlan;
}