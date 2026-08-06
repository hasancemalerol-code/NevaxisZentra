import type { ProductionPlanRepository } from "../application/production-plan-repository";
import { PRODUCTION_PLAN_PREVIEW_DATA } from "../data/production-plan-preview-data";
import { createProductionPlanPreviewRepository } from "../data/production-plan-preview-repository";
import type { ProductionPlanningServerScope } from "./production-plan-scope";

export function createProductionPlanRepository(
  scope: ProductionPlanningServerScope,
): ProductionPlanRepository {
  void scope;

  return createProductionPlanPreviewRepository({
    productionPlans:
      PRODUCTION_PLAN_PREVIEW_DATA,
  });
}
