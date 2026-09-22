import "server-only";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  readContinuousOptimizationControlCenter,
  readContinuousOptimizationOpportunities,
  type ContinuousOptimizationControlCenterApi,
  type OptimizationOpportunityApi,
} from "./continuous-optimization-api-client";

export type ContinuousOptimizationWorkspaceResult =
  | Readonly<{ status: "success"; controlCenter: ContinuousOptimizationControlCenterApi }>
  | Readonly<{ status: "failure"; message: string }>;

export type ContinuousOptimizationOpportunitiesResult =
  | Readonly<{ status: "success"; opportunities: readonly OptimizationOpportunityApi[] }>
  | Readonly<{ status: "failure"; message: string }>;

async function resolveContext(permission = requiredRoutePermission("continuous-optimization")) {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, permission)) {
    return { status: "failure" as const, message: "Sürekli optimizasyon görüntüleme yetkiniz yok." };
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

export async function loadContinuousOptimizationControlCenter(): Promise<ContinuousOptimizationWorkspaceResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readContinuousOptimizationControlCenter(resolved.context);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", controlCenter: result.value };
}

export async function loadContinuousOptimizationOpportunities(
  lineId?: string,
): Promise<ContinuousOptimizationOpportunitiesResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await readContinuousOptimizationOpportunities(resolved.context, lineId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", opportunities: result.value };
}
