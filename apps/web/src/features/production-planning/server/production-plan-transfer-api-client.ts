import "./production-plan-server-boundary";

import {
  parseCreateTransferRequestSuccess,
  parseExecuteTransferSuccess,
  parseTransferExecuteFailure,
  parseTransferFailure,
  type CreateTransferRequestBody,
  type ExecuteTransferRequestBody,
  type ProductionPlanTransferExecuteResult,
  type ProductionPlanTransferRequestResult,
} from "./production-plan-transfer-api-contracts";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildTransferRequestUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(productionPlanId.trim())}/transfer-requests?facilityId=${encodeURIComponent(context.facilityId)}`;
}

function buildTransferExecuteUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  transferRequestId: string,
): string {
  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(productionPlanId.trim())}/transfer-requests/${encodeURIComponent(transferRequestId.trim())}/execute?facilityId=${encodeURIComponent(context.facilityId)}`;
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

export async function createTransferRequest(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  body: CreateTransferRequestBody,
): Promise<ProductionPlanTransferRequestResult> {
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

  const url = buildTransferRequestUrl(context, productionPlanId);

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
    return parseCreateTransferRequestSuccess(responseBody);
  }

  return parseTransferFailure(responseBody, "The transfer request could not be created.");
}

export async function executeTransfer(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  transferRequestId: string,
  body: ExecuteTransferRequestBody,
): Promise<ProductionPlanTransferExecuteResult> {
  if (!context.apiBaseUrl.trim()) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "API base URL is not configured.",
    };
  }

  if (!productionPlanId.trim() || !transferRequestId.trim()) {
    return {
      status: "failure",
      code: "invalid-request",
      message: "Production plan and transfer request identifiers are required.",
    };
  }

  const url = buildTransferExecuteUrl(context, productionPlanId, transferRequestId);

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

  if (response.status === 200) {
    return parseExecuteTransferSuccess(responseBody);
  }

  return parseTransferExecuteFailure(responseBody, "The transfer could not be executed.");
}
