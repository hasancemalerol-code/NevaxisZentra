"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  approveNetworkAllocation,
  executeNetworkAllocation,
  simulateFactoryNetwork,
  type ApproveNetworkAllocationRequestApi,
  type ExecuteNetworkAllocationRequestApi,
  type SimulateNetworkRequestApi,
} from "./factory-network-api-client";

type ActionResult =
  | { status: "success"; message: string }
  | { status: "failure"; code: string; message: string };

async function resolveActionContext(permission: string) {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Bu işlem için yetkiniz yok.",
    };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, code: "configuration-error", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, code: contextResult.code, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

function revalidateFactoryNetworkSurfaces() {
  revalidatePath("/fabrika-agi");
  revalidatePath("/planlama");
  revalidatePath("/dashboard");
  revalidatePath("/operasyon/yonetim-merkezi");
}

export async function approveNetworkAllocationAction(
  request: ApproveNetworkAllocationRequestApi,
): Promise<ActionResult> {
  const resolved = await resolveActionContext("factory-network.approve");
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await approveNetworkAllocation(resolved.context, request);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  revalidateFactoryNetworkSurfaces();
  return {
    status: "success",
    message: request.approved ? "Ağ tahsisi onaylandı." : "Ağ tahsisi reddedildi.",
  };
}

export async function executeNetworkAllocationAction(
  request: ExecuteNetworkAllocationRequestApi,
): Promise<ActionResult> {
  const resolved = await resolveActionContext("factory-network.execute");
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await executeNetworkAllocation(resolved.context, request);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  revalidateFactoryNetworkSurfaces();
  return {
    status: "success",
    message: result.value.autoExecuted
      ? "Ağ tahsisi otomatik olarak uygulandı."
      : "Ağ tahsisi uygulama kuyruğuna alındı.",
  };
}

export async function simulateFactoryNetworkAction(
  request: SimulateNetworkRequestApi,
): Promise<ActionResult> {
  const resolved = await resolveActionContext("factory-network.simulate");
  if (resolved.status === "failure") {
    return resolved;
  }

  const result = await simulateFactoryNetwork(resolved.context, request);
  if (result.status === "failure") {
    return { status: "failure", code: result.code, message: result.message };
  }

  revalidateFactoryNetworkSurfaces();
  return {
    status: "success",
    message: `${result.value.length} ağ senaryosu simülasyonu tamamlandı.`,
  };
}
