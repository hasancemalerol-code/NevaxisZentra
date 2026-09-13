import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  evaluateNetworkAllocation,
  getFactoryNetworkWorkspace,
  type FactoryNetworkWorkspaceApi,
  type NetworkAllocationEvaluationApi,
} from "./factory-network-api-client";

async function resolveAuthorizedContext(permission = "factory-network.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Fabrika ağı görüntüleme yetkiniz yok.",
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

export function hasMultiFacilityNetwork(workspace: FactoryNetworkWorkspaceApi): boolean {
  return workspace.facilities.length > 1;
}

export async function readFactoryNetworkWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getFactoryNetworkWorkspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return { status: "success" as const, workspace: workspace.value };
}

async function readLatestAllocationEvaluation(
  workspace: FactoryNetworkWorkspaceApi,
  context: Awaited<ReturnType<typeof resolveAuthorizedContext>> & { status: "success" },
): Promise<NetworkAllocationEvaluationApi | null> {
  const latestAllocation = workspace.activeAllocations[0];
  const triggerType = latestAllocation?.triggerType ?? "scheduled-review";
  const triggerContextJson = latestAllocation
    ? JSON.stringify({ allocationId: latestAllocation.id, actionType: latestAllocation.actionType })
    : "{}";

  const evaluation = await evaluateNetworkAllocation(context.context, {
    networkId: workspace.network.id,
    triggerType,
    triggerContextJson,
  });

  return evaluation.status === "success" ? evaluation.value : null;
}

export async function readFactoryNetworkWorkspaceDetail() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspaceResult = await getFactoryNetworkWorkspace(resolved.context);
  if (workspaceResult.status === "failure") {
    return { status: "failure" as const, code: workspaceResult.code, message: workspaceResult.message };
  }

  const workspace = workspaceResult.value;
  const allocationEvaluation = await readLatestAllocationEvaluation(workspace, resolved);

  return {
    status: "success" as const,
    workspace,
    allocationEvaluation,
  };
}

export async function readFactoryNetworkDashboardPanel() {
  const result = await readFactoryNetworkWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const };
  }

  const { workspace } = result;
  if (!hasMultiFacilityNetwork(workspace)) {
    return { status: "failure" as const };
  }

  const latestAllocation = workspace.activeAllocations[0];
  const topOpportunity = workspace.opportunities[0];
  const highestLoadFacility = [...workspace.facilityStates].sort(
    (a, b) => b.loadPercent - a.loadPercent,
  )[0];

  return {
    status: "success" as const,
    metrics: workspace.metrics,
    network: workspace.network,
    facilityCount: workspace.facilities.length,
    latestAllocation,
    topOpportunity,
    highestLoadFacility,
    resilience: workspace.resilience,
  };
}

export async function readFactoryNetworkCrossLinkSummary() {
  const result = await readFactoryNetworkWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const };
  }

  const { workspace } = result;
  if (!hasMultiFacilityNetwork(workspace)) {
    return { status: "failure" as const };
  }

  const pendingAllocations = workspace.activeAllocations.filter(
    (allocation) =>
      allocation.status === "approval-pending" || allocation.status === "ready",
  );

  return {
    status: "success" as const,
    networkName: workspace.network.name,
    facilityCount: workspace.facilities.length,
    ordersAtRisk: workspace.metrics.ordersAtRisk,
    activeCrossPlantDecisions: workspace.metrics.activeCrossPlantDecisions,
    pendingAllocationCount: pendingAllocations.length,
    opportunityCount: workspace.opportunities.length,
    capacityImbalancePercent: workspace.metrics.capacityImbalancePercent,
  };
}
