import "./production-plan-server-boundary";

import {
  parsePlanningLogisticsEvaluationApiDto,
  type PlanningLogisticsEvaluationApiDto,
} from "./production-plan-logistics-api-contracts";
import {
  mapHttpStatusToProductionPlanApiFailure,
  parseProductionPlanApiProblemDetails,
  type ProductionPlanApiFailure,
} from "./production-plan-api-errors";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

export type PlanningLogisticsApiClientResult<T> =
  | Readonly<{
      status: "success";
      value: T;
    }>
  | ProductionPlanApiFailure
  | Readonly<{
      status: "not-found";
    }>;

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildLogisticsUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  facilityId: string,
  evaluate: boolean,
): string {
  const searchParams = new URLSearchParams();
  searchParams.set("facilityId", facilityId.trim());

  const suffix = evaluate
    ? `/api/production-planning/plans/${productionPlanId.trim()}/logistics-constraints/evaluate`
    : `/api/production-planning/plans/${productionPlanId.trim()}/logistics-constraints`;

  return `${trimTrailingSlash(context.apiBaseUrl)}${suffix}?${searchParams.toString()}`;
}

async function requestLogisticsEvaluation(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  facilityId: string,
  evaluate: boolean,
): Promise<PlanningLogisticsApiClientResult<PlanningLogisticsEvaluationApiDto>> {
  const response = await fetch(
    buildLogisticsUrl(
      context,
      productionPlanId,
      facilityId,
      evaluate,
    ),
    {
      method: evaluate ? "POST" : "GET",
      headers: {
        Accept: "application/json",
        ...context.requestHeaders,
      },
      cache: "no-store",
    },
  );

  if (response.status === 404) {
    return { status: "not-found" };
  }

  if (!response.ok) {
    const problem = await parseProductionPlanApiProblemDetails(response);
    return mapHttpStatusToProductionPlanApiFailure(
      response.status,
      problem,
    );
  }

  const payload: unknown = await response.json();
  const parsed = parsePlanningLogisticsEvaluationApiDto(payload);

  if (!parsed) {
    return {
      status: "failure",
      code: "invalid-response",
      message: "Planning logistics response could not be parsed.",
      retryable: false,
    };
  }

  return {
    status: "success",
    value: parsed,
  };
}

export async function fetchPlanningLogisticsEvaluation(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  facilityId: string,
): Promise<PlanningLogisticsApiClientResult<PlanningLogisticsEvaluationApiDto>> {
  return requestLogisticsEvaluation(
    context,
    productionPlanId,
    facilityId,
    false,
  );
}

export async function evaluatePlanningLogisticsConstraints(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  facilityId: string,
): Promise<PlanningLogisticsApiClientResult<PlanningLogisticsEvaluationApiDto>> {
  return requestLogisticsEvaluation(
    context,
    productionPlanId,
    facilityId,
    true,
  );
}
