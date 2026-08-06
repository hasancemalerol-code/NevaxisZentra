import type { ProductionPlan } from "../domain/production-plan";
import {
  readProductionPlanDetail,
  type ProductionPlanDetailReadResult,
} from "../application/production-plan-detail-query";
import {
  readProductionPlans,
} from "../application/production-plan-query";
import type {
  ProductionPlanRepositoryFailure,
  ProductionPlanRepositoryListResult,
} from "../application/production-plan-repository";
import { createProductionPlanRepository } from "./production-plan-repository-factory";
import {
  resolveProductionPlanningServerScope,
  type ProductionPlanningScopeResolutionFailure,
} from "./production-plan-scope";

export type ProductionPlanningWorkspaceReadResult =
  | Extract<
      ProductionPlanRepositoryListResult,
      { status: "success" }
    >
  | ProductionPlanRepositoryFailure
  | ProductionPlanningScopeResolutionFailure;

export type ProductionPlanningDetailReadResult =
  ProductionPlanDetailReadResult
  | ProductionPlanningScopeResolutionFailure;

function createInvalidProductionPlanIdFailure(
  message: string,
): ProductionPlanRepositoryFailure {
  return {
    status: "failure",
    code: "invalid-scope",
    message,
    retryable: false,
  };
}

function normalizeProductionPlanId(
  productionPlanId: string,
): string {
  return productionPlanId.trim();
}

export async function readProductionPlanningWorkspace(): Promise<ProductionPlanningWorkspaceReadResult> {
  const scopeResult =
    resolveProductionPlanningServerScope();

  if (scopeResult.status === "failure") {
    return scopeResult;
  }

  const repository =
    createProductionPlanRepository(
      scopeResult.scope,
    );

  return readProductionPlans(
    repository,
    scopeResult.scope,
  );
}

export async function readProductionPlanningDetail(
  productionPlanId: string,
): Promise<ProductionPlanningDetailReadResult> {
  const normalizedProductionPlanId =
    normalizeProductionPlanId(
      productionPlanId,
    );

  if (!normalizedProductionPlanId) {
    return createInvalidProductionPlanIdFailure(
      "productionPlanId is required.",
    );
  }

  const scopeResult =
    resolveProductionPlanningServerScope();

  if (scopeResult.status === "failure") {
    return scopeResult;
  }

  const repository =
    createProductionPlanRepository(
      scopeResult.scope,
    );

  return readProductionPlanDetail(
    repository,
    {
      scope: scopeResult.scope,
      productionPlanId:
        normalizedProductionPlanId,
    },
  );
}

export type {
  ProductionPlan,
};
