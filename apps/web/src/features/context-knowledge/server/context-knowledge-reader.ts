import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { buildWorkerContext, type WorkerContextPackageApi } from "./context-knowledge-api-client";

async function resolveContext() {
  const authorization = await denyUnlessGranted("context-knowledge.read");
  if (authorization.status === "failure") {
    return { status: "failure" as const, message: "Bağlam görüntüleme yetkiniz yok." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function loadWorkerContextForLine(
  lineId: string,
): Promise<
  | { status: "success"; package: WorkerContextPackageApi }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await buildWorkerContext(resolved.context, {
    entityType: "line",
    entityId: lineId,
    lineId,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", package: result.value };
}

export async function loadWorkerContextForCase(
  caseId: string,
): Promise<
  | { status: "success"; package: WorkerContextPackageApi }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await buildWorkerContext(resolved.context, {
    entityType: "digital_work_case",
    entityId: caseId,
    digitalWorkCaseId: caseId,
  });

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", package: result.value };
}
