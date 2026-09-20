import "./works-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import type { WorksCaseDetailSnapshot, WorksWorkspaceSnapshot } from "../domain/works-types";

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value) {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

async function request(
  context: ProductionOrderRequestContext,
  method: string,
  path: string,
  query?: Record<string, string | undefined>,
): Promise<Response> {
  const response = await fetch(buildUrl(context, path, query), {
    method,
    headers: {
      ...context.requestHeaders,
      "Content-Type": "application/json",
    },
    cache: "no-store",
  });
  if (!response.ok) {
    const text = await response.text();
    let message = `İstek başarısız (${response.status})`;
    try {
      const body = JSON.parse(text) as { message?: string; title?: string };
      message = body.message ?? body.title ?? message;
    } catch {
      // Never surface raw HTML or stack traces in operational UI.
    }
    throw new Error(message);
  }
  return response;
}

export async function fetchWorksWorkspace(
  context: ProductionOrderRequestContext,
  input?: Readonly<{
    queue?: string;
    search?: string;
    limit?: number;
  }>,
): Promise<WorksWorkspaceSnapshot> {
  const response = await request(context, "GET", "/api/digital-workforce/works/workspace", {
    queue: input?.queue,
    search: input?.search,
    limit: input?.limit ? String(input.limit) : undefined,
  });
  return (await response.json()) as WorksWorkspaceSnapshot;
}

export async function fetchWorksCaseDetail(
  context: ProductionOrderRequestContext,
  caseId: string,
): Promise<WorksCaseDetailSnapshot> {
  const response = await request(context, "GET", `/api/digital-workforce/works/cases/${caseId}`);
  return (await response.json()) as WorksCaseDetailSnapshot;
}
