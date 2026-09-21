import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  readOperationalValueControlCenter,
  type OperationalValueControlCenterApi,
} from "./operational-value-api-client";

export type OperationalValueWorkspaceResult =
  | Readonly<{ status: "success"; controlCenter: OperationalValueControlCenterApi }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext(permission = "operational-value.read") {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, permission)) {
    return { status: "failure" as const, message: "Operasyonel değer görüntüleme yetkiniz yok." };
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

export async function loadOperationalValueControlCenter(): Promise<OperationalValueWorkspaceResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readOperationalValueControlCenter(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", controlCenter: result.value };
}
