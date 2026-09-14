import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  getSystemStatusWorkspace,
  type SystemStatusWorkspaceApi,
} from "./enterprise-reliability-api-client";

async function resolveAuthorizedContext(permission = "operability.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Sistem durumu görüntüleme yetkiniz yok.",
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

export async function readSystemStatusWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getSystemStatusWorkspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return { status: "success" as const, workspace: workspace.value };
}

export function findComponentByKind(
  workspace: SystemStatusWorkspaceApi,
  componentKind: string,
): SystemStatusWorkspaceApi["components"][number] | undefined {
  return workspace.components.find((component) => component.componentKind === componentKind);
}

export async function readSystemStatusDashboardSummary() {
  const result = await readSystemStatusWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const };
  }

  const { workspace } = result;
  const unhealthyIntegrations = workspace.integrations.filter(
    (integration) => integration.uiStatusLabel !== "Bağlı",
  );
  const offlineEdgeAgents = workspace.edgeAgents.filter(
    (agent) => agent.connectivityStatus !== "healthy",
  );

  return {
    status: "success" as const,
    operatingMode: workspace.operatingMode,
    showsFalseHealth: workspace.showsFalseHealth,
    openAlertCount: workspace.alerts.length,
    integrationCount: workspace.integrations.length,
    unhealthyIntegrationCount: unhealthyIntegrations.length,
    edgeAgentCount: workspace.edgeAgents.length,
    offlineEdgeAgentCount: offlineEdgeAgents.length,
    generatedAtUtc: workspace.generatedAtUtc,
  };
}
