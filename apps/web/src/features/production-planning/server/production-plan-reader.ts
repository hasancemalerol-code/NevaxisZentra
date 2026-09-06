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
  resolveProductionPlanRequestContextForIncomingRequest,
  type ProductionPlanRequestContext,
  type ProductionPlanRequestContextFailure,
} from "./production-plan-request-context";
import {
  parseProductionPlanRuntimeConfig,
  type ProductionPlanRuntimeConfigFailure,
} from "./production-plan-runtime-config";
import {
  resolveProductionPlanningServerScope,
  type ProductionPlanningScopeResolutionFailure,
} from "./production-plan-scope";

export type ProductionPlanningReadFailure =
  | ProductionPlanRepositoryFailure
  | ProductionPlanningScopeResolutionFailure
  | ProductionPlanRuntimeConfigFailure
  | ProductionPlanRequestContextFailure;

export type ProductionPlanningWorkspaceReadResult =
  | Extract<
      ProductionPlanRepositoryListResult,
      { status: "success" }
    >
  | ProductionPlanningReadFailure;

export type ProductionPlanningDetailReadResult =
  | ProductionPlanDetailReadResult
  | ProductionPlanningReadFailure;

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

function mapRequestContextFailure(
  failure: ProductionPlanRequestContextFailure,
): ProductionPlanRepositoryFailure {
  if (failure.code === "unauthenticated") {
    return {
      status: "failure",
      code: "unauthenticated",
      message: failure.message,
      retryable: false,
    };
  }

  if (failure.code === "configuration-error") {
    return {
      status: "failure",
      code: "configuration-error",
      message: failure.message,
      retryable: false,
    };
  }

  return {
    status: "failure",
    code: "invalid-scope",
    message: failure.message,
    retryable: false,
  };
}

async function resolveProductionPlanningReadDependencies(): Promise<
  | Readonly<{
      status: "success";
      runtimeConfig: NonNullable<
        Extract<
          ReturnType<
            typeof parseProductionPlanRuntimeConfig
          >,
          { status: "success" }
        >["config"]
      >;
      scope: NonNullable<
        Extract<
          ReturnType<
            typeof resolveProductionPlanningServerScope
          >,
          { status: "success" }
        >["scope"]
      >;
      requestContext: ProductionPlanRequestContext | null;
    }>
  | ProductionPlanningReadFailure
> {
  const runtimeConfigResult =
    parseProductionPlanRuntimeConfig();

  if (
    runtimeConfigResult.status === "failure"
  ) {
    return runtimeConfigResult;
  }

  const scopeResult =
    resolveProductionPlanningServerScope(
      runtimeConfigResult.config,
    );

  if (scopeResult.status === "failure") {
    return scopeResult;
  }

  if (
    runtimeConfigResult.config.mode ===
    "development-preview"
  ) {
    return {
      status: "success",
      runtimeConfig:
        runtimeConfigResult.config,
      scope: scopeResult.scope,
      requestContext: null,
    };
  }

  const requestContextResult =
    await resolveProductionPlanRequestContextForIncomingRequest(
      runtimeConfigResult.config,
      scopeResult.scope,
    );

  if (
    requestContextResult.status === "failure"
  ) {
    return mapRequestContextFailure(
      requestContextResult,
    );
  }

  return {
    status: "success",
    runtimeConfig: runtimeConfigResult.config,
    scope: scopeResult.scope,
    requestContext:
      requestContextResult.context,
  };
}

export async function readProductionPlanningWorkspace(): Promise<ProductionPlanningWorkspaceReadResult> {
  const dependencies =
    await resolveProductionPlanningReadDependencies();

  if (dependencies.status === "failure") {
    return dependencies;
  }

  const repository =
    createProductionPlanRepository({
      runtimeConfig:
        dependencies.runtimeConfig,
      requestContext:
        dependencies.requestContext,
      scope: dependencies.scope,
    });

  return readProductionPlans(
    repository,
    dependencies.scope,
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

  const dependencies =
    await resolveProductionPlanningReadDependencies();

  if (dependencies.status === "failure") {
    return dependencies;
  }

  const repository =
    createProductionPlanRepository({
      runtimeConfig:
        dependencies.runtimeConfig,
      requestContext:
        dependencies.requestContext,
      scope: dependencies.scope,
    });

  return readProductionPlanDetail(
    repository,
    {
      scope: dependencies.scope,
      productionPlanId:
        normalizedProductionPlanId,
    },
  );
}

export type {
  ProductionPlan,
};
