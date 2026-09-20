import "./factory-copilot-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import type {
  FactoryCopilotContextInput,
  FactoryCopilotQueryRequest,
  FactoryCopilotQueryResult,
  FactoryCopilotSuggestedQuestionsResult,
} from "../domain/factory-copilot-types";

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
    const text = await response.text();
    let message = `Copilot isteği başarısız (${response.status})`;
    try {
      const parsed = JSON.parse(text) as { message?: string };
      message = parsed.message ?? message;
    } catch {
      // ignore
    }
    throw new Error(message);
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
