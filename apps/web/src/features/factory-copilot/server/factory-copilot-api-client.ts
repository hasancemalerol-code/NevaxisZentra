import "./factory-copilot-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import { classifyFailedResponse, type ClassifiedApiFailure } from "@/lib/api/api-failure-classifier";

import type {
  FactoryCopilotContextInput,
  FactoryCopilotQueryRequest,
  FactoryCopilotQueryResult,
  FactoryCopilotSuggestedQuestionsResult,
} from "../domain/factory-copilot-types";

/** A failed Copilot HTTP call, carrying the classified cause instead of a bare message. */
export class FactoryCopilotRequestError extends Error {
  constructor(
    readonly failure: ClassifiedApiFailure,
    readonly backendMessage: string | null,
  ) {
    super("Copilot request failed");
  }
}

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  return url.toString();
}

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
): Promise<T> {
  const response = await fetch(buildUrl(context, path), {
    method: "POST",
    headers: {
      ...context.requestHeaders,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
    cache: "no-store",
  });

  if (!response.ok) {
    const failure = await classifyFailedResponse(response);
    let backendMessage: string | null = null;
    try {
      const parsed = JSON.parse(await response.text()) as { message?: unknown };
      backendMessage = typeof parsed.message === "string" ? parsed.message : null;
    } catch {
      backendMessage = null;
    }

    throw new FactoryCopilotRequestError(failure, backendMessage);
  }

  return (await response.json()) as T;
}

export async function fetchFactoryCopilotQuery(
  context: ProductionOrderRequestContext,
  request: FactoryCopilotQueryRequest,
): Promise<FactoryCopilotQueryResult> {
  return postJson<FactoryCopilotQueryResult>(context, "/api/factory-copilot/query", request);
}

export async function fetchFactoryCopilotSuggestedQuestions(
  context: ProductionOrderRequestContext,
  copilotContext: FactoryCopilotContextInput,
): Promise<FactoryCopilotSuggestedQuestionsResult> {
  return postJson<FactoryCopilotSuggestedQuestionsResult>(
    context,
    "/api/factory-copilot/suggested-questions",
    { context: copilotContext },
  );
}
