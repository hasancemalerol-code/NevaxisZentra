import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  readIntelligenceRuntimeControlCenter,
  type IntelligenceRuntimeControlCenterApi,
} from "./intelligence-runtime-api-client";

export type IntelligenceRuntimeWorkspaceResult =
  | Readonly<{ status: "success"; controlCenter: IntelligenceRuntimeControlCenterApi }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext(permission = requiredRoutePermission("intelligence-runtime")) {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, permission)) {
    return { status: "failure" as const, message: "Intelligence runtime görüntüleme yetkiniz yok." };
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

export async function loadIntelligenceRuntimeControlCenter(): Promise<IntelligenceRuntimeWorkspaceResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readIntelligenceRuntimeControlCenter(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", controlCenter: result.value };
}
