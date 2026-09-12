import "./production-plan-server-boundary";

import {
  parseScheduleFailure,
  parseScheduleSuccess,
  type ProductionPlanScheduleRequestBody,
  type ProductionPlanScheduleResult,
} from "./production-plan-scheduling-api-contracts";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildScheduleUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(productionPlanId.trim())}/schedule?facilityId=${encodeURIComponent(context.facilityId)}`;
}

async function readResponseBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text.trim()) return null;
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

export async function scheduleProductionPlan(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: ProductionPlanScheduleRequestBody,
): Promise<ProductionPlanScheduleResult> {
  if (!context.apiBaseUrl.trim()) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "API base URL is not configured.",
    };
  }

  if (!productionPlanId.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Production plan identifier is required.",
    };
  }

  const url = buildScheduleUrl(context, productionPlanId);

  let response: Response;

  try {
    response = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        ...context.requestHeaders,
      },
      body: JSON.stringify(body),
      cache: "no-store",
    });
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "The production planning service is unavailable.",
    };
  }

  const responseBody = await readResponseBody(response);

  if (response.status === 200) {
    return parseScheduleSuccess(responseBody);
  }

  return parseScheduleFailure(responseBody, "The production plan could not be scheduled.");
}
