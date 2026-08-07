import "./production-plan-server-boundary";

import {
  parseFailureCode,
  parseProductionPlanDraftFieldErrors,
  parseProductionPlanDraftWriteSuccess,
  type ProductionPlanDraftUpdateRequestBody,
  type ProductionPlanDraftWriteRequestBody,
  type ProductionPlanDraftWriteResult,
} from "./production-plan-draft-api-contracts";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildCreateUrl(context: ProductionPlanRequestContext): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans`;
}

function buildUpdateUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(productionPlanId.trim())}`;
}

async function readResponseBody(response: Response): Promise<unknown> {
  const text = await response.text();

  if (!text.trim()) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

export async function createProductionPlanDraft(
  context: ProductionPlanRequestContext,
  body: ProductionPlanDraftWriteRequestBody,
): Promise<ProductionPlanDraftWriteResult> {
  if (!context.apiBaseUrl.trim()) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "API base URL is not configured.",
    };
  }

  const url = buildCreateUrl(context);

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

  if (response.status === 201) {
    return parseProductionPlanDraftWriteSuccess(responseBody);
  }

  const failureCode = parseFailureCode(responseBody);
  const fieldErrors =
    failureCode === "validation-failed"
      ? parseProductionPlanDraftFieldErrors(responseBody)
      : undefined;

  return {
    status: "failure",
    code: failureCode,
    message: extractTitle(responseBody) ?? "The production plan could not be created.",
    fieldErrors,
  };
}

export async function updateProductionPlanDraft(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: ProductionPlanDraftUpdateRequestBody,
): Promise<ProductionPlanDraftWriteResult> {
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

  const url = buildUpdateUrl(context, productionPlanId);

  let response: Response;

  try {
    response = await fetch(url, {
      method: "PUT",
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
    return parseProductionPlanDraftWriteSuccess(responseBody);
  }

  const failureCode = parseFailureCode(responseBody);
  const fieldErrors =
    failureCode === "validation-failed"
      ? parseProductionPlanDraftFieldErrors(responseBody)
      : undefined;

  return {
    status: "failure",
    code: failureCode,
    message: extractTitle(responseBody) ?? "The production plan could not be updated.",
    fieldErrors,
  };
}

function extractTitle(value: unknown): string | undefined {
  if (
    typeof value === "object" &&
    value !== null &&
    "title" in value &&
    typeof (value as Record<string, unknown>).title === "string"
  ) {
    return (value as Record<string, unknown>).title as string;
  }
  return undefined;
}
