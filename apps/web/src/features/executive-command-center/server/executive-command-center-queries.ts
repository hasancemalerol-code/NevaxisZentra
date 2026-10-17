import { gateAccess, accessFailureMessage } from "@/features/admin-access/server/require-current-permission";
import "./executive-command-center-server-boundary";

import {
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
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import { buildExecutiveCommandCenterWorkspace } from "../application/executive-command-center-builder";
import type { ExecutiveCommandCenterFilters } from "../types/executive-command-center-types";
import type { ExecutiveCommandCenterWorkspacePayload } from "../types/executive-command-center-types";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

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

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readExecutiveCommandCenterWorkspace(
  filters: ExecutiveCommandCenterFilters,
): Promise<ExecutiveCommandCenterResult> {
  const gate = await gateAccess(EXECUTIVE_READ_PERMISSIONS);
  if (gate.status === "failure") {
    return {
      status: "failure",
      code: gate.code === "authorization-denied" ? "forbidden" : gate.code,
      message: accessFailureMessage(gate, "Yönetici Komuta Merkezi görüntüleme yetkiniz yok."),
    };
  }

  const access = gate.access;

  const contextResult = await resolveRequestContext();
  if (contextResult.status === "failure") {
    return contextResult;
  }

  const context = contextResult.context;
  const unavailableSources: string[] = [];

  const decisionCenterPromise = hasGrantedPermission(access, "factory-case.read")
    ? readFactoryDecisionCenterWorkspace(context)
    : Promise.resolve({ status: "failure" as const, code: "forbidden", message: "factory-case.read" });

  const workforceEnabled = deferredSurfacesEnabled() && hasGrantedPermission(access, "digital-workforce.read");

  const workforcePromise = workforceEnabled
    ? fetchDigitalWorkforceControlCenter(context).then(
        (value) => ({ status: "success" as const, value }),
        () => ({ status: "failure" as const, message: "digital-workforce" }),
      )
    : Promise.resolve({ status: "failure" as const, message: "digital-workforce.read" });

  const approvalsPromise = workforceEnabled
    ? fetchApprovals(context, "pending").then(
        (value) => ({ status: "success" as const, value }),
        () => ({ status: "failure" as const, message: "approvals" }),
      )
    : Promise.resolve({ status: "failure" as const, message: "digital-workforce.read" });

  const activityPromise = workforceEnabled
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

  // A source the account may not read, or one that is deferred in this deployment, is "not applicable": it is not reported
  // as unavailable and it never counts towards collapsing the page. Only a source that was actually asked and failed does.
  const sources = [
    { name: "factory-decision-center", attempted: hasGrantedPermission(access, "factory-case.read"), ok: decisionCenterResult.status === "success" },
    { name: "digital-workforce", attempted: workforceEnabled, ok: workforceResult.status === "success" },
    { name: "digital-workforce-approvals", attempted: workforceEnabled, ok: approvalsResult.status === "success" },
    { name: "digital-workforce-activity", attempted: workforceEnabled, ok: activityResult.status === "success" },
    { name: "ai-task-center", attempted: hasGrantedPermission(access, "ai-task.read"), ok: aiTasksResult.status === "success" },
    { name: "throughput-optimizer", attempted: hasGrantedPermission(access, "throughput-optimizer.read"), ok: throughputResult.status === "success" },
  ];
  for (const source of sources) {
    if (source.attempted && !source.ok) {
      unavailableSources.push(source.name);
    }
  }

  if (!sources.some((source) => source.ok)) {
    return {
      status: "failure",
      message: "Yönetici özet verisi yüklenemedi.",
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
