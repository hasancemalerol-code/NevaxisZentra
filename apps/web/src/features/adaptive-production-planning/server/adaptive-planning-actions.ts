"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  approveAdaptiveReplan,
  simulateAdaptivePlan,
  type ApproveReplanRequestApi,
  type SimulatePlanRequestApi,
} from "./adaptive-planning-api-client";

type ActionResult =
  | { status: "success"; message: string }
  | { status: "failure"; code: string; message: string };

async function resolveActionContext(permission: string) {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: authorization.code === "authorization-denied" ? "Bu işlem için yetkiniz yok." : authorization.message,
    };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, code: "configuration-error", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, code: contextResult.code, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

function revalidateAdaptivePlanningSurfaces() {
  revalidatePath("/planlama");
  revalidatePath("/dashboard");
  revalidatePath("/vardiyalar");
}

export async function approveAdaptiveReplanAction(
  request: ApproveReplanRequestApi,
): Promise<ActionResult> {
  const resolved = await resolveActionContext("production.planning.approve");
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await approveAdaptiveReplan(resolved.context, request);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  revalidateAdaptivePlanningSurfaces();
  return { status: "success", message: request.approved ? "Replan onaylandı." : "Replan reddedildi." };
}

export async function simulateAdaptiveReplanAction(
  request: SimulatePlanRequestApi,
): Promise<ActionResult> {
  const resolved = await resolveActionContext("production.planning.simulate");
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await simulateAdaptivePlan(resolved.context, request);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  revalidateAdaptivePlanningSurfaces();
  return {
    status: "success",
    message: `${result.value.length} senaryo simülasyonu tamamlandı.`,
  };
}
