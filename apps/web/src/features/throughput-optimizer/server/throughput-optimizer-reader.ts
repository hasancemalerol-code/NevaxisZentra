import "server-only";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  observeThroughputGap,
  readThroughputOptimizerWorkspace,
  type ThroughputOptimizerWorkspaceApi,
} from "./throughput-optimizer-api-client";

async function resolveContext(permission = "throughput-optimizer.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return { status: "failure" as const, message: authorization.code === "authorization-denied" ? "Verim optimizasyon görüntüleme yetkiniz yok." : authorization.message };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function loadThroughputOptimizerWorkspaceForLine(
  lineId: string,
): Promise<
  | { status: "success"; workspace: ThroughputOptimizerWorkspaceApi }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext("throughput-optimizer.run");
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await observeThroughputGap(resolved.context, lineId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", workspace: result.value };
}

export async function loadThroughputOptimizerWorkspaceForCase(
  caseId: string,
): Promise<
  | { status: "success"; workspace: ThroughputOptimizerWorkspaceApi }
  | { status: "failure"; message: string }
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readThroughputOptimizerWorkspace(resolved.context, caseId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", workspace: result.value };
}
