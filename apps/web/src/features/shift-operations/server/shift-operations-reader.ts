import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

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
    return { status: "failure" as const, code: "permission-denied", message: authorization.code === "authorization-denied" ? "Vardiya görüntüleme yetkiniz yok." : authorization.message };
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

export async function readShiftOperationsWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const current = await fetchCurrentShift(resolved.context);
  if (
    current.status === "failure"
    && current.code !== "shift-not-found"
    && current.code !== "http-404"
  ) {
    return { status: "failure" as const, message: current.message, code: current.code };
  }

  const currentShift = current.status === "success" ? current.value : null;
  const [history, openItems, metrics, preview] = currentShift
    ? await Promise.all([
        fetchShiftHistory(resolved.context),
        fetchShiftOpenItems(resolved.context, currentShift.id),
        fetchShiftMetrics(resolved.context, currentShift.id),
        fetchShiftClosingPreview(resolved.context, currentShift.id),
      ])
    : [await fetchShiftHistory(resolved.context), null, null, null];

  return {
    status: "success" as const,
    currentShift,
    history: history.status === "success" ? history.value.items : [],
    openItems: openItems?.status === "success" ? openItems.value.items : [],
    metrics: metrics?.status === "success" ? metrics.value : null,
    preview: preview?.status === "success" ? preview.value : null,
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
