import "server-only";

import {
  fetchCurrentEffectiveAccessResult,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import {
  AUTHENTICATION_REQUIRED_MESSAGE,
  AUTHORIZATION_UNAVAILABLE_MESSAGE,
} from "@/features/admin-access/server/require-current-permission";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  readOperationalValueControlCenter,
  type OperationalValueControlCenterApi,
} from "./operational-value-api-client";

export type OperationalValueWorkspaceResult =
  | Readonly<{ status: "success"; controlCenter: OperationalValueControlCenterApi }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext(permission = requiredRoutePermission("operational-value")) {
  const accessResult = await fetchCurrentEffectiveAccessResult();
  if (accessResult.status === "unauthenticated") {
    return { status: "failure" as const, message: AUTHENTICATION_REQUIRED_MESSAGE };
  }

  if (accessResult.status === "unavailable") {
    return { status: "failure" as const, message: AUTHORIZATION_UNAVAILABLE_MESSAGE };
  }

  const access = accessResult.access;
  if (!hasGrantedPermission(access, permission)) {
    return { status: "failure" as const, message: "Operasyonel değer görüntüleme yetkiniz yok." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return {
    status: "success" as const,
    context: contextResult.context,
    capabilities: {
      canManageEconomicInputs: hasGrantedPermission(access, "operational-value.manage-economic-inputs"),
      canExport: hasGrantedPermission(access, "operational-value.export"),
    },
  };
}

export async function loadOperationalValueControlCenter(): Promise<OperationalValueWorkspaceResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readOperationalValueControlCenter(resolved.context, resolved.capabilities);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", controlCenter: result.value };
}
