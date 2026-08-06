import "./production-plan-server-boundary";

import type { ProductionPlanRepository } from "../application/production-plan-repository";
import { PRODUCTION_PLAN_PREVIEW_DATA } from "../data/production-plan-preview-data";
import { createProductionPlanPreviewRepository } from "../data/production-plan-preview-repository";
import { createProductionPlanApiRepository } from "./production-plan-api-repository";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";
import type { ProductionPlanRuntimeConfig } from "./production-plan-runtime-config";
import type { ProductionPlanningServerScope } from "./production-plan-scope";

export type CreateProductionPlanRepositoryInput =
  Readonly<{
    runtimeConfig: ProductionPlanRuntimeConfig;
    requestContext: ProductionPlanRequestContext | null;
    scope: ProductionPlanningServerScope;
  }>;

export function createProductionPlanRepository({
  runtimeConfig,
  requestContext,
  scope,
}: CreateProductionPlanRepositoryInput): ProductionPlanRepository {
  if (
    runtimeConfig.mode ===
    "development-preview"
  ) {
    void scope;

    return createProductionPlanPreviewRepository(
      {
        productionPlans:
          PRODUCTION_PLAN_PREVIEW_DATA,
      },
    );
  }

  if (!requestContext) {
    throw new Error(
      "API request context is required for production planning API repository creation.",
    );
  }

  void scope;

  return createProductionPlanApiRepository(
    requestContext,
  );
}
