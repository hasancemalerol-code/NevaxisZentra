import {
  evaluatePlanningLogisticsConstraints,
  fetchPlanningLogisticsEvaluation,
} from "./production-plan-logistics-api-client";
import {
  createPlanningLogisticsCardSummary,
  type PlanningLogisticsCardSummary,
} from "../presentation/production-plan-logistics-formatters";
import type { PlanningLogisticsEvaluationApiDto } from "./production-plan-logistics-api-contracts";
import {
  resolveProductionPlanRequestContextForIncomingRequest,
  type ProductionPlanRequestContext,
} from "./production-plan-request-context";
import {
  parseProductionPlanRuntimeConfig,
  type ProductionPlanRuntimeConfigFailure,
} from "./production-plan-runtime-config";
import {
  resolveProductionPlanningServerScope,
  type ProductionPlanningScopeResolutionFailure,
} from "./production-plan-scope";

export type PlanningLogisticsReadFailure =
  | ProductionPlanningScopeResolutionFailure
  | ProductionPlanRuntimeConfigFailure
  | Readonly<{
      status: "failure";
      code: string;
      message: string;
    }>;

export type PlanningLogisticsReadResult =
  | Readonly<{
      status: "success";
      evaluation: PlanningLogisticsEvaluationApiDto;
      cardSummary: PlanningLogisticsCardSummary;
    }>
  | Readonly<{
      status: "not-found";
    }>
  | PlanningLogisticsReadFailure;

type PlanningLogisticsDependencies = Readonly<{
  requestContext: ProductionPlanRequestContext;
  facilityId: string;
}>;

async function resolveDependencies(): Promise<
  PlanningLogisticsDependencies | PlanningLogisticsReadFailure
> {
  const runtimeConfig = parseProductionPlanRuntimeConfig();
  if (runtimeConfig.status === "failure") {
    return runtimeConfig;
  }

  const scope = resolveProductionPlanningServerScope(
    runtimeConfig.config,
  );
  if (scope.status === "failure") {
    return scope;
  }

  const requestContextResult = await resolveProductionPlanRequestContextForIncomingRequest(
    runtimeConfig.config,
    scope.scope,
  );
  if (requestContextResult.status === "failure") {
    return {
      status: "failure",
      code: requestContextResult.code,
      message: requestContextResult.message,
    };
  }

  return {
    requestContext: requestContextResult.context,
    facilityId: scope.scope.facilityId,
  };
}

function isPlanningLogisticsDependencies(
  value: PlanningLogisticsDependencies | PlanningLogisticsReadFailure,
): value is PlanningLogisticsDependencies {
  return "requestContext" in value;
}

export async function readPlanningLogisticsEvaluation(
  productionPlanId: string,
  options?: Readonly<{ evaluateIfMissing?: boolean }>,
): Promise<PlanningLogisticsReadResult> {
  const dependencies = await resolveDependencies();
  if (!isPlanningLogisticsDependencies(dependencies)) {
    return dependencies;
  }

  const { requestContext, facilityId } = dependencies;

  let result = await fetchPlanningLogisticsEvaluation(
    requestContext,
    productionPlanId,
    facilityId,
  );

  if (
    result.status === "not-found" &&
    options?.evaluateIfMissing
  ) {
    result = await evaluatePlanningLogisticsConstraints(
      requestContext,
      productionPlanId,
      facilityId,
    );
  }

  if (result.status === "not-found") {
    return { status: "not-found" };
  }

  if (result.status === "failure") {
    return {
      status: "failure",
      code: result.code,
      message: result.message,
    };
  }

  return {
    status: "success",
    evaluation: result.value,
    cardSummary: createPlanningLogisticsCardSummary(result.value),
  };
}

export async function readPlanningLogisticsSummaries(
  productionPlanIds: readonly string[],
): Promise<
  ReadonlyMap<string, PlanningLogisticsCardSummary>
> {
  const dependencies = await resolveDependencies();
  if (!isPlanningLogisticsDependencies(dependencies)) {
    return new Map();
  }

  const { requestContext, facilityId } = dependencies;
  const summaries = new Map<string, PlanningLogisticsCardSummary>();

  await Promise.all(
    productionPlanIds.map(async (planId) => {
      const result = await fetchPlanningLogisticsEvaluation(
        requestContext,
        planId,
        facilityId,
      );

      if (result.status === "success") {
        summaries.set(
          planId,
          createPlanningLogisticsCardSummary(result.value),
        );
      }
    }),
  );

  return summaries;
}

export async function evaluatePlanningLogisticsForPlan(
  productionPlanId: string,
): Promise<PlanningLogisticsReadResult> {
  const dependencies = await resolveDependencies();
  if (!isPlanningLogisticsDependencies(dependencies)) {
    return dependencies;
  }

  const { requestContext, facilityId } = dependencies;
  const result = await evaluatePlanningLogisticsConstraints(
    requestContext,
    productionPlanId,
    facilityId,
  );

  if (result.status === "not-found") {
    return { status: "not-found" };
  }

  if (result.status === "failure") {
    return {
      status: "failure",
      code: result.code,
      message: result.message,
    };
  }

  return {
    status: "success",
    evaluation: result.value,
    cardSummary: createPlanningLogisticsCardSummary(result.value),
  };
}
