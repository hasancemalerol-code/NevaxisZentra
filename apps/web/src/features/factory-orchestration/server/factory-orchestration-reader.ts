import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { getFactoryCaseDetail, listFactoryCases } from "./factory-orchestration-api-client";

async function resolveAuthorizedContext(permission = "factory-case.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return { status: "failure" as const, code: "permission-denied", message: "Fabrika vakası görüntüleme yetkiniz yok." };
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

export async function readActiveFactoryCasesPanel() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return { status: "failure" as const };
  }

  const cases = await listFactoryCases(resolved.context, 10);
  if (cases.status === "failure") {
    return { status: "failure" as const };
  }

  const active = cases.value.items.filter((row) => row.status !== "closed");
  const verifiedToday = cases.value.items.filter((row) =>
    row.status === "closed" && row.verificationState === "passed");

  return {
    status: "success" as const,
    criticalCases: active.filter((row) => row.severity === "critical" || row.severity === "high"),
    activeCount: active.length,
    verifiedTodayCount: verifiedToday.length,
    cases: active.slice(0, 5),
  };
}

export async function readFactoryCaseDetail(caseId: string) {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const detail = await getFactoryCaseDetail(resolved.context, caseId);
  if (detail.status === "failure") {
    return { status: "failure" as const, code: detail.code, message: detail.message };
  }

  return { status: "success" as const, detail: detail.value };
}
