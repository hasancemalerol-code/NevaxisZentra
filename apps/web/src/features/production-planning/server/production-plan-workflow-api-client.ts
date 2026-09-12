import "./production-plan-server-boundary";

import {
  parseProductionPlanWorkflowSuccess,
  parseWorkflowFailure,
  type ProductionPlanApproveRequestBody,
  type ProductionPlanRejectRequestBody,
  type ProductionPlanRequestRevisionRequestBody,
  type ProductionPlanSubmitForReviewRequestBody,
  type ProductionPlanWorkflowResult,
} from "./production-plan-workflow-api-contracts";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildWorkflowUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  action: string,
): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(productionPlanId.trim())}/${action}?facilityId=${encodeURIComponent(context.facilityId)}`;
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

async function executeWorkflowPost(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  action: string,
  body: object,
  fallbackErrorMessage: string,
): Promise<ProductionPlanWorkflowResult> {
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

  const url = buildWorkflowUrl(context, productionPlanId, action);

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
    return parseProductionPlanWorkflowSuccess(responseBody);
  }

  return parseWorkflowFailure(responseBody, fallbackErrorMessage);
}

export async function submitProductionPlanForReview(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: Omit<ProductionPlanSubmitForReviewRequestBody, "facilityId">,
): Promise<ProductionPlanWorkflowResult> {
  return executeWorkflowPost(
    context,
    productionPlanId,
    "submit-for-review",
    body,
    "The production plan could not be submitted for review.",
  );
}

export async function requestProductionPlanRevision(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: Omit<ProductionPlanRequestRevisionRequestBody, "facilityId">,
): Promise<ProductionPlanWorkflowResult> {
  return executeWorkflowPost(
    context,
    productionPlanId,
    "request-revision",
    body,
    "Revision could not be requested.",
  );
}

export async function approveProductionPlan(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: Omit<ProductionPlanApproveRequestBody, "facilityId">,
): Promise<ProductionPlanWorkflowResult> {
  return executeWorkflowPost(
    context,
    productionPlanId,
    "approve",
    body,
    "The production plan could not be approved.",
  );
}

export async function rejectProductionPlan(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: Omit<ProductionPlanRejectRequestBody, "facilityId">,
): Promise<ProductionPlanWorkflowResult> {
  return executeWorkflowPost(
    context,
    productionPlanId,
    "reject",
    body,
    "The production plan could not be rejected.",
  );
}
