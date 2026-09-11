import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  fetchCurrentShift,
  fetchShiftClosingPreview,
  fetchShiftHistory,
  fetchShiftMetrics,
  fetchShiftOpenItems,
} from "./shift-operations-api-client";

async function resolveAuthorizedContext() {
  const authorization = await denyUnlessGranted("shift-workforce.read");
  if (authorization.status === "failure") {
    return { status: "failure" as const, code: "permission-denied", message: "Vardiya görüntüleme yetkiniz yok." };
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

export async function readShiftOperationsWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const current = await fetchCurrentShift(resolved.context);
  if (current.status === "failure") {
    return { status: "failure" as const, message: current.message, code: current.code };
  }

  const [history, openItems, metrics, preview] = await Promise.all([
    fetchShiftHistory(resolved.context),
    fetchShiftOpenItems(resolved.context, current.value.id),
    fetchShiftMetrics(resolved.context, current.value.id),
    fetchShiftClosingPreview(resolved.context, current.value.id),
  ]);

  return {
    status: "success" as const,
    currentShift: current.value,
    history: history.status === "success" ? history.value.items : [],
    openItems: openItems.status === "success" ? openItems.value.items : [],
    metrics: metrics.status === "success" ? metrics.value : null,
    preview: preview.status === "success" ? preview.value : null,
  };
}

export async function readActiveShiftPanel() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return { status: "failure" as const, message: resolved.message };
  }

  const current = await fetchCurrentShift(resolved.context);
  if (current.status === "failure") {
    return { status: "failure" as const, message: current.message };
  }

  const [openItems, metrics] = await Promise.all([
    fetchShiftOpenItems(resolved.context, current.value.id),
    fetchShiftMetrics(resolved.context, current.value.id),
  ]);

  return {
    status: "success" as const,
    shift: current.value,
    openItems: openItems.status === "success" ? openItems.value.items : [],
    metrics: metrics.status === "success" ? metrics.value : null,
  };
}
