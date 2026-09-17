import "./executive-command-center-server-boundary";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { listAiTasks } from "@/features/ai-task-center/server/ai-task-api-client";
import {
  fetchActivity,
  fetchApprovals,
  fetchDigitalWorkforceControlCenter,
} from "@/features/digital-workforce/server/digital-workforce-api-client";
import { readFactoryDecisionCenterWorkspace } from "@/features/factory-decision-center/server/factory-decision-center-api-client";
import { readThroughputOptimizerExecutiveSummary } from "@/features/throughput-optimizer/server/throughput-optimizer-api-client";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { buildExecutiveCommandCenterWorkspace } from "../application/executive-command-center-builder";
import type { ExecutiveCommandCenterFilters } from "../types/executive-command-center-types";
import type { ExecutiveCommandCenterWorkspacePayload } from "../types/executive-command-center-types";

const EXECUTIVE_READ_PERMISSIONS = [
  "factory-case.read",
  "digital-workforce.read",
  "ai-task.read",
  "factory-intelligence.read",
  "throughput-optimizer.read",
] as const;

export type ExecutiveCommandCenterResult =
  | Readonly<{ status: "success"; workspace: ExecutiveCommandCenterWorkspacePayload }>
  | Readonly<{ status: "failure"; message: string; code?: string }>;

async function resolveRequestContext() {
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

export async function readExecutiveCommandCenterWorkspace(
  filters: ExecutiveCommandCenterFilters,
): Promise<ExecutiveCommandCenterResult> {
  const access = await fetchCurrentEffectiveAccess();
  const hasAnyPermission = EXECUTIVE_READ_PERMISSIONS.some((permission) =>
    hasGrantedPermission(access, permission),
  );

  if (!hasAnyPermission) {
    return {
      status: "failure",
      code: "forbidden",
      message: "Yönetici Komuta Merkezi görüntüleme yetkiniz yok.",
    };
  }

  const contextResult = await resolveRequestContext();
  if (contextResult.status === "failure") {
    return contextResult;
  }

  const context = contextResult.context;
  const unavailableSources: string[] = [];

  const decisionCenterPromise = hasGrantedPermission(access, "factory-case.read")
    ? readFactoryDecisionCenterWorkspace(context)
    : Promise.resolve({ status: "failure" as const, code: "forbidden", message: "factory-case.read" });

  const workforcePromise = hasGrantedPermission(access, "digital-workforce.read")
    ? fetchDigitalWorkforceControlCenter(context).then(
        (value) => ({ status: "success" as const, value }),
        () => ({ status: "failure" as const, message: "digital-workforce" }),
      )
    : Promise.resolve({ status: "failure" as const, message: "digital-workforce.read" });

  const approvalsPromise = hasGrantedPermission(access, "digital-workforce.read")
    ? fetchApprovals(context, "pending").then(
        (value) => ({ status: "success" as const, value }),
        () => ({ status: "failure" as const, message: "approvals" }),
      )
    : Promise.resolve({ status: "failure" as const, message: "digital-workforce.read" });

  const activityPromise = hasGrantedPermission(access, "digital-workforce.read")
    ? fetchActivity(context).then(
        (value) => ({ status: "success" as const, value }),
        () => ({ status: "failure" as const, message: "activity" }),
      )
    : Promise.resolve({ status: "failure" as const, message: "digital-workforce.read" });

  const aiTasksPromise = hasGrantedPermission(access, "ai-task.read")
    ? listAiTasks(context, { page: 1, pageSize: 100 })
    : Promise.resolve({ status: "failure" as const, code: "permission-denied", message: "ai-task.read", retryable: false });

  const throughputPromise = hasGrantedPermission(access, "throughput-optimizer.read")
    ? readThroughputOptimizerExecutiveSummary(context)
    : Promise.resolve({ status: "failure" as const, code: "permission-denied", message: "throughput-optimizer.read" });

  const [decisionCenterResult, workforceResult, approvalsResult, activityResult, aiTasksResult, throughputResult] =
    await Promise.all([
      decisionCenterPromise,
      workforcePromise,
      approvalsPromise,
      activityPromise,
      aiTasksPromise,
      throughputPromise,
    ]);

  if (decisionCenterResult.status === "failure") {
    unavailableSources.push("factory-decision-center");
  }
  if (workforceResult.status === "failure") {
    unavailableSources.push("digital-workforce");
  }
  if (approvalsResult.status === "failure") {
    unavailableSources.push("digital-workforce-approvals");
  }
  if (activityResult.status === "failure") {
    unavailableSources.push("digital-workforce-activity");
  }
  if (aiTasksResult.status === "failure") {
    unavailableSources.push("ai-task-center");
  }
  if (throughputResult.status === "failure") {
    unavailableSources.push("throughput-optimizer");
  }

  if (decisionCenterResult.status === "failure" && workforceResult.status === "failure") {
    return {
      status: "failure",
      message: "Yönetici özet verisi yüklenemedi. Karar merkezi ve dijital iş gücü erişilemedi.",
    };
  }

  const workspace = buildExecutiveCommandCenterWorkspace({
    decisionCenter: decisionCenterResult.status === "success" ? decisionCenterResult.value : undefined,
    digitalWorkforce: workforceResult.status === "success" ? workforceResult.value : undefined,
    workforceApprovals: approvalsResult.status === "success" ? approvalsResult.value : undefined,
    workforceActivity: activityResult.status === "success" ? activityResult.value : undefined,
    aiTasks: aiTasksResult.status === "success" ? aiTasksResult.value.items : undefined,
    throughputSummary: throughputResult.status === "success" ? throughputResult.value : undefined,
    generatedAtUtc: new Date().toISOString(),
    filters,
    unavailableSources,
  });

  return { status: "success", workspace };
}
