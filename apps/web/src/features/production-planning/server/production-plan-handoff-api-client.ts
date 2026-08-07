import "./production-plan-server-boundary";

import {
  parseHandoffSuccess,
  parseHandoffFailure,
  type PrepareHandoffBody,
  type ProductionPlanHandoffResult,
} from "./production-plan-handoff-api-contracts";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildHandoffUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(productionPlanId.trim())}/handoff?facilityId=${encodeURIComponent(context.facilityId)}`;
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

export async function prepareHandoff(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: PrepareHandoffBody,
): Promise<ProductionPlanHandoffResult> {
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

  const url = buildHandoffUrl(context, productionPlanId);

  // eslint-disable-next-line @typescript-eslint/init-declarations
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

  if (response.status === 201 || response.status === 200) {
    return parseHandoffSuccess(responseBody);
  }

  return parseHandoffFailure(responseBody, "The handoff could not be prepared.");
}
